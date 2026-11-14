using Microsoft.EntityFrameworkCore;
using Nexus.Api.Dtos;
using Nexus.Core.Entities;
using Nexus.Core.Services;
using Nexus.Data;

namespace Nexus.Api.Services;

/// <summary>
/// Capa de servicios de aplicación: coordina el acceso a datos con las reglas de negocio de Nexus.Core.
/// Cada cambio relevante deja un registro en HISTORIAL_INCIDENTE (RF012 / RNF007).
/// </summary>
public class IncidenteAppService
{
    private readonly NexusDbContext _db;
    private readonly PrioridadService _prioridad;
    private readonly RecomendacionService _recomendacion;
    private readonly DetectorCoincidencias _detector;

    public IncidenteAppService(NexusDbContext db, PrioridadService prioridad,
        RecomendacionService recomendacion, DetectorCoincidencias detector)
    {
        _db = db; _prioridad = prioridad; _recomendacion = recomendacion; _detector = detector;
    }

    public async Task<List<IncidenteDto>> ListarAsync()
    {
        var regla = await ReglaActivaAsync();
        var incidentes = await _db.Incidentes.AsNoTracking().Include(i => i.Categoria).ToListAsync();
        var ahora = DateTime.UtcNow;
        return incidentes
            .Select(i => ADto(i, _prioridad.Calcular(i, regla, ahora, Recurrencia(i, incidentes, ahora))))
            .OrderByDescending(d => d.Prioridad.Puntaje)
            .ToList();
    }

    public async Task<Resultado<CreadoResponse>> CrearAsync(CrearIncidenteRequest req, int idUsuario)
    {
        var categoria = await _db.Categorias.FindAsync(req.IdCategoria);
        if (categoria is null || !categoria.Activa)
            return Resultado<CreadoResponse>.Fallo("La categoría no existe o está inactiva.");

        var incidente = new Incidente
        {
            Titulo = req.Titulo.Trim(),
            Descripcion = req.Descripcion.Trim(),
            IdCategoria = req.IdCategoria,
            IdReportante = idUsuario,
            AreaAfectada = req.AreaAfectada.Trim(),
            Impacto = req.Impacto,
            Urgencia = req.Urgencia,
            Estado = EstadoIncidente.Nuevo,
            Categoria = categoria,
        };

        var regla = await ReglaActivaAsync();
        var otros = await _db.Incidentes.AsNoTracking().ToListAsync();
        var ahora = DateTime.UtcNow;
        var prioridad = _prioridad.Calcular(incidente, regla, ahora, Recurrencia(incidente, otros, ahora));
        incidente.PrioridadPuntaje = prioridad.Puntaje;

        _db.Incidentes.Add(incidente);
        _db.Historial.Add(new HistorialIncidente
        {
            Incidente = incidente, IdUsuario = idUsuario, EstadoNuevo = EstadoIncidente.Nuevo,
            Accion = "Incidente creado",
        });
        await _db.SaveChangesAsync(); // una sola transacción: incidente + historial

        var coincidencias = _detector.Detectar(incidente, otros, TimeSpan.FromDays(3));
        return new(new CreadoResponse(ADto(incidente, prioridad), coincidencias));
    }

    public async Task<Resultado<IncidenteDto>> CambiarEstadoAsync(int id, CambiarEstadoRequest req, int idUsuario)
    {
        if (!Enum.TryParse<EstadoIncidente>(req.Estado, true, out var nuevo))
            return Resultado<IncidenteDto>.Fallo("Estado desconocido.");

        var inc = await _db.Incidentes.Include(i => i.Categoria).FirstOrDefaultAsync(i => i.IdIncidente == id);
        if (inc is null) return Resultado<IncidenteDto>.Fallo("Incidente no encontrado.", 404);

        if (nuevo == EstadoIncidente.Asignado)
            return Resultado<IncidenteDto>.Fallo("Para asignar use el endpoint /asignar.");
        if (!TransicionEstadoService.EsValida(inc.Estado, nuevo))
            return Resultado<IncidenteDto>.Fallo($"Transición no permitida: {inc.Estado} → {nuevo}.", 409);

        var anterior = inc.Estado;
        inc.Estado = nuevo;
        inc.FechaActualizacion = DateTime.UtcNow;
        if (nuevo == EstadoIncidente.Resuelto)
        {
            inc.FechaResolucion = DateTime.UtcNow;
            await CerrarAsignacionesActivasAsync(id);
        }
        _db.Historial.Add(new HistorialIncidente
        {
            IdIncidente = id, IdUsuario = idUsuario, EstadoAnterior = anterior, EstadoNuevo = nuevo,
            Accion = "Cambio de estado", Observacion = req.Observacion,
        });
        await _db.SaveChangesAsync();
        return new(await DtoAsync(inc));
    }

    public async Task<Resultado<IncidenteDto>> AsignarAsync(int id, AsignarRequest req, int idUsuario)
    {
        var inc = await _db.Incidentes.Include(i => i.Categoria).FirstOrDefaultAsync(i => i.IdIncidente == id);
        if (inc is null) return Resultado<IncidenteDto>.Fallo("Incidente no encontrado.", 404);
        if (!TransicionEstadoService.EsValida(inc.Estado, EstadoIncidente.Asignado))
            return Resultado<IncidenteDto>.Fallo($"No se puede asignar un incidente en estado {inc.Estado}.", 409);

        var recursos = await _db.Recursos.Where(r => r.IdRecurso == req.IdRecurso).ToListAsync();
        var carga = await CargaPorRecursoAsync();
        var valido = _recomendacion.Recomendar(inc.Categoria!.Nombre, recursos, carga).Any();
        if (!valido)
            return Resultado<IncidenteDto>.Fallo(
                "El recurso no es compatible, no está disponible o alcanzó su capacidad máxima.", 409);

        _db.Asignaciones.Add(new Asignacion
        {
            IdIncidente = id, IdRecurso = req.IdRecurso, IdAsignador = idUsuario, Observacion = req.Observacion,
        });
        inc.Estado = EstadoIncidente.Asignado;
        inc.FechaActualizacion = DateTime.UtcNow;
        _db.Historial.Add(new HistorialIncidente
        {
            IdIncidente = id, IdUsuario = idUsuario, EstadoAnterior = EstadoIncidente.Nuevo,
            EstadoNuevo = EstadoIncidente.Asignado, Accion = "Incidente asignado", Observacion = req.Observacion,
        });
        await _db.SaveChangesAsync();
        return new(await DtoAsync(inc));
    }

    public async Task<Resultado<RecomendacionesResponse>> RecomendarAsync(int id)
    {
        var inc = await _db.Incidentes.AsNoTracking().Include(i => i.Categoria).FirstOrDefaultAsync(i => i.IdIncidente == id);
        if (inc is null) return Resultado<RecomendacionesResponse>.Fallo("Incidente no encontrado.", 404);

        var recursos = await _db.Recursos.AsNoTracking().ToListAsync();
        var recomendaciones = _recomendacion.Recomendar(inc.Categoria!.Nombre, recursos, await CargaPorRecursoAsync());
        var dto = recomendaciones
            .Select(r => new RecomendacionDto(r.Recurso.IdRecurso, r.Recurso.Nombre, r.Puntaje, r.CargaActual, r.Criterios))
            .ToList();
        return new(new RecomendacionesResponse(dto,
            dto.Count == 0 ? "No hay una recomendación válida para este incidente." : null));
    }

    public async Task<Resultado<IReadOnlyList<Coincidencia>>> CoincidenciasAsync(int id)
    {
        var todos = await _db.Incidentes.AsNoTracking().ToListAsync();
        var inc = todos.FirstOrDefault(i => i.IdIncidente == id);
        if (inc is null) return Resultado<IReadOnlyList<Coincidencia>>.Fallo("Incidente no encontrado.", 404);
        return new(_detector.Detectar(inc, todos, TimeSpan.FromDays(3)));
    }

    public async Task<PanelDto> PanelAsync()
    {
        var incidentes = await ListarAsync();
        var porEstado = incidentes.GroupBy(i => i.Estado).ToDictionary(g => g.Key, g => g.Count());
        var porNivel = incidentes.GroupBy(i => i.Prioridad.Nivel).ToDictionary(g => g.Key, g => g.Count());
        return new PanelDto(incidentes.Count, porEstado, porNivel, await RecursosAsync());
    }

    public async Task<List<RecursoDto>> RecursosAsync()
    {
        var carga = await CargaPorRecursoAsync();
        var recursos = await _db.Recursos.AsNoTracking().Where(r => r.Activo).ToListAsync();
        return recursos
            .Select(r => new RecursoDto(r.IdRecurso, r.Nombre, r.Capacidades, r.Disponible, r.CapacidadMaxima,
                carga.TryGetValue(r.IdRecurso, out var c) ? c : 0))
            .ToList();
    }

    // ---------- auxiliares ----------

    private async Task<ReglaPrioridad> ReglaActivaAsync() =>
        await _db.ReglasPrioridad.AsNoTracking().FirstOrDefaultAsync(r => r.Activa)
        ?? throw new InvalidOperationException("No hay una regla de prioridad activa.");

    /// <summary>Incidentes de la misma categoría y área en los últimos 30 días (sin contar el propio).</summary>
    private static int Recurrencia(Incidente i, IEnumerable<Incidente> todos, DateTime ahora) =>
        todos.Count(o => o.IdIncidente != i.IdIncidente
                         && o.IdCategoria == i.IdCategoria
                         && string.Equals(o.AreaAfectada, i.AreaAfectada, StringComparison.OrdinalIgnoreCase)
                         && (ahora - o.FechaCreacion).TotalDays <= 30);

    public async Task<Dictionary<int, int>> CargaPorRecursoAsync() =>
        await _db.Asignaciones.AsNoTracking()
            .Where(a => a.FechaFin == null)
            .GroupBy(a => a.IdRecurso)
            .Select(g => new { g.Key, N = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.N);

    private async Task CerrarAsignacionesActivasAsync(int idIncidente)
    {
        var activas = await _db.Asignaciones.Where(a => a.IdIncidente == idIncidente && a.FechaFin == null).ToListAsync();
        foreach (var a in activas) a.FechaFin = DateTime.UtcNow;
    }

    private async Task<IncidenteDto> DtoAsync(Incidente inc)
    {
        var todos = await _db.Incidentes.AsNoTracking().ToListAsync();
        var p = _prioridad.Calcular(inc, await ReglaActivaAsync(), DateTime.UtcNow, Recurrencia(inc, todos, DateTime.UtcNow));
        return ADto(inc, p);
    }

    private static IncidenteDto ADto(Incidente i, ResultadoPrioridad p) =>
        new(i.IdIncidente, i.Titulo, i.Categoria?.Nombre ?? "", i.AreaAfectada, i.Impacto, i.Urgencia,
            i.Estado.ToString(), i.FechaCreacion, new PrioridadDto(p.Puntaje, p.Nivel.ToString(), p.Factores));
}

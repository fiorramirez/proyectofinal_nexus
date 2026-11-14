using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Nexus.Api.Dtos;
using Nexus.Core.Entities;
using Nexus.Core.Services;
using Nexus.Data;

namespace Nexus.Api.Services;

/// <summary>
/// RF010 / RF011. Lee los datos reales en modo solo lectura (AsNoTracking) y guarda únicamente
/// en la tabla ESCENARIO, de modo que simular nunca altera incidentes ni asignaciones.
/// </summary>
public class SimulacionAppService
{
    private const string Aviso = "Estimación calculada a partir de los supuestos indicados; no es una predicción.";

    private readonly NexusDbContext _db;
    private readonly SimuladorService _simulador;

    public SimulacionAppService(NexusDbContext db, SimuladorService simulador)
    {
        _db = db; _simulador = simulador;
    }

    public async Task<SimulacionResponse> EjecutarAsync(SimulacionRequest req, int idUsuario, bool guardar = true)
    {
        var abiertos = await _db.Incidentes.AsNoTracking()
            .Include(i => i.Categoria)
            .Where(i => i.Estado == EstadoIncidente.Nuevo || i.Estado == EstadoIncidente.Asignado || i.Estado == EstadoIncidente.EnProceso)
            .ToListAsync();
        var incidentes = abiertos
            .Select(i => new IncidenteSim(i.IdIncidente, i.Categoria!.Nombre, i.PrioridadPuntaje ?? 0))
            .ToList();
        var recursos = (await _db.Recursos.AsNoTracking().Where(r => r.Activo).ToListAsync())
            .Select(r => new RecursoSim(r.IdRecurso, r.Capacidades, r.Disponible, r.CapacidadMaxima))
            .ToList();

        var supuestos = new SupuestosSimulacion(req.DisponibilidadRecursos, req.VolumenAdicionalPorcentaje, req.HorasPorIncidente);
        var resultado = _simulador.Simular(incidentes, recursos, supuestos);

        if (guardar)
        {
            _db.Escenarios.Add(new Escenario
            {
                IdCreador = idUsuario,
                Nombre = req.Nombre,
                SupuestosJson = JsonSerializer.Serialize(req),
                ResultadosJson = JsonSerializer.Serialize(resultado),
                FechaEjecucion = DateTime.UtcNow,
            });
            await _db.SaveChangesAsync();
        }
        return new SimulacionResponse(req.Nombre, resultado, Aviso);
    }

    public async Task<List<SimulacionResponse>> CompararAsync(ComparacionRequest req, int idUsuario)
    {
        var lista = new List<SimulacionResponse>();
        foreach (var esc in req.Escenarios)
            lista.Add(await EjecutarAsync(esc, idUsuario));
        return lista;
    }
}

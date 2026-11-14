namespace Nexus.Core.Services;

public record IncidenteSim(int Id, string Categoria, decimal Puntaje);
public record RecursoSim(int Id, string Capacidades, bool Disponible, int CapacidadMaxima);

public record SupuestosSimulacion(
    IReadOnlyDictionary<int, bool>? DisponibilidadRecursos = null,
    int VolumenAdicionalPorcentaje = 0,
    double HorasPorIncidente = 2.0);

public record ResultadoSimulacion(
    int IncidentesAtendidos,
    int IncidentesPendientes,
    double CargaEstimadaPorcentaje,
    double TiempoTotalEstimadoHoras,
    IReadOnlyDictionary<int, int> CargaPorRecurso,
    IReadOnlyList<string> SupuestosAplicados);

/// <summary>
/// RF010 / RF011. Trabaja únicamente sobre copias de los datos recibidos (registros inmutables),
/// por lo que ejecutar una simulación nunca modifica los incidentes reales.
/// Los resultados son estimaciones condicionadas por los supuestos, no predicciones.
/// </summary>
public class SimuladorService
{
    public ResultadoSimulacion Simular(
        IEnumerable<IncidenteSim> incidentes,
        IEnumerable<RecursoSim> recursos,
        SupuestosSimulacion supuestos)
    {
        if (supuestos.VolumenAdicionalPorcentaje < 0)
            throw new ArgumentOutOfRangeException(nameof(supuestos), "El volumen adicional no puede ser negativo.");
        if (supuestos.HorasPorIncidente <= 0)
            throw new ArgumentOutOfRangeException(nameof(supuestos), "Las horas por incidente deben ser mayores que cero.");

        var cola = incidentes.OrderByDescending(i => i.Puntaje).ThenBy(i => i.Id).ToList();

        // Volumen adicional: se replican los incidentes más prioritarios con ids negativos (sintéticos).
        var baseCount = cola.Count;
        var extra = baseCount == 0 ? 0 : (int)Math.Ceiling(baseCount * supuestos.VolumenAdicionalPorcentaje / 100.0);
        for (var k = 0; k < extra; k++)
            cola.Add(cola[k % baseCount] with { Id = -(k + 1) });
        cola = cola.OrderByDescending(i => i.Puntaje).ThenBy(i => i.Id).ToList();

        // Disponibilidad efectiva de cada recurso bajo los supuestos.
        var activos = new List<RecursoSim>();
        foreach (var r in recursos)
        {
            var disponible = r.Disponible;
            if (supuestos.DisponibilidadRecursos != null &&
                supuestos.DisponibilidadRecursos.TryGetValue(r.Id, out var forzada))
                disponible = forzada;
            if (disponible && r.CapacidadMaxima > 0)
                activos.Add(r with { Disponible = true });
        }

        var carga = activos.ToDictionary(r => r.Id, _ => 0);
        var atendidos = 0;

        foreach (var inc in cola)
        {
            var elegido = activos
                .Where(r => carga[r.Id] < r.CapacidadMaxima && Capacidades.Atiende(r.Capacidades, inc.Categoria))
                .OrderBy(r => carga[r.Id] / (double)r.CapacidadMaxima)
                .ThenBy(r => r.Id)
                .FirstOrDefault();
            if (elegido is null) continue;
            carga[elegido.Id]++;
            atendidos++;
        }

        var capacidadTotal = activos.Sum(r => r.CapacidadMaxima);
        var cargaPct = capacidadTotal == 0 ? 0 : Math.Round(carga.Values.Sum() * 100.0 / capacidadTotal, 1);
        var horas = Math.Round(atendidos * supuestos.HorasPorIncidente / Math.Max(1, activos.Count), 1);

        var aplicados = new List<string>
        {
            $"Volumen adicional: {supuestos.VolumenAdicionalPorcentaje} %",
            $"Horas estimadas por incidente: {supuestos.HorasPorIncidente}",
            $"Recursos disponibles considerados: {activos.Count}",
        };

        return new ResultadoSimulacion(atendidos, cola.Count - atendidos, cargaPct, horas, carga, aplicados);
    }
}

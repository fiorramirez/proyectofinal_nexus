using Nexus.Core.Entities;

namespace Nexus.Core.Services;

public record RecomendacionRecurso(Recurso Recurso, double Puntaje, int CargaActual, IReadOnlyList<string> Criterios);

/// <summary>RF007: ordena recursos compatibles y explica los criterios usados.</summary>
public class RecomendacionService
{
    /// <returns>Lista vacía cuando no existe un recurso válido.</returns>
    public IReadOnlyList<RecomendacionRecurso> Recomendar(
        string categoriaIncidente,
        IEnumerable<Recurso> recursos,
        IReadOnlyDictionary<int, int> cargaPorRecurso)
    {
        var resultado = new List<RecomendacionRecurso>();
        foreach (var r in recursos)
        {
            if (!r.Activo || !r.Disponible) continue;
            if (!Capacidades.Atiende(r.Capacidades, categoriaIncidente)) continue;

            var carga = cargaPorRecurso.TryGetValue(r.IdRecurso, out var c) ? c : 0;
            if (r.CapacidadMaxima <= 0 || carga >= r.CapacidadMaxima) continue;

            var libre = 1.0 - carga / (double)r.CapacidadMaxima;
            resultado.Add(new RecomendacionRecurso(r, Math.Round(libre * 100, 1), carga, new[]
            {
                $"Atiende la categoría \"{categoriaIncidente}\"",
                "Está disponible",
                $"Carga actual {carga} de {r.CapacidadMaxima}",
            }));
        }
        return resultado.OrderByDescending(x => x.Puntaje).ThenBy(x => x.Recurso.IdRecurso).ToList();
    }
}

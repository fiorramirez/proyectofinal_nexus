using System.Text.RegularExpressions;
using Nexus.Core.Entities;

namespace Nexus.Core.Services;

public record Coincidencia(int IdIncidente, string Titulo, double Similitud, IReadOnlyList<string> Criterios);

/// <summary>
/// RF004: sugiere incidentes posiblemente relacionados (categoría, área, palabras clave y cercanía en el tiempo).
/// Solo sugiere: nunca fusiona ni elimina registros.
/// </summary>
public class DetectorCoincidencias
{
    private static readonly HashSet<string> PalabrasVacias = new()
    {
        "para", "pero", "como", "este", "esta", "esto", "desde", "hasta", "porque", "cuando", "tiene", "tienen", "sigue", "problema",
    };

    public IReadOnlyList<Coincidencia> Detectar(
        Incidente objetivo,
        IEnumerable<Incidente> abiertos,
        TimeSpan ventana,
        double umbral = 0.5)
    {
        var palabrasObjetivo = Palabras(objetivo);
        var lista = new List<Coincidencia>();

        foreach (var otro in abiertos)
        {
            if (otro.IdIncidente == objetivo.IdIncidente) continue;
            if (otro.Estado is EstadoIncidente.Resuelto or EstadoIncidente.Cerrado) continue;

            var criterios = new List<string>();
            double similitud = 0;

            if (otro.IdCategoria == objetivo.IdCategoria) { similitud += 0.3; criterios.Add("Misma categoría"); }
            if (string.Equals(otro.AreaAfectada, objetivo.AreaAfectada, StringComparison.OrdinalIgnoreCase))
            { similitud += 0.3; criterios.Add("Misma área afectada"); }

            var palabrasOtro = Palabras(otro);
            var comunes = palabrasObjetivo.Intersect(palabrasOtro).Count();
            var union = palabrasObjetivo.Union(palabrasOtro).Count();
            if (union > 0 && comunes > 0)
            {
                similitud += 0.3 * comunes / union;
                criterios.Add($"Palabras clave en común ({comunes})");
            }

            if ((objetivo.FechaCreacion - otro.FechaCreacion).Duration() <= ventana)
            { similitud += 0.1; criterios.Add("Reportados con poca diferencia de tiempo"); }

            if (similitud >= umbral)
                lista.Add(new Coincidencia(otro.IdIncidente, otro.Titulo, Math.Round(similitud, 2), criterios));
        }
        return lista.OrderByDescending(c => c.Similitud).ToList();
    }

    private static HashSet<string> Palabras(Incidente i) =>
        Regex.Matches($"{i.Titulo} {i.Descripcion}".ToLowerInvariant(), @"[\p{L}\p{N}]{4,}")
            .Select(m => m.Value)
            .Where(p => !PalabrasVacias.Contains(p))
            .ToHashSet();
}

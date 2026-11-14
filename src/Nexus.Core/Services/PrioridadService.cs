using Nexus.Core.Entities;

namespace Nexus.Core.Services;

public record FactorPrioridad(string Nombre, double ValorNormalizado, decimal Peso, decimal Aporte);

public record ResultadoPrioridad(decimal Puntaje, NivelPrioridad Nivel, IReadOnlyList<FactorPrioridad> Factores);

/// <summary>
/// RF005 / RNF005. Puntaje de 0 a 100 = suma ponderada de factores normalizados a [0,1].
/// Es una función pura: los mismos datos y pesos producen siempre el mismo resultado.
/// </summary>
public class PrioridadService
{
    public const int EscalaMaxima = 5;        // impacto y urgencia: 1..5
    public const int DiasAntiguedadTope = 7;  // a partir de 7 días la antigüedad aporta el máximo
    public const int RecurrenciaTope = 5;     // 5 o más incidentes similares recientes = máximo

    public ResultadoPrioridad Calcular(Incidente incidente, ReglaPrioridad regla, DateTime ahora, int recurrencia)
    {
        if (incidente.Impacto is < 1 or > EscalaMaxima)
            throw new ArgumentOutOfRangeException(nameof(incidente), "El impacto debe estar entre 1 y 5.");
        if (incidente.Urgencia is < 1 or > EscalaMaxima)
            throw new ArgumentOutOfRangeException(nameof(incidente), "La urgencia debe estar entre 1 y 5.");

        var totalPesos = regla.PesoImpacto + regla.PesoUrgencia + regla.PesoAntiguedad + regla.PesoRecurrencia;
        if (totalPesos <= 0)
            throw new InvalidOperationException("La suma de los pesos de la regla debe ser mayor que cero.");

        var dias = Math.Max(0, (ahora - incidente.FechaCreacion).TotalDays);

        var factores = new List<FactorPrioridad>
        {
            Factor("Impacto", incidente.Impacto / (double)EscalaMaxima, regla.PesoImpacto, totalPesos),
            Factor("Urgencia", incidente.Urgencia / (double)EscalaMaxima, regla.PesoUrgencia, totalPesos),
            Factor("Antigüedad", Math.Min(dias / DiasAntiguedadTope, 1.0), regla.PesoAntiguedad, totalPesos),
            Factor("Recurrencia", Math.Min(recurrencia / (double)RecurrenciaTope, 1.0), regla.PesoRecurrencia, totalPesos),
        };

        var puntaje = Math.Round(factores.Sum(f => f.Aporte), 2);
        return new ResultadoPrioridad(puntaje, ClasificarNivel(puntaje), factores);
    }

    public static NivelPrioridad ClasificarNivel(decimal puntaje) => puntaje switch
    {
        >= 75 => NivelPrioridad.Critica,
        >= 50 => NivelPrioridad.Alta,
        >= 25 => NivelPrioridad.Media,
        _ => NivelPrioridad.Baja,
    };

    private static FactorPrioridad Factor(string nombre, double valor, decimal peso, decimal totalPesos) =>
        new(nombre, Math.Round(valor, 4), peso, (decimal)valor * peso / totalPesos * 100m);
}

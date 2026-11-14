using Nexus.Core.Entities;
using Nexus.Core.Services;

namespace Nexus.Tests;

/// <summary>CP005 / RF005: el cálculo es consistente y explicable.</summary>
public class PrioridadServiceTests
{
    private static readonly DateTime Ahora = new(2026, 11, 13, 12, 0, 0, DateTimeKind.Utc);
    private static readonly ReglaPrioridad Regla = new()
    {
        PesoImpacto = 0.35m, PesoUrgencia = 0.30m, PesoAntiguedad = 0.20m, PesoRecurrencia = 0.15m, Activa = true,
    };
    private readonly PrioridadService _svc = new();

    private static Incidente Inc(byte impacto, byte urgencia, int horas = 0) => new()
    {
        Impacto = impacto, Urgencia = urgencia, FechaCreacion = Ahora.AddHours(-horas),
    };

    [Fact]
    public void MismosDatosYPesos_ProducenLaMismaPuntuacion()
    {
        var a = _svc.Calcular(Inc(4, 3, 10), Regla, Ahora, 1);
        var b = _svc.Calcular(Inc(4, 3, 10), Regla, Ahora, 1);
        Assert.Equal(a.Puntaje, b.Puntaje);
    }

    [Fact]
    public void MayorImpactoYUrgencia_DanMayorPuntaje()
    {
        var bajo = _svc.Calcular(Inc(1, 1), Regla, Ahora, 0);
        var alto = _svc.Calcular(Inc(5, 5), Regla, Ahora, 0);
        Assert.True(alto.Puntaje > bajo.Puntaje);
    }

    [Fact]
    public void Resultado_IncluyeLosCuatroFactores()
    {
        var r = _svc.Calcular(Inc(3, 3), Regla, Ahora, 0);
        Assert.Equal(new[] { "Impacto", "Urgencia", "Antigüedad", "Recurrencia" }, r.Factores.Select(f => f.Nombre));
    }

    [Fact]
    public void ValoresMaximos_DanCien_YNivelCritico()
    {
        var r = _svc.Calcular(Inc(5, 5, 24 * 30), Regla, Ahora, 10);
        Assert.Equal(100m, r.Puntaje);
        Assert.Equal(NivelPrioridad.Critica, r.Nivel);
    }

    [Fact]
    public void ImpactoFueraDeEscala_SeRechaza() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => _svc.Calcular(Inc(9, 3), Regla, Ahora, 0));

    [Fact]
    public void PesosEnCero_SeRechazan() =>
        Assert.Throws<InvalidOperationException>(() => _svc.Calcular(Inc(3, 3), new ReglaPrioridad(), Ahora, 0));
}

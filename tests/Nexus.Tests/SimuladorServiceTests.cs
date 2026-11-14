using Nexus.Core.Services;

namespace Nexus.Tests;

/// <summary>CP009 / RF010: la simulación refleja los supuestos y no altera los datos de entrada.</summary>
public class SimuladorServiceTests
{
    private readonly SimuladorService _svc = new();

    private static List<IncidenteSim> Incidentes() => new()
    {
        new(1, "Red", 80), new(2, "Red", 60), new(3, "Hardware", 40), new(4, "Software", 20),
    };

    private static List<RecursoSim> Recursos() => new()
    {
        new(1, "Red", true, 2),
        new(2, "Hardware, Software", true, 2),
    };

    [Fact]
    public void SinCambios_AtiendeLosIncidentesCompatibles()
    {
        var r = _svc.Simular(Incidentes(), Recursos(), new SupuestosSimulacion());
        Assert.Equal(4, r.IncidentesAtendidos);
        Assert.Equal(0, r.IncidentesPendientes);
    }

    [Fact]
    public void DeshabilitarUnRecurso_ReduceLosAtendidos()
    {
        var supuestos = new SupuestosSimulacion(new Dictionary<int, bool> { [1] = false });
        var r = _svc.Simular(Incidentes(), Recursos(), supuestos);
        Assert.True(r.IncidentesAtendidos < 4);
        Assert.Equal(4 - r.IncidentesAtendidos, r.IncidentesPendientes);
    }

    [Fact]
    public void VolumenAdicional_AumentaLosPendientesPorLaCapacidadLimitada()
    {
        var baseR = _svc.Simular(Incidentes(), Recursos(), new SupuestosSimulacion());
        var masR = _svc.Simular(Incidentes(), Recursos(), new SupuestosSimulacion(null, 100));
        Assert.True(masR.IncidentesPendientes > baseR.IncidentesPendientes);
    }

    [Fact]
    public void NoModificaLosDatosDeEntrada()
    {
        var incidentes = Incidentes();
        var recursos = Recursos();
        _svc.Simular(incidentes, recursos, new SupuestosSimulacion(new Dictionary<int, bool> { [1] = false }, 50));
        Assert.Equal(Incidentes(), incidentes);
        Assert.Equal(Recursos(), recursos);
    }

    [Fact]
    public void SupuestosInvalidos_SeRechazan() =>
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _svc.Simular(Incidentes(), Recursos(), new SupuestosSimulacion(null, -5)));
}

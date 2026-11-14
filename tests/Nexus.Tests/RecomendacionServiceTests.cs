using Nexus.Core.Entities;
using Nexus.Core.Services;

namespace Nexus.Tests;

/// <summary>CP006 / RF007: solo se recomiendan recursos compatibles, disponibles y con capacidad.</summary>
public class RecomendacionServiceTests
{
    private readonly RecomendacionService _svc = new();
    private static readonly Dictionary<int, int> SinCarga = new();

    [Fact]
    public void RecursoNoDisponible_SeExcluye_YSeRecomiendaElCompatible()
    {
        var recursos = new[]
        {
            new Recurso { IdRecurso = 1, Nombre = "A", Capacidades = "Red", CapacidadMaxima = 3, Disponible = false },
            new Recurso { IdRecurso = 2, Nombre = "B", Capacidades = "Red, Hardware", CapacidadMaxima = 3 },
        };
        var r = _svc.Recomendar("Red", recursos, SinCarga);
        Assert.Single(r);
        Assert.Equal(2, r[0].Recurso.IdRecurso);
        Assert.NotEmpty(r[0].Criterios);
    }

    [Fact]
    public void SinRecursoAdecuado_DevuelveListaVacia()
    {
        var recursos = new[] { new Recurso { IdRecurso = 1, Capacidades = "Hardware", CapacidadMaxima = 2 } };
        Assert.Empty(_svc.Recomendar("Red", recursos, SinCarga));
    }

    [Fact]
    public void RecursoLleno_NoSeRecomienda()
    {
        var recursos = new[] { new Recurso { IdRecurso = 1, Capacidades = "Red", CapacidadMaxima = 2 } };
        Assert.Empty(_svc.Recomendar("Red", recursos, new Dictionary<int, int> { [1] = 2 }));
    }

    [Fact]
    public void SePrefiereElRecursoConMenosCarga()
    {
        var recursos = new[]
        {
            new Recurso { IdRecurso = 1, Capacidades = "Red", CapacidadMaxima = 4 },
            new Recurso { IdRecurso = 2, Capacidades = "Red", CapacidadMaxima = 4 },
        };
        var r = _svc.Recomendar("Red", recursos, new Dictionary<int, int> { [1] = 3, [2] = 1 });
        Assert.Equal(2, r[0].Recurso.IdRecurso);
    }
}

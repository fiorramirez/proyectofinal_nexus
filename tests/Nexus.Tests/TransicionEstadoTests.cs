using Nexus.Core.Entities;
using Nexus.Core.Services;

namespace Nexus.Tests;

/// <summary>CP007 / RF008: las transiciones de estado no permitidas se rechazan.</summary>
public class TransicionEstadoTests
{
    [Theory]
    [InlineData(EstadoIncidente.Nuevo, EstadoIncidente.Asignado)]
    [InlineData(EstadoIncidente.Asignado, EstadoIncidente.EnProceso)]
    [InlineData(EstadoIncidente.EnProceso, EstadoIncidente.Resuelto)]
    [InlineData(EstadoIncidente.Resuelto, EstadoIncidente.Cerrado)]
    public void TransicionesPermitidas(EstadoIncidente de, EstadoIncidente a) =>
        Assert.True(TransicionEstadoService.EsValida(de, a));

    [Theory]
    [InlineData(EstadoIncidente.Nuevo, EstadoIncidente.Cerrado)]
    [InlineData(EstadoIncidente.Nuevo, EstadoIncidente.Resuelto)]
    [InlineData(EstadoIncidente.Cerrado, EstadoIncidente.Nuevo)]
    public void TransicionesNoPermitidas(EstadoIncidente de, EstadoIncidente a) =>
        Assert.False(TransicionEstadoService.EsValida(de, a));
}

using Nexus.Core.Entities;

namespace Nexus.Core.Services;

/// <summary>RF008 / RNF002: rechaza transiciones de estado no permitidas.</summary>
public static class TransicionEstadoService
{
    private static readonly Dictionary<EstadoIncidente, EstadoIncidente[]> Permitidas = new()
    {
        [EstadoIncidente.Nuevo] = new[] { EstadoIncidente.Asignado },
        [EstadoIncidente.Asignado] = new[] { EstadoIncidente.EnProceso },
        [EstadoIncidente.EnProceso] = new[] { EstadoIncidente.Resuelto },
        [EstadoIncidente.Resuelto] = new[] { EstadoIncidente.Cerrado, EstadoIncidente.EnProceso }, // reapertura
        [EstadoIncidente.Cerrado] = Array.Empty<EstadoIncidente>(),
    };

    public static bool EsValida(EstadoIncidente actual, EstadoIncidente nuevo) =>
        Permitidas[actual].Contains(nuevo);
}

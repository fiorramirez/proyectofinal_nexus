namespace Nexus.Core.Services;

/// <summary>Regla de compatibilidad entre un recurso y la categoría de un incidente.</summary>
public static class Capacidades
{
    public static bool Atiende(string capacidades, string categoria) =>
        capacidades
            .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(c => string.Equals(c, categoria, StringComparison.OrdinalIgnoreCase));
}

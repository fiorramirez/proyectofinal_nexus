using System.ComponentModel.DataAnnotations;

namespace Nexus.Core.Entities;

/// <summary>Tabla RECURSO (persona o equipo que atiende incidentes).</summary>
public class Recurso
{
    public int IdRecurso { get; set; }
    [MaxLength(120)] public string Nombre { get; set; } = string.Empty;
    /// <summary>Categorías que puede atender, separadas por coma. Ej.: "Red, Hardware".</summary>
    [MaxLength(500)] public string Capacidades { get; set; } = string.Empty;
    public bool Disponible { get; set; } = true;
    /// <summary>Carga simultánea máxima; debe ser mayor que cero.</summary>
    public int CapacidadMaxima { get; set; }
    public bool Activo { get; set; } = true;
}

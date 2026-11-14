using System.ComponentModel.DataAnnotations;

namespace Nexus.Core.Entities;

/// <summary>Tabla ESCENARIO: supuestos y resultados de una simulación, separados de los datos reales.</summary>
public class Escenario
{
    public int IdEscenario { get; set; }
    public int IdCreador { get; set; }
    public Usuario? Creador { get; set; }
    [MaxLength(120)] public string Nombre { get; set; } = string.Empty;
    public string SupuestosJson { get; set; } = "{}";
    public string? ResultadosJson { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaEjecucion { get; set; }
}

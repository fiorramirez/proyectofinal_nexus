using System.ComponentModel.DataAnnotations;

namespace Nexus.Core.Entities;

/// <summary>Tabla REGLA_PRIORIDAD: pesos configurables del cálculo (RF005).</summary>
public class ReglaPrioridad
{
    public int IdRegla { get; set; }
    [MaxLength(100)] public string Nombre { get; set; } = string.Empty;
    public decimal PesoImpacto { get; set; }
    public decimal PesoUrgencia { get; set; }
    public decimal PesoAntiguedad { get; set; }
    public decimal PesoRecurrencia { get; set; }
    public bool Activa { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}

using System.ComponentModel.DataAnnotations;

namespace Nexus.Core.Entities;

/// <summary>Tabla HISTORIAL_INCIDENTE: auditoría de cambios (RF012).</summary>
public class HistorialIncidente
{
    public long IdHistorial { get; set; }
    public int IdIncidente { get; set; }
    public Incidente? Incidente { get; set; }
    public int IdUsuario { get; set; }
    public Usuario? Usuario { get; set; }
    public EstadoIncidente? EstadoAnterior { get; set; }
    public EstadoIncidente? EstadoNuevo { get; set; }
    [MaxLength(120)] public string Accion { get; set; } = string.Empty;
    [MaxLength(600)] public string? Observacion { get; set; }
    public DateTime FechaEvento { get; set; } = DateTime.UtcNow;
}

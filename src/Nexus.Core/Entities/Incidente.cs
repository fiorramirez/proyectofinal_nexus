using System.ComponentModel.DataAnnotations;

namespace Nexus.Core.Entities;

/// <summary>Tabla INCIDENTE.</summary>
public class Incidente
{
    public int IdIncidente { get; set; }
    [MaxLength(150)] public string Titulo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public int IdCategoria { get; set; }
    public Categoria? Categoria { get; set; }
    public int IdReportante { get; set; }
    public Usuario? Reportante { get; set; }
    [MaxLength(120)] public string AreaAfectada { get; set; } = string.Empty;
    /// <summary>Escala 1 (bajo) a 5 (crítico).</summary>
    public byte Impacto { get; set; }
    /// <summary>Escala 1 (baja) a 5 (inmediata).</summary>
    public byte Urgencia { get; set; }
    public decimal? PrioridadPuntaje { get; set; }
    public EstadoIncidente Estado { get; set; } = EstadoIncidente.Nuevo;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime FechaActualizacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaResolucion { get; set; }
}

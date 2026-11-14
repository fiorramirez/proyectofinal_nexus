using System.ComponentModel.DataAnnotations;

namespace Nexus.Core.Entities;

/// <summary>Tabla ASIGNACION: conserva reasignaciones históricas.</summary>
public class Asignacion
{
    public int IdAsignacion { get; set; }
    public int IdIncidente { get; set; }
    public Incidente? Incidente { get; set; }
    public int IdRecurso { get; set; }
    public Recurso? Recurso { get; set; }
    public int IdAsignador { get; set; }
    public Usuario? Asignador { get; set; }
    public DateTime FechaAsignacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaFin { get; set; }
    [MaxLength(500)] public string? Observacion { get; set; }
}

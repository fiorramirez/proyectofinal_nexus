using System.ComponentModel.DataAnnotations;

namespace Nexus.Core.Entities;

/// <summary>Tabla USUARIO.</summary>
public class Usuario
{
    public int IdUsuario { get; set; }
    [MaxLength(120)] public string Nombre { get; set; } = string.Empty;
    [MaxLength(180)] public string Correo { get; set; } = string.Empty;
    [MaxLength(255)] public string HashContrasena { get; set; } = string.Empty;
    public Rol Rol { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}

using System.ComponentModel.DataAnnotations;

namespace Nexus.Core.Entities;

/// <summary>Tabla CATEGORIA.</summary>
public class Categoria
{
    public int IdCategoria { get; set; }
    [MaxLength(80)] public string Nombre { get; set; } = string.Empty;
    [MaxLength(300)] public string? Descripcion { get; set; }
    public bool Activa { get; set; } = true;
}

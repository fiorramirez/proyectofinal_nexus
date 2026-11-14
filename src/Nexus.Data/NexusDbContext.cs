using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Nexus.Core.Entities;

namespace Nexus.Data;

/// <summary>
/// Capa de acceso a datos. Mapea las entidades al esquema de database/01_schema.sql
/// (tablas en MAYÚSCULAS y columnas en snake_case, como en el diccionario de datos).
/// </summary>
public class NexusDbContext : DbContext
{
    public NexusDbContext(DbContextOptions<NexusDbContext> options) : base(options) { }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Incidente> Incidentes => Set<Incidente>();
    public DbSet<Recurso> Recursos => Set<Recurso>();
    public DbSet<Asignacion> Asignaciones => Set<Asignacion>();
    public DbSet<ReglaPrioridad> ReglasPrioridad => Set<ReglaPrioridad>();
    public DbSet<Escenario> Escenarios => Set<Escenario>();
    public DbSet<HistorialIncidente> Historial => Set<HistorialIncidente>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Usuario>(e =>
        {
            e.ToTable("USUARIO");
            e.HasKey(x => x.IdUsuario);
            e.HasIndex(x => x.Correo).IsUnique();
            e.Property(x => x.Rol).HasConversion<string>().HasMaxLength(20).IsUnicode(false);
        });

        mb.Entity<Categoria>(e =>
        {
            e.ToTable("CATEGORIA");
            e.HasKey(x => x.IdCategoria);
            e.HasIndex(x => x.Nombre).IsUnique();
        });

        mb.Entity<Incidente>(e =>
        {
            e.ToTable("INCIDENTE");
            e.HasKey(x => x.IdIncidente);
            e.Property(x => x.PrioridadPuntaje).HasPrecision(6, 2);
            e.Property(x => x.Estado).HasConversion<string>().HasMaxLength(20).IsUnicode(false);
            e.HasOne(x => x.Categoria).WithMany().HasForeignKey(x => x.IdCategoria);
            e.HasOne(x => x.Reportante).WithMany().HasForeignKey(x => x.IdReportante);
        });

        mb.Entity<Recurso>(e =>
        {
            e.ToTable("RECURSO");
            e.HasKey(x => x.IdRecurso);
        });

        mb.Entity<Asignacion>(e =>
        {
            e.ToTable("ASIGNACION");
            e.HasKey(x => x.IdAsignacion);
            e.HasOne(x => x.Incidente).WithMany().HasForeignKey(x => x.IdIncidente);
            e.HasOne(x => x.Recurso).WithMany().HasForeignKey(x => x.IdRecurso);
            e.HasOne(x => x.Asignador).WithMany().HasForeignKey(x => x.IdAsignador);
        });

        mb.Entity<ReglaPrioridad>(e =>
        {
            e.ToTable("REGLA_PRIORIDAD");
            e.HasKey(x => x.IdRegla);
            e.Property(x => x.PesoImpacto).HasPrecision(5, 2);
            e.Property(x => x.PesoUrgencia).HasPrecision(5, 2);
            e.Property(x => x.PesoAntiguedad).HasPrecision(5, 2);
            e.Property(x => x.PesoRecurrencia).HasPrecision(5, 2);
        });

        mb.Entity<Escenario>(e =>
        {
            e.ToTable("ESCENARIO");
            e.HasKey(x => x.IdEscenario);
            e.HasOne(x => x.Creador).WithMany().HasForeignKey(x => x.IdCreador);
        });

        mb.Entity<HistorialIncidente>(e =>
        {
            e.ToTable("HISTORIAL_INCIDENTE");
            e.HasKey(x => x.IdHistorial);
            e.Property(x => x.EstadoAnterior).HasConversion<string>().HasMaxLength(20).IsUnicode(false);
            e.Property(x => x.EstadoNuevo).HasConversion<string>().HasMaxLength(20).IsUnicode(false);
            e.HasOne(x => x.Incidente).WithMany().HasForeignKey(x => x.IdIncidente);
            e.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.IdUsuario);
        });

        // Convención del diccionario de datos: columnas en snake_case y sin borrado en cascada.
        foreach (var entity in mb.Model.GetEntityTypes())
        {
            foreach (var prop in entity.GetProperties())
                prop.SetColumnName(ASnakeCase(prop.Name));
            foreach (var fk in entity.GetForeignKeys())
                fk.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }

    private static string ASnakeCase(string nombre) =>
        Regex.Replace(nombre, "([a-z0-9])([A-Z])", "$1_$2").ToLowerInvariant();
}

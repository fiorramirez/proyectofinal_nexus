using Nexus.Core.Entities;
using Nexus.Core.Services;
using Nexus.Data;

namespace Nexus.Api.Services;

/// <summary>
/// Datos SINTÉTICOS de demostración (solo en Development). No representan una universidad real.
/// Las cuentas y contraseñas son ficticias: cámbielas fuera del entorno de desarrollo.
/// </summary>
public static class DataSeeder
{
    public static void Sembrar(NexusDbContext db)
    {
        if (db.Usuarios.Any()) return;

        var admin = NuevoUsuario("Admin Demo", "admin@nexus.test", "Admin#2026", Rol.Administrador);
        var coord = NuevoUsuario("Coordinación Demo", "coordinador@nexus.test", "Coord#2026", Rol.Coordinador);
        var oper = NuevoUsuario("Operador Demo", "operador@nexus.test", "Oper#2026", Rol.Operador);
        db.Usuarios.AddRange(admin, coord, oper);

        var red = new Categoria { Nombre = "Red", Descripcion = "Conectividad, WiFi, switches" };
        var sw = new Categoria { Nombre = "Software", Descripcion = "Instalación y fallos de aplicaciones" };
        var hw = new Categoria { Nombre = "Hardware", Descripcion = "Equipos, impresoras, periféricos" };
        var cuentas = new Categoria { Nombre = "Cuentas y accesos", Descripcion = "Contraseñas, permisos, correo" };
        db.Categorias.AddRange(red, sw, hw, cuentas);

        db.Recursos.AddRange(
            new Recurso { Nombre = "Equipo de Redes", Capacidades = "Red", CapacidadMaxima = 3 },
            new Recurso { Nombre = "Soporte General", Capacidades = "Software, Hardware, Cuentas y accesos", CapacidadMaxima = 4 },
            new Recurso { Nombre = "Soporte de Laboratorios", Capacidades = "Hardware, Software", CapacidadMaxima = 2 });

        db.ReglasPrioridad.Add(new ReglaPrioridad
        {
            Nombre = "Regla inicial v1", PesoImpacto = 0.35m, PesoUrgencia = 0.30m,
            PesoAntiguedad = 0.20m, PesoRecurrencia = 0.15m, Activa = true,
        });
        db.SaveChanges();

        var ahora = DateTime.UtcNow;
        db.Incidentes.AddRange(
            Inc("WiFi caído en biblioteca", "No hay señal WiFi en el segundo piso de la biblioteca", red, admin, "Biblioteca", 4, 5, ahora.AddHours(-30)),
            Inc("Sin conexión WiFi biblioteca", "Estudiantes reportan que la señal WiFi de la biblioteca no funciona", red, admin, "Biblioteca", 4, 4, ahora.AddHours(-26)),
            Inc("Impresora no imprime", "La impresora del laboratorio 2 no responde", hw, admin, "Laboratorio 2", 2, 3, ahora.AddDays(-2)),
            Inc("Error al abrir el sistema de matrícula", "La aplicación cierra al iniciar sesión", sw, admin, "Registro", 5, 4, ahora.AddHours(-5)),
            Inc("Restablecer contraseña docente", "Docente no puede acceder a su correo institucional", cuentas, admin, "Facultad de Ingenierías", 2, 2, ahora.AddDays(-4)));
        db.SaveChanges();

        // Puntaje inicial de los incidentes de demostración (misma regla que usa la API).
        var regla = db.ReglasPrioridad.First(r => r.Activa);
        var prioridad = new PrioridadService();
        foreach (var i in db.Incidentes.ToList())
            i.PrioridadPuntaje = prioridad.Calcular(i, regla, ahora, 0).Puntaje;
        db.SaveChanges();
    }

    private static Usuario NuevoUsuario(string nombre, string correo, string clave, Rol rol) => new()
    {
        Nombre = nombre, Correo = correo, Rol = rol,
        HashContrasena = BCrypt.Net.BCrypt.HashPassword(clave),
    };

    private static Incidente Inc(string titulo, string desc, Categoria cat, Usuario rep, string area, byte imp, byte urg, DateTime creado) => new()
    {
        Titulo = titulo, Descripcion = desc, Categoria = cat, Reportante = rep, AreaAfectada = area,
        Impacto = imp, Urgencia = urg, FechaCreacion = creado, FechaActualizacion = creado,
    };
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nexus.Api.Dtos;
using Nexus.Core.Entities;
using Nexus.Data;

namespace Nexus.Api.Controllers;

/// <summary>RF002. Solo el administrador gestiona cuentas (validado en el servidor, RNF001).</summary>
[ApiController]
[Route("api/usuarios")]
[Authorize(Roles = nameof(Rol.Administrador))]
public class UsuariosController : ControllerBase
{
    private readonly NexusDbContext _db;
    public UsuariosController(NexusDbContext db) => _db = db;

    [HttpGet]
    public async Task<List<UsuarioDto>> Listar() =>
        await _db.Usuarios.AsNoTracking()
            .Select(u => new UsuarioDto(u.IdUsuario, u.Nombre, u.Correo, u.Rol.ToString(), u.Activo))
            .ToListAsync();

    [HttpPost]
    public async Task<ActionResult<UsuarioDto>> Crear(CrearUsuarioRequest req)
    {
        if (!Enum.TryParse<Rol>(req.Rol, true, out var rol))
            return BadRequest(new { error = "Rol no permitido." });
        if (await _db.Usuarios.AnyAsync(u => u.Correo == req.Correo))
            return Conflict(new { error = "Ya existe una cuenta con ese correo." });

        var u = new Usuario
        {
            Nombre = req.Nombre, Correo = req.Correo, Rol = rol,
            HashContrasena = BCrypt.Net.BCrypt.HashPassword(req.Contrasena),
        };
        _db.Usuarios.Add(u);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Listar), new UsuarioDto(u.IdUsuario, u.Nombre, u.Correo, u.Rol.ToString(), u.Activo));
    }

    [HttpPatch("{id:int}/activo")]
    public async Task<IActionResult> CambiarActivo(int id, [FromQuery] bool valor)
    {
        var u = await _db.Usuarios.FindAsync(id);
        if (u is null) return NotFound();
        u.Activo = valor;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nexus.Api.Dtos;
using Nexus.Api.Services;
using Nexus.Data;

namespace Nexus.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly NexusDbContext _db;
    private readonly JwtService _jwt;
    public AuthController(NexusDbContext db, JwtService jwt) { _db = db; _jwt = jwt; }

    /// <summary>RF001. Credenciales inválidas → 401 sin revelar si falló el correo o la contraseña.</summary>
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest req)
    {
        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Correo == req.Correo && u.Activo);
        if (usuario is null || !BCrypt.Net.BCrypt.Verify(req.Contrasena, usuario.HashContrasena))
            return Unauthorized(new { error = "Credenciales inválidas." });

        return new LoginResponse(_jwt.Generar(usuario), usuario.Nombre, usuario.Rol.ToString());
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nexus.Data;

namespace Nexus.Api.Controllers;

[ApiController]
[Route("api/categorias")]
[Authorize]
public class CategoriasController : ControllerBase
{
    private readonly NexusDbContext _db;
    public CategoriasController(NexusDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Listar() =>
        Ok(await _db.Categorias.AsNoTracking().Where(c => c.Activa)
            .OrderBy(c => c.Nombre)
            .Select(c => new { id = c.IdCategoria, nombre = c.Nombre })
            .ToListAsync());
}

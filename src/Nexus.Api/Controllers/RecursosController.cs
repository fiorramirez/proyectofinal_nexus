using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexus.Api.Dtos;
using Nexus.Api.Services;
using Nexus.Core.Entities;
using Nexus.Data;

namespace Nexus.Api.Controllers;

/// <summary>RF006: administración de recursos operativos.</summary>
[ApiController]
[Route("api/recursos")]
[Authorize]
public class RecursosController : ControllerBase
{
    private readonly IncidenteAppService _svc;
    private readonly NexusDbContext _db;
    public RecursosController(IncidenteAppService svc, NexusDbContext db) { _svc = svc; _db = db; }

    [HttpGet]
    public async Task<List<RecursoDto>> Listar() => await _svc.RecursosAsync();

    [HttpPost]
    [Authorize(Roles = nameof(Rol.Coordinador))]
    public async Task<ActionResult<RecursoDto>> Crear(CrearRecursoRequest req)
    {
        var r = new Recurso
        {
            Nombre = req.Nombre.Trim(), Capacidades = req.Capacidades.Trim(),
            CapacidadMaxima = req.CapacidadMaxima, Disponible = req.Disponible,
        };
        _db.Recursos.Add(r);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Listar), new RecursoDto(r.IdRecurso, r.Nombre, r.Capacidades, r.Disponible, r.CapacidadMaxima, 0));
    }
}

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexus.Api.Dtos;
using Nexus.Api.Services;
using Nexus.Core.Services;

namespace Nexus.Api.Controllers;

[ApiController]
[Route("api/incidentes")]
[Authorize]
public class IncidentesController : ControllerBase
{
    private readonly IncidenteAppService _svc;
    public IncidentesController(IncidenteAppService svc) => _svc = svc;

    private int IdUsuario => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private ActionResult Responder<T>(Resultado<T> r) =>
        r.Ok ? Ok(r.Valor) : StatusCode(r.Status, new { error = r.Error });

    /// <summary>RF003 + RF005: lista ordenada por prioridad, con los factores de cada puntaje.</summary>
    [HttpGet]
    public async Task<List<IncidenteDto>> Listar() => await _svc.ListarAsync();

    /// <summary>RF003 + RF004: registra y devuelve coincidencias potenciales (sin fusionar nada).</summary>
    [HttpPost]
    [Authorize(Roles = "Coordinador,Operador")]
    public async Task<ActionResult> Crear(CrearIncidenteRequest req) => Responder(await _svc.CrearAsync(req, IdUsuario));

    [HttpGet("{id:int}/coincidencias")]
    public async Task<ActionResult> Coincidencias(int id) => Responder(await _svc.CoincidenciasAsync(id));

    /// <summary>RF007: recursos recomendados con los criterios usados.</summary>
    [HttpGet("{id:int}/recomendaciones")]
    [Authorize(Roles = "Coordinador,Administrador")]
    public async Task<ActionResult> Recomendaciones(int id) => Responder(await _svc.RecomendarAsync(id));

    /// <summary>RF008: confirma una asignación (valida compatibilidad, disponibilidad y capacidad).</summary>
    [HttpPost("{id:int}/asignar")]
    [Authorize(Roles = "Coordinador")]
    public async Task<ActionResult> Asignar(int id, AsignarRequest req) => Responder(await _svc.AsignarAsync(id, req, IdUsuario));

    /// <summary>RF008: cambia el estado; rechaza transiciones no permitidas.</summary>
    [HttpPut("{id:int}/estado")]
    [Authorize(Roles = "Coordinador,Operador")]
    public async Task<ActionResult> CambiarEstado(int id, CambiarEstadoRequest req) =>
        Responder(await _svc.CambiarEstadoAsync(id, req, IdUsuario));
}

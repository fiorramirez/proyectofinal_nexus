using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexus.Api.Dtos;
using Nexus.Api.Services;
using Nexus.Core.Entities;

namespace Nexus.Api.Controllers;

/// <summary>RF010 y RF011. Las simulaciones no modifican incidentes ni asignaciones reales.</summary>
[ApiController]
[Route("api/simulaciones")]
[Authorize(Roles = nameof(Rol.Coordinador))]
public class SimulacionesController : ControllerBase
{
    private readonly SimulacionAppService _svc;
    public SimulacionesController(SimulacionAppService svc) => _svc = svc;

    private int IdUsuario => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost("ejecutar")]
    public async Task<SimulacionResponse> Ejecutar(SimulacionRequest req) => await _svc.EjecutarAsync(req, IdUsuario);

    [HttpPost("comparar")]
    public async Task<List<SimulacionResponse>> Comparar(ComparacionRequest req) => await _svc.CompararAsync(req, IdUsuario);
}

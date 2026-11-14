using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexus.Api.Dtos;
using Nexus.Api.Services;

namespace Nexus.Api.Controllers;

/// <summary>RF009: indicadores calculados a partir de los registros existentes.</summary>
[ApiController]
[Route("api/panel")]
[Authorize(Roles = "Coordinador,Administrador")]
public class PanelController : ControllerBase
{
    private readonly IncidenteAppService _svc;
    public PanelController(IncidenteAppService svc) => _svc = svc;

    [HttpGet]
    public async Task<PanelDto> Obtener() => await _svc.PanelAsync();
}

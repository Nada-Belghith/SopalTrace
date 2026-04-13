using Microsoft.AspNetCore.Mvc;
using SopalTrace.Application.Interfaces;
using System.Threading.Tasks;

namespace SopalTrace.Api.Controllers;

[Route("api/referentiels")]
[ApiController]
public class ReferentielController : ControllerBase
{
    private readonly IReferentielService _service;

    public ReferentielController(IReferentielService service)
    {
        _service = service;
    }

    [HttpGet("fabrication")]
    public async Task<IActionResult> GetDictionnairesFabrication()
    {
        var data = await _service.GetFabricationReferentielsAsync();
        return Ok(new { success = true, data });
    }
}
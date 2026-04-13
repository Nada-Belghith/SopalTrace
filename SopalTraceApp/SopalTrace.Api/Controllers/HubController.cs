using Microsoft.AspNetCore.Mvc;
using SopalTrace.Application.Interfaces;
using System.Threading.Tasks;

namespace SopalTrace.Api.Controllers;

[Route("api/hub")]
[ApiController]
public class HubController : ControllerBase
{
    private readonly IHubService _service;

    public HubController(IHubService service)
    {
        _service = service;
    }

    [HttpGet("modeles")]
    public async Task<IActionResult> GetTousLesModeles()
    {
        var data = await _service.GetTousLesModelesAsync();
        return Ok(new { success = true, data });
    }
}
using Microsoft.AspNetCore.Mvc;
using SopalTrace.Application.Interfaces;
using SopalTrace.Infrastructure.Services;
using System.Threading.Tasks;

namespace SopalTrace.Api.Controllers;

[Route("api/referentiels")]
[ApiController]
public class ReferentielController : ControllerBase
{
    private readonly IReferentielService _referentielService;

    public ReferentielController(IReferentielService referentielService)
    {
        _referentielService = referentielService;
    }

    [HttpGet("fabrication")]
    public async Task<IActionResult> GetDictionnairesFabrication()
    {
        var data = await _referentielService.GetFabricationReferentielsAsync();
        return Ok(new { success = true, data });
    }
    [HttpGet("article/{codeArticle}")]
    public async Task<IActionResult> GetArticle(string codeArticle)
    {
        if (string.IsNullOrWhiteSpace(codeArticle))
            return BadRequest("Le code article est requis.");

        var article = await _referentielService.GetArticleInfosAsync(codeArticle);

        if (article == null)
            return NotFound($"Aucun article trouvé pour le code '{codeArticle}' dans l'ERP.");

        return Ok(article);
    }
}
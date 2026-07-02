using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SopalTrace.Application.Alertes;
using SopalTrace.Application.Interfaces;

namespace SopalTrace.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AlertesController : ControllerBase
{
    private readonly AlerteService<PlanManquantContexte> _alertePlanManquantService;
    private readonly IUserRepository _userRepository;

    public AlertesController(
        AlerteService<PlanManquantContexte> alertePlanManquantService,
        IUserRepository userRepository)
    {
        _alertePlanManquantService = alertePlanManquantService;
        _userRepository = userRepository;
    }

    [HttpPost("plan-manquant")]
    public async Task<IActionResult> SignalerPlanManquant([FromBody] PlanManquantContexte request)
    {
        if (string.IsNullOrWhiteSpace(request.PosteCode) || string.IsNullOrWhiteSpace(request.ArticleCode))
        {
            return BadRequest("Le poste et l'article sont obligatoires.");
        }

        // Toujours récupérer les infos de l'opérateur connecté depuis la base de données
        // via le claim "matricule" présent dans le token JWT
        var matricule = User.FindFirst("matricule")?.Value;
        if (!string.IsNullOrEmpty(matricule))
        {
            var user = await _userRepository.GetUserByMatriculeAsync(matricule);
            if (user != null)
            {
                request.NomOperateur = user.NomComplet;
                request.EmailOperateur = user.Email;
            }
        }

        // Fallback si l'utilisateur est introuvable
        if (string.IsNullOrWhiteSpace(request.NomOperateur))
            request.NomOperateur = "Opérateur Inconnu";

        try
        {
            await _alertePlanManquantService.DeclencherAsync(request);
            return Ok(new { message = "L'alerte de plan manquant a été déclenchée avec succès." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/resoudre")]
    [Authorize(Roles = "RESPONSABLE_DI")]
    public async Task<IActionResult> ResoudreAlerte(Guid id)
    {
        // Récupérer le nom complet du responsable depuis la base de données
        var matricule = User.FindFirst("matricule")?.Value;
        string nomResoluteur = "Responsable Inconnu";
        if (!string.IsNullOrEmpty(matricule))
        {
            var user = await _userRepository.GetUserByMatriculeAsync(matricule);
            if (user != null) nomResoluteur = user.NomComplet;
        }

        await _alertePlanManquantService.ResoudreAsync(id, nomResoluteur);
        return Ok(new { message = "L'alerte a été marquée comme résolue." });
    }
}

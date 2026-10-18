using SopalTrace.Domain.Constants;
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
        // L'identité du déclarant provient toujours du jeton, jamais de la requête.
        request.NomOperateur = "Opérateur Inconnu";
        request.EmailOperateur = null;

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

        await _alertePlanManquantService.DeclencherAsync(request);
        return Ok(new { message = "L'alerte de plan manquant a été déclenchée avec succès." });
    }

    [HttpPost("{id:guid}/resoudre")]
    [Authorize(Roles = RolesApp.ResponsableDI)]
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

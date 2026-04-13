using Microsoft.AspNetCore.Mvc;
using SopalTrace.Application.DTOs.QualityPlans.PlanAssemblage;
using SopalTrace.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SopalTrace.Api.Controllers;

/// <summary>
/// Contrôleur pour la gestion des plans d'assemblage (Plans Maître et Exceptions)
/// Permet la création, modification, versioning et consultation des plans
/// </summary>
[ApiController]
[Route("api/plans-assemblage")]
public class PlanAssemblageController : ControllerBase
{
    private readonly IPlanAssService _service;

    public PlanAssemblageController(IPlanAssService service)
    {
        _service = service;
    }

    /// <summary>
    /// Crée un nouveau plan d'assemblage (maître ou exception)
    /// </summary>
    /// <param name="req">Données de création du plan</param>
    /// <returns>ID du plan créé</returns>
    /// <response code="200">Plan créé avec succès</response>
    /// <response code="400">Requête invalide (validation échouée)</response>
    /// <response code="409">Conflit: un plan actif existe déjà</response>
    [HttpPost]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> Create([FromBody] CreatePlanAssRequestDto req)
    {
        var id = await _service.CreerPlanAsync(req, "ADMIN");
        return Ok(new { success = true, id });
    }

    /// <summary>
    /// Met à jour l'arborescence complète des sections et lignes d'un plan
    /// Active automatiquement le plan après synchronisation
    /// </summary>
    /// <param name="id">ID du plan à mettre à jour</param>
    /// <param name="sections">Sections modifiées (nouvelles et existantes)</param>
    /// <returns>Résultat de la synchronisation</returns>
    /// <response code="200">Plan synchronisé et activé avec succès</response>
    /// <response code="404">Plan non trouvé</response>
    /// <response code="400">Requête invalide</response>
    [HttpPut("{id}/valeurs")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> UpdateFullTree(Guid id, [FromBody] List<SectionAssEditDto> sections)
    {
        var ok = await _service.MettreAJourValeursPlanAsync(id, sections);
        if (!ok) return NotFound(new { success = false, message = "Plan non trouvé" });
        return Ok(new { success = true, message = "Plan synchronisé et activé." });
    }

    /// <summary>
    /// Récupère un plan complet avec toutes ses sections et lignes
    /// </summary>
    /// <param name="id">ID du plan à récupérer</param>
    /// <returns>Données complètes du plan</returns>
    /// <response code="200">Plan récupéré avec succès</response>
    /// <response code="404">Plan non trouvé</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(PlanAssResponseDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Get(Guid id)
    {
        var plan = await _service.GetPlanByIdAsync(id);
        return Ok(plan);
    }

    /// <summary>
    /// Crée une nouvelle version d'un plan existant
    /// </summary>
    /// <param name="request">Données de la nouvelle version</param>
    /// <returns>ID de la nouvelle version créée</returns>
    /// <response code="200">Nouvelle version créée avec succès</response>
    /// <response code="400">Requête invalide (validation échouée)</response>
    /// <response code="404">Plan non trouvé</response>
    [HttpPost("nouvelle-version")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> NouvelleVersion([FromBody] NouvelleVersionAssRequestDto request)
    {
        var id = await _service.CreerNouvelleVersionPlanAsync(request);
        return Ok(new { success = true, planId = id, message = "Nouvelle version créée en BROUILLON." });
    }
}
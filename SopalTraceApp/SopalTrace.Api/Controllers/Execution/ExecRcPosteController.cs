using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SopalTrace.Api.Extensions;
using SopalTrace.Application.DTOs.Execution;
using SopalTrace.Application.Interfaces;
using SopalTrace.Domain.Constants;

namespace SopalTrace.API.Controllers.Execution
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = RolesApp.Admin + "," + RolesApp.Operateur)]
    public class ExecRcPosteController : ControllerBase
    {
        private readonly IExecRcPosteService _execRcPosteService;

        public ExecRcPosteController(IExecRcPosteService execRcPosteService)
        {
            _execRcPosteService = execRcPosteService;
        }

        [HttpGet("{execControleOfId:guid}")]
        public async Task<IActionResult> GetPlanPourOf(
            [FromRoute] Guid execControleOfId,
            [FromQuery, Required, RegularExpression(FormatsEntree.Code, ErrorMessage = FormatsEntree.MessageCodeInvalide)] string posteCode,
            [FromQuery, Required, StringLength(20)] string equipe)
        {
            var result = await _execRcPosteService.GetPlanPourOfAsync(execControleOfId, posteCode, equipe);
            if (result == null) return NotFound(new { message = "Plan non initialisé pour ce poste." });
            return Ok(result);
        }

        [HttpGet("statut/{statutId:guid}")]
        public async Task<IActionResult> GetPlanByStatutId([FromRoute] Guid statutId)
        {
            var result = await _execRcPosteService.GetPlanByStatutIdAsync(statutId);
            if (result == null) return NotFound(new { message = "Plan non trouvé." });
            return Ok(result);
        }

        [HttpPut("{execControleDocumentStatutId:guid}")]
        public async Task<IActionResult> UpdatePlan([FromRoute] Guid execControleDocumentStatutId, [FromBody] ExecRcPosteDto request)
        {
            return Ok(await _execRcPosteService.UpdatePlanAsync(execControleDocumentStatutId, request, User.GetMatricule()));
        }

        /// <summary>L'équipe en production valide ses heures sur le document du jour (partagé par les équipes).</summary>
        [HttpPost("{execControleDocumentStatutId:guid}/valider-equipe")]
        public async Task<IActionResult> ValiderEquipe([FromRoute] Guid execControleDocumentStatutId)
        {
            return Ok(await _execRcPosteService.ValiderEquipeAsync(execControleDocumentStatutId, User.GetMatricule()));
        }

        [HttpPost("{execControleOfId:guid}/nouveau-document")]
        public async Task<IActionResult> CreerNouveauDocument([FromRoute] Guid execControleOfId, [FromBody] CreerRcPosteRequest request)
        {
            var result = await _execRcPosteService.CreerNouveauDocumentAsync(execControleOfId, request.PosteCode, request.DateExecution, request.Equipe, User.GetMatricule());
            return Ok(result);
        }

        [HttpPost("{execControleOfId:guid}/cloturer-tous")]
        public async Task<IActionResult> CloturerTous(
            [FromRoute] Guid execControleOfId,
            [FromQuery, Required, RegularExpression(FormatsEntree.Code, ErrorMessage = FormatsEntree.MessageCodeInvalide)] string posteCode)
        {
            return Ok(await _execRcPosteService.CloturerTousLesDocumentsAsync(execControleOfId, posteCode));
        }
    }

    public class CreerRcPosteRequest
    {
        [Required, RegularExpression(FormatsEntree.Code, ErrorMessage = FormatsEntree.MessageCodeInvalide)]
        public string PosteCode { get; set; } = null!;

        public DateTime DateExecution { get; set; }

        [Required, StringLength(20)]
        public string Equipe { get; set; } = null!;
    }
}

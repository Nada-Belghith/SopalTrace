using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SopalTrace.Application.DTOs.Execution;
using SopalTrace.Application.Interfaces;

namespace SopalTrace.API.Controllers.Execution
{
    [ApiController]
    [Route("api/[controller]")]
    public class ExecRcPosteController : ControllerBase
    {
        private readonly IExecRcPosteService _execRcPosteService;

        public ExecRcPosteController(IExecRcPosteService execRcPosteService)
        {
            _execRcPosteService = execRcPosteService;
        }

        [HttpGet("{execControleOfId}")]
        public async Task<IActionResult> GetPlanPourOf([FromRoute] Guid execControleOfId, [FromQuery] string posteCode, [FromQuery] string equipe)
        {
            try
            {
                var result = await _execRcPosteService.GetPlanPourOfAsync(execControleOfId, posteCode, equipe);
                if (result == null) return NotFound(new { message = "Plan non initialisé pour ce poste." });
                return Ok(result); 
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("statut/{statutId}")]
        public async Task<IActionResult> GetPlanByStatutId([FromRoute] Guid statutId)
        {
            try
            {
                var result = await _execRcPosteService.GetPlanByStatutIdAsync(statutId);
                if (result == null) return NotFound(new { message = "Plan non trouvé." });
                return Ok(result); 
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{execControleDocumentStatutId}")]
        public async Task<IActionResult> UpdatePlan([FromRoute] Guid execControleDocumentStatutId, [FromBody] ExecRcPosteDto request)
        {
            try
            {
                var matriculeOperateur = User.FindFirst("matricule")?.Value;
                var result = await _execRcPosteService.UpdatePlanAsync(execControleDocumentStatutId, request, matriculeOperateur);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{execControleOfId}/nouveau-document")]
        public async Task<IActionResult> CreerNouveauDocument([FromRoute] Guid execControleOfId, [FromBody] CreerRcPosteRequest request)
        {
            try
            {
                var matriculeOperateur = User.FindFirst("matricule")?.Value ?? "";
                var result = await _execRcPosteService.CreerNouveauDocumentAsync(execControleOfId, request.PosteCode, request.DateExecution, request.Equipe, matriculeOperateur);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{execControleOfId}/cloturer-tous")]
        public async Task<IActionResult> CloturerTous([FromRoute] Guid execControleOfId, [FromQuery] string posteCode)
        {
            try
            {
                var result = await _execRcPosteService.CloturerTousLesDocumentsAsync(execControleOfId, posteCode);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }

    public class CreerRcPosteRequest
    {
        public string PosteCode { get; set; }
        public DateTime DateExecution { get; set; }
        public string Equipe { get; set; }
    }
}

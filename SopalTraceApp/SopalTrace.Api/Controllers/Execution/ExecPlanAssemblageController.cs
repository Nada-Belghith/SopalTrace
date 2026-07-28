using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SopalTrace.Application.DTOs.Execution;
using SopalTrace.Application.Interfaces.Execution;
using System;
using System.Threading.Tasks;

namespace SopalTrace.Api.Controllers.Execution
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ExecPlanAssemblageController : ControllerBase
    {
        private readonly IExecPlanAssemblageService _service;

        public ExecPlanAssemblageController(IExecPlanAssemblageService service)
        {
            _service = service;
        }

        [HttpGet("{execControleOfId}")]
        public async Task<ActionResult<ExecPlanAssemblageDto>> GetExecution(Guid execControleOfId, [FromQuery] string? posteCode = null)
        {
            try
            {
                var result = await _service.GetOrInitExecutionAsync(execControleOfId, posteCode);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.InnerException != null ? $"{ex.Message} -> Détail : {ex.InnerException.Message}" : ex.Message });
            }
        }

        [HttpGet("{execControleOfId}/verifier-documents")]
        public async Task<ActionResult<object>> VerifierDocuments(Guid execControleOfId, [FromQuery] string? posteCode = null)
        {
            try
            {
                var result = await _service.VerifierDocumentsAsync(execControleOfId, posteCode);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.InnerException != null ? $"{ex.Message} -> Détail : {ex.InnerException.Message}" : ex.Message });
            }
        }

        [HttpPost("{execControleOfId}/config-horaires")]
        public async Task<ActionResult<ExecPlanAssemblageDto>> ConfigurerHoraires(Guid execControleOfId, [FromBody] ConfigHeuresPosteRequest request)
        {
            try
            {
                var result = await _service.ConfigurerHorairesAsync(execControleOfId, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.InnerException != null ? $"{ex.Message} -> Détail : {ex.InnerException.Message}" : ex.Message });
            }
        }

        [HttpPost("{execControleOfId}/resultat")]
        public async Task<ActionResult> SaveResultat(Guid execControleOfId, [FromBody] SaveResultatAssRequest request)
        {
            try
            {
                var success = await _service.SaveResultatAsync(execControleOfId, request);
                return Ok(new { success });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.InnerException != null ? $"{ex.Message} -> Détail : {ex.InnerException.Message}" : ex.Message });
            }
        }

        [HttpPost("{execControleOfId}/cloturer")]
        public async Task<ActionResult> Cloturer(Guid execControleOfId)
        {
            try
            {
                var success = await _service.CloturerExecutionAsync(execControleOfId);
                return Ok(new { success });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.InnerException != null ? $"{ex.Message} -> Détail : {ex.InnerException.Message}" : ex.Message });
            }
        }
    }
}

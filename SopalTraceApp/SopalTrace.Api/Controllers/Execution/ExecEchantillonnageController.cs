using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SopalTrace.Application.Services;

namespace SopalTrace.API.Controllers.Execution
{
    [ApiController]
    [Route("api/[controller]")]
    // [Authorize] // Uncomment if authentication is required globally
    public class ExecEchantillonnageController : ControllerBase
    {
        private readonly IExecEchantillonnageService _execEchantillonnageService;

        public ExecEchantillonnageController(IExecEchantillonnageService execEchantillonnageService)
        {
            _execEchantillonnageService = execEchantillonnageService;
        }

        [HttpPost("init/{execControleOfId}")]
        public async Task<IActionResult> InitPlanPourOf([FromRoute] Guid execControleOfId, [FromQuery] string? posteCode, [FromQuery] int? nbPostes = null)
        {
            try
            {
                var result = await _execEchantillonnageService.InitPlanPourOfAsync(execControleOfId, posteCode, nbPostes);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{execControleOfId}")]
        public async Task<IActionResult> GetPlanPourOf([FromRoute] Guid execControleOfId, [FromQuery] string? posteCode)
        {
            try
            {
                var result = await _execEchantillonnageService.GetPlanPourOfAsync(execControleOfId, posteCode);
                if (result == null) return NotFound("Plan non initialisé pour cet OF.");
                return Ok(result); 
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("check-source/{execControleOfId}")]
        public async Task<IActionResult> CheckSourcePlan([FromRoute] Guid execControleOfId)
        {
            try
            {
                var exists = await _execEchantillonnageService.SourcePlanExistsAsync(execControleOfId);
                return Ok(new { exists });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}

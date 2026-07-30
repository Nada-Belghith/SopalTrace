using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SopalTrace.Application.DTOs.Tracabilite;
using SopalTrace.Application.Interfaces.Execution;
using SopalTrace.Domain.Constants;

namespace SopalTrace.Api.Controllers
{
    [Authorize(Roles = RolesApp.Admin + "," + RolesApp.Operateur)]
    [Route("api/Operateur/tracabilite")]
    [ApiController]
    public class TracabiliteController : ControllerBase
    {
        private readonly ITracabiliteService _tracabiliteService;

        public TracabiliteController(ITracabiliteService tracabiliteService)
        {
            _tracabiliteService = tracabiliteService;
        }

        [HttpGet("{execControleOfId}")]
        public async Task<IActionResult> GetRegistre(Guid execControleOfId)
        {
            try
            {
                var result = await _tracabiliteService.GetRegistreTracabiliteAsync(execControleOfId);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("ligne")]
        public async Task<IActionResult> AddLigne([FromBody] AddRegistreTracabiliteDto dto)
        {
            try
            {
                if (dto == null || dto.ExecControleOfId == Guid.Empty)
                {
                    return BadRequest(new { success = false, message = "Données invalides." });
                }

                var result = await _tracabiliteService.AddLigneTracabiliteAsync(dto);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("validate-lot")]
        public async Task<IActionResult> ValidateLot([FromQuery] string type, [FromQuery] string lot)
        {
            try
            {
                var isValid = await _tracabiliteService.ValidateLotAsync(type, lot);
                return Ok(new { success = true, isValid });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("search-lots")]
        public async Task<IActionResult> SearchLots([FromQuery] string type, [FromQuery] string query = "")
        {
            try
            {
                var lots = await _tracabiliteService.SearchLotsAsync(type, query ?? "");
                return Ok(new { success = true, data = lots });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("{execControleOfId}/cloturer")]
        public async Task<IActionResult> CloturerRegistre(Guid execControleOfId, [FromQuery] string posteCode)
        {
            try
            {
                var matricule = User.Claims.FirstOrDefault(c => c.Type == "Matricule")?.Value ?? "SYSTEM";
                var result = await _tracabiliteService.CloturerRegistreAsync(execControleOfId, posteCode, matricule);
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpDelete("ligne/{ligneId}")]
        public async Task<IActionResult> DeleteLigne(Guid ligneId)
        {
            try
            {
                var result = await _tracabiliteService.DeleteLigneTracabiliteAsync(ligneId);
                if (!result)
                {
                    return NotFound(new { success = false, message = "Ligne non trouvée." });
                }
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }
}

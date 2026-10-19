using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SopalTrace.Api.Extensions;
using SopalTrace.Application.DTOs.Execution;
using SopalTrace.Application.Services;
using SopalTrace.Domain.Constants;

namespace SopalTrace.API.Controllers.Execution
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = RolesApp.Admin + "," + RolesApp.Operateur)]
    public class ExecEchantillonnageController : ControllerBase
    {
        private readonly IExecEchantillonnageService _execEchantillonnageService;

        public ExecEchantillonnageController(IExecEchantillonnageService execEchantillonnageService)
        {
            _execEchantillonnageService = execEchantillonnageService;
        }

        [HttpPost("init/{execControleOfId:guid}")]
        public async Task<IActionResult> InitPlanPourOf([FromRoute] Guid execControleOfId, [FromBody] InitEchantillonnageRequest request)
        {
            request.MatriculeOperateur = User.GetMatricule();
            return Ok(await _execEchantillonnageService.InitPlanPourOfAsync(execControleOfId, request));
        }

        [HttpGet("{execControleOfId:guid}")]
        public async Task<IActionResult> GetPlanPourOf(
            [FromRoute] Guid execControleOfId,
            [FromQuery, RegularExpression(FormatsEntree.Code, ErrorMessage = FormatsEntree.MessageCodeInvalide)] string? posteCode)
        {
            var result = await _execEchantillonnageService.GetPlanPourOfAsync(execControleOfId, posteCode);
            if (result == null) return NotFound(new { message = "Plan non initialisé pour cet OF." });
            return Ok(result);
        }

        [HttpGet("check-source/{execControleOfId:guid}")]
        public async Task<IActionResult> CheckSourcePlan([FromRoute] Guid execControleOfId)
        {
            var exists = await _execEchantillonnageService.SourcePlanExistsAsync(execControleOfId);
            return Ok(new { exists });
        }

    }
}

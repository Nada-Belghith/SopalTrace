using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SopalTrace.Api.Extensions;
using SopalTrace.Application.DTOs.Execution;
using SopalTrace.Application.Interfaces.Execution;
using SopalTrace.Domain.Constants;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace SopalTrace.Api.Controllers.Execution
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = RolesApp.Admin + "," + RolesApp.Operateur)]
    public class ExecPlanAssemblageController : ControllerBase
    {
        private readonly IExecPlanAssemblageService _service;

        public ExecPlanAssemblageController(IExecPlanAssemblageService service)
        {
            _service = service;
        }

        [HttpGet("{execControleOfId:guid}")]
        public async Task<ActionResult<ExecPlanAssemblageDto>> GetExecution(
            Guid execControleOfId,
            [FromQuery, Required, RegularExpression(FormatsEntree.Code, ErrorMessage = FormatsEntree.MessageCodeInvalide)] string posteCode)
        {
            return Ok(await _service.GetOrInitExecutionAsync(execControleOfId, posteCode));
        }

        [HttpPost("{execControleOfId:guid}/resultat")]
        public async Task<IActionResult> SaveResultat(Guid execControleOfId, [FromBody] SaveResultatAssRequest request)
        {
            await _service.SaveResultatAsync(execControleOfId, request, User.GetMatricule());
            return NoContent();
        }
    }
}

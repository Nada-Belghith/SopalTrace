using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SopalTrace.Application.DTOs.Execution;
using SopalTrace.Application.Interfaces.Execution;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using SopalTrace.Api.Extensions;

using SopalTrace.Domain.Constants;

namespace SopalTrace.Api.Controllers.Execution
{
    [Authorize(Roles = RolesApp.Admin + "," + RolesApp.Operateur + "," + RolesApp.SuperviseurQualite)]
    [ApiController]
    [Route("api/exec/encf")]
    public class ExecEncfController : ControllerBase
    {
        private readonly IExecEncfService _execEncfService;

        public ExecEncfController(IExecEncfService execEncfService)
        {
            _execEncfService = execEncfService;
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _execEncfService.GetExecEncfAsync(id);
            if (result == null) return NotFound(new { message = "Exécution introuvable." });
            return Ok(result);
        }

        [HttpGet("by-of/{numeroOf}/poste/{posteCode}")]
        public async Task<IActionResult> GetOrCreateByOf(
            [RegularExpression(FormatsEntree.Code, ErrorMessage = FormatsEntree.MessageCodeInvalide)] string numeroOf,
            [RegularExpression(FormatsEntree.Code, ErrorMessage = FormatsEntree.MessageCodeInvalide)] string posteCode)
        {
            return Ok(await _execEncfService.GetOrCreateExecEncfByOfAsync(numeroOf, posteCode, User.GetMatricule()));
        }

        [HttpPost]
        public async Task<IActionResult> Save([FromBody] ExecEncfDto dto)
        {
            return Ok(await _execEncfService.SaveExecEncfAsync(dto, User.GetMatricule()));
        }
    }
}

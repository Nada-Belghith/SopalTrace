using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SopalTrace.Application.DTOs.Execution;
using SopalTrace.Application.Interfaces.Execution;
using System;
using System.Threading.Tasks;

namespace SopalTrace.Api.Controllers.Execution
{
    [ApiController]
    [Route("api/exec/encf")]
    public class ExecEncfController : ControllerBase
    {
        private readonly IExecEncfService _execEncfService;

        public ExecEncfController(IExecEncfService execEncfService)
        {
            _execEncfService = execEncfService;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                var result = await _execEncfService.GetExecEncfAsync(id);
                if (result == null) return NotFound();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("by-of/{numeroOf}/poste/{posteCode}")]
        public async Task<IActionResult> GetOrCreateByOf(string numeroOf, string posteCode)
        {
            try
            {
                var result = await _execEncfService.GetOrCreateExecEncfByOfAsync(numeroOf, posteCode);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Save([FromBody] ExecEncfDto dto)
        {
            try
            {
                var result = await _execEncfService.SaveExecEncfAsync(dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SopalTrace.Application.DTOs.QualityPlans.PlanRCCF;
using SopalTrace.Application.Interfaces;
using SopalTrace.Application.Services;

namespace SopalTrace.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PlanRccfController : ControllerBase
    {
        private readonly IPlanRccfService _service;
        private readonly IExcelImportRccfService _excelService;

        public PlanRccfController(IPlanRccfService service, IExcelImportRccfService excelService)
        {
            _service = service;
            _excelService = excelService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] bool includeArchived = false)
        {
            var plans = await _service.GetAllAsync(includeArchived);
            return Ok(plans);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var plan = await _service.GetByIdAsync(id);
            if (plan == null) return NotFound();
            return Ok(plan);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePlanRccfRequest request)
        {
            var matricule = "11111"; // En dur pour l'instant (comme ControlePoste)
            var plan = await _service.CreateAsync(request, matricule);
            return CreatedAtAction(nameof(GetById), new { id = plan.Id }, plan);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePlanRccfRequest request)
        {
            var matricule = "11111"; // En dur pour l'instant
            var plan = await _service.UpdateAsync(id, request, matricule);
            return Ok(plan);
        }

        [HttpPost("{id}/archive")]
        public async Task<IActionResult> Archive(Guid id)
        {
            var matricule = "11111"; // En dur pour l'instant
            var plan = await _service.ArchiveAsync(id, matricule);
            return Ok(plan);
        }

        [HttpPost("{id}/nouvelle-version")]
        public async Task<IActionResult> CreateNewVersion(Guid id)
        {
            var matricule = "11111"; // En dur pour l'instant
            var plan = await _service.CreateNewVersionAsync(id, matricule);
            return Ok(plan);
        }

        [HttpPost("{id}/valider")]
        public async Task<IActionResult> Validate(Guid id)
        {
            var matricule = "11111"; // En dur pour l'instant
            await _service.ValidateAsync(id, matricule);
            return NoContent();
        }

        [HttpPost("{id}/annuler-validation")]
        public async Task<IActionResult> CancelValidation(Guid id)
        {
            var matricule = "11111"; // En dur pour l'instant
            await _service.CancelValidationAsync(id, matricule);
            return NoContent();
        }

        [HttpPost("import-excel")]
        public async Task<IActionResult> ImportExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Fichier invalide." });

            try
            {
                using var stream = file.OpenReadStream();
                var lignes = await _excelService.ImportAssemblageExcelAsync(stream);
                return Ok(lignes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}

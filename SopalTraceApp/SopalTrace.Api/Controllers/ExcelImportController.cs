using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SopalTrace.Application.Interfaces;
using SopalTrace.Infrastructure.Services.ExcelImport;
using System.IO;
using System.Threading.Tasks;

namespace SopalTrace.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ExcelImportController : ControllerBase
{
    private readonly IExcelImportService _excelImportService;
    private readonly IExcelImportRccfService _excelImportRccfService;

    public ExcelImportController(IExcelImportService excelImportService, IExcelImportRccfService excelImportRccfService)
    {
        _excelImportService = excelImportService;
        _excelImportRccfService = excelImportRccfService;
    }

    [HttpPost("plan")]
    public async Task<IActionResult> ImportPlanExcel(IFormFile file, [FromForm] string? configurationColonnesJson = null)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "Aucun fichier fourni." });
        }

        using var stream = new MemoryStream();
        await file.CopyToAsync(stream);
        stream.Position = 0;

        var result = await _excelImportService.ParsePlanExcelAsync(stream, file.FileName, configurationColonnesJson);

        return Ok(result);
    }

    [HttpPost("verif-machine")]
    public async Task<IActionResult> ImportVerifMachineExcel(IFormFile file, [FromForm] string? configurationColonnesJson = null)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "Aucun fichier fourni." });
        }

        using var stream = new MemoryStream();
        await file.CopyToAsync(stream);
        stream.Position = 0;

        var result = await _excelImportService.ParseVerifMachineExcelAsync(stream, file.FileName, configurationColonnesJson);

        return Ok(result);
    }

    [HttpPost("controle-poste")]
    public async Task<IActionResult> ImportControlePosteExcel(IFormFile file, [FromForm] string? configurationColonnesJson = null)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "Aucun fichier fourni." });
        }

        using var stream = new MemoryStream();
        await file.CopyToAsync(stream);
        stream.Position = 0;

        var result = await _excelImportService.ParseControlePosteExcelAsync(stream, file.FileName, configurationColonnesJson);

        return Ok(result);
    }

    [HttpPost("rccf")]
    public async Task<IActionResult> ImportRccfExcel(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "Aucun fichier fourni." });
        }

        using var stream = new MemoryStream();
        await file.CopyToAsync(stream);
        stream.Position = 0;

        var result = await _excelImportRccfService.ImportAssemblageExcelAsync(stream);

        return Ok(result);
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SopalTrace.Application.Interfaces;
using SopalTrace.Infrastructure.Services.ExcelImport;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using SopalTrace.Domain.Constants;

namespace SopalTrace.Api.Controllers;

[Authorize(Roles = RolesApp.Admin + "," + RolesApp.ResponsableDI + "," + RolesApp.ResponsableQualite + "," + RolesApp.SuperviseurQualite)]
[ApiController]
[Route("api/[controller]")]
public class ExcelImportController : ControllerBase
{
    /// <summary>Aligné sur client_max_body_size de nginx ; un plan pèse quelques centaines de Ko, croquis compris.</summary>
    public const long TailleMaximaleOctets = 10 * 1024 * 1024;

    private static readonly byte[] SignatureZip = { 0x50, 0x4B, 0x03, 0x04 };
    private static readonly byte[] SignatureXlsAncien = { 0xD0, 0xCF, 0x11, 0xE0 };

    private readonly IExcelImportService _excelImportService;
    private readonly IExcelImportRccfService _excelImportRccfService;

    public ExcelImportController(IExcelImportService excelImportService, IExcelImportRccfService excelImportRccfService)
    {
        _excelImportService = excelImportService;
        _excelImportRccfService = excelImportRccfService;
    }

    [HttpPost("plan")]
    [RequestSizeLimit(TailleMaximaleOctets + 1024 * 1024)]
    public Task<IActionResult> ImportPlanExcel(IFormFile file, [FromForm] string? configurationColonnesJson = null)
        => ImporterAsync(file, csvAccepte: true,
            async flux => await _excelImportService.ParsePlanExcelAsync(flux, file.FileName, configurationColonnesJson));

    [HttpPost("verif-machine")]
    [RequestSizeLimit(TailleMaximaleOctets + 1024 * 1024)]
    public Task<IActionResult> ImportVerifMachineExcel(IFormFile file, [FromForm] string? configurationColonnesJson = null)
        => ImporterAsync(file, csvAccepte: false,
            async flux => await _excelImportService.ParseVerifMachineExcelAsync(flux, file.FileName, configurationColonnesJson));

    [HttpPost("controle-poste")]
    [RequestSizeLimit(TailleMaximaleOctets + 1024 * 1024)]
    public Task<IActionResult> ImportControlePosteExcel(IFormFile file, [FromForm] string? configurationColonnesJson = null)
        => ImporterAsync(file, csvAccepte: false,
            async flux => await _excelImportService.ParseControlePosteExcelAsync(flux, file.FileName, configurationColonnesJson));

    [HttpPost("rccf")]
    [RequestSizeLimit(TailleMaximaleOctets + 1024 * 1024)]
    public Task<IActionResult> ImportRccfExcel(IFormFile file)
        => ImporterAsync(file, csvAccepte: false,
            async flux => await _excelImportRccfService.ImportAssemblageExcelAsync(flux));

    /// <summary>
    /// Contrôle le fichier avant de l'ouvrir (taille, format réel) et transforme un classeur illisible
    /// en erreur 400 explicite plutôt qu'en erreur serveur.
    /// </summary>
    private async Task<IActionResult> ImporterAsync(IFormFile? file, bool csvAccepte, Func<Stream, Task<object>> lire)
    {
        var refus = Verifier(file, csvAccepte);
        if (refus != null) return BadRequest(new { message = refus });

        using var flux = new MemoryStream();
        await file!.CopyToAsync(flux);

        refus = VerifierContenu(flux, file.FileName, csvAccepte);
        if (refus != null) return BadRequest(new { message = refus });

        flux.Position = 0;
        try
        {
            return Ok(await lire(flux));
        }
        catch (Exception e) when (EstClasseurIllisible(e))
        {
            return BadRequest(new { message = "Le classeur Excel est illisible ou endommagé. Ouvrez-le dans Excel puis enregistrez-le de nouveau au format .xlsx." });
        }
    }

    private static string? Verifier(IFormFile? file, bool csvAccepte)
    {
        if (file == null || file.Length == 0) return "Aucun fichier fourni.";
        if (file.Length > TailleMaximaleOctets)
            return $"Fichier trop volumineux ({file.Length / (1024 * 1024)} Mo) : {TailleMaximaleOctets / (1024 * 1024)} Mo au maximum.";

        var extension = Path.GetExtension(file.FileName ?? string.Empty).ToLowerInvariant();
        if (extension == ".xls")
            return "Le format Excel 97-2003 (.xls) n'est pas pris en charge : enregistrez le fichier au format .xlsx.";
        var acceptees = csvAccepte ? new[] { ".xlsx", ".xlsm", ".csv" } : new[] { ".xlsx", ".xlsm" };
        if (!acceptees.Contains(extension))
            return $"Type de fichier non pris en charge ({(extension.Length > 0 ? extension : "sans extension")}) : formats acceptés {string.Join(", ", acceptees)}.";
        return null;
    }

    /// <summary>Le contenu doit correspondre à l'extension : un .xlsx est une archive ZIP.</summary>
    private static string? VerifierContenu(MemoryStream flux, string fileName, bool csvAccepte)
    {
        var debut = flux.GetBuffer().AsSpan(0, (int)Math.Min(flux.Length, 8));
        if (csvAccepte && fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            return debut.IndexOf((byte)0) >= 0 ? "Le fichier .csv n'est pas un fichier texte." : null;
        if (debut.StartsWith(SignatureXlsAncien))
            return "Ce fichier est au format Excel 97-2003 malgré son extension : enregistrez-le au format .xlsx.";
        if (!debut.StartsWith(SignatureZip) || !ContientUnClasseur(flux))
            return "Ce fichier n'est pas un classeur Excel (.xlsx) valide.";
        return null;
    }

    /// <summary>
    /// Archive ZIP contenant bien un classeur : ClosedXML lève une NullReferenceException
    /// sur une archive quelconque au lieu d'une erreur de format.
    /// </summary>
    private static bool ContientUnClasseur(MemoryStream flux)
    {
        try
        {
            flux.Position = 0;
            using var archive = new System.IO.Compression.ZipArchive(flux, System.IO.Compression.ZipArchiveMode.Read, leaveOpen: true);
            return archive.GetEntry("[Content_Types].xml") != null && archive.GetEntry("xl/workbook.xml") != null;
        }
        catch (InvalidDataException)
        {
            return false;
        }
    }

    private static bool EstClasseurIllisible(Exception e)
        => e is InvalidDataException
            or FileFormatException
            or DocumentFormat.OpenXml.Packaging.OpenXmlPackageException
            or System.Xml.XmlException;
}

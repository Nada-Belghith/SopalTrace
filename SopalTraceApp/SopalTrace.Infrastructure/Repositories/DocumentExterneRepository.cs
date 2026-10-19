using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SopalTrace.Application.DTOs.DocumentsExternes;
using SopalTrace.Application.Interfaces;
using SopalTrace.Domain.Entities;
using SopalTrace.Infrastructure.Data;

namespace SopalTrace.Infrastructure.Repositories;

public class DocumentExterneRepository : IDocumentExterneRepository
{
    private readonly SopalTraceDbContext _context;

    public DocumentExterneRepository(SopalTraceDbContext context) => _context = context;

    public Task<List<ExternalDocumentType>> GetTypesAsync() => _context.ExternalDocumentTypes.AsNoTracking().ToListAsync();

    public Task<bool> ArticleExisteAsync(string codeArticle) => _context.Articles.AnyAsync(a => a.CodeArticle == codeArticle);
    public Task<bool> OfExisteAsync(string numeroOf) => _context.MfgheadOrdreFabrications.AnyAsync(o => o.NumeroOf == numeroOf);
    public Task<bool> CommandeFournisseurExisteAsync(string numeroCommande) => _context.Porders.AnyAsync(c => c.NumeroCommande == numeroCommande);
    public Task<string?> GetReferenceCommandeClientAsync(string reference) =>
        // Comparaison sans casse (classement de la base) : la référence est rendue telle qu'écrite dans Sage.
        _context.Sorders.AsNoTracking()
            .Where(c => c.ReferenceCommandeClient == reference)
            .Select(c => c.ReferenceCommandeClient)
            .FirstOrDefaultAsync();

    public Task<CommandeClientSage?> GetCommandeClientAsync(string numeroCommande) =>
        _context.Sorders.AsNoTracking()
            .Where(c => c.NumeroCommande == numeroCommande)
            .Select(c => new CommandeClientSage(c.NumeroCommande, c.ReferenceCommandeClient))
            .FirstOrDefaultAsync();

    public Task<BlSage?> GetBlAsync(string numeroBl) =>
        _context.Sdeliveries.AsNoTracking()
            .Where(b => b.NumeroBl == numeroBl)
            .Select(b => new BlSage(b.NumeroBl, b.NumeroCommande, b.NumeroCommandeNavigation != null ? b.NumeroCommandeNavigation.ReferenceCommandeClient : null))
            .FirstOrDefaultAsync();

    public Task<DocumentExterneDto?> GetEnVigueurAsync(string typeCode, string cle) =>
        _context.ExternalDocuments.AsNoTracking()
            .Where(d => d.TypeCode == typeCode && d.DocumentKey == cle && d.SupersededAt == null)
            .Select(VersDto)
            .FirstOrDefaultAsync();

    public async Task AjouterAsync(ExternalDocument document)
    {
        // L'ancien document est marqué remplacé avant l'ajout du nouveau : un seul document en vigueur à chaque instant
        // (index UX_ExternalDocument_Current), le tout dans une transaction rejouable par la stratégie de réessai.
        await _context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            if (document.ReplacesDocumentId is { } remplace)
            {
                var marques = await _context.ExternalDocuments
                    .Where(d => d.Id == remplace && d.SupersededAt == null)
                    .ExecuteUpdateAsync(s => s.SetProperty(d => d.SupersededAt, document.UploadedAt));
                if (marques == 0)
                    throw new Domain.Exceptions.ConflitConcurrenceException("Ce document vient d'être remplacé par une autre personne : rafraîchissez puis réessayez.");
            }
            if (_context.Entry(document).State == EntityState.Detached) _context.ExternalDocuments.Add(document);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        });
    }

    public Task<List<DocumentExterneDto>> ListerAsync(IReadOnlyCollection<string> types, string? recherche, int nombreMaximal) =>
        _context.ExternalDocuments.AsNoTracking()
            .Where(d => types.Contains(d.TypeCode)
                && (recherche == null || d.FileName!.Contains(recherche) || d.OriginalFileName.Contains(recherche)))
            .OrderByDescending(d => d.UploadedAt)
            .Take(nombreMaximal)
            .Select(VersDto)
            .ToListAsync();

    public Task<DocumentExterneDto?> GetAsync(Guid id) =>
        _context.ExternalDocuments.AsNoTracking().Where(d => d.Id == id).Select(VersDto).FirstOrDefaultAsync();

    public Task<FichierDocumentExterne?> GetFichierAsync(Guid id) =>
        _context.ExternalDocuments.AsNoTracking()
            .Where(d => d.Id == id)
            .Select(d => new FichierDocumentExterne(d.TypeCode, d.FileName!, d.Content))
            .FirstOrDefaultAsync();

    // ── Documents d'un OF ──

    public async Task<List<LotServi>> GetLotsServisAsync(string numeroOf) =>
        // Étiquettes des préparations non annulées ; un même lot d'un même article n'apparaît qu'une fois.
        (await _context.MagPreparationOfLots.AsNoTracking()
            .Where(l => l.PreparationOf.NumeroOf == numeroOf && l.PreparationOf.Statut != "ANNULE")
            .Select(l => new { l.CodeArticle, l.CodeArticleNavigation.Designation, l.NumeroLotScanne, l.QualityControlReport })
            .ToListAsync())
        .GroupBy(l => (l.CodeArticle, l.NumeroLotScanne))
        .Select(g => new LotServi(g.Key.CodeArticle, g.First().Designation, g.Key.NumeroLotScanne,
            g.Select(l => l.QualityControlReport).FirstOrDefault(r => !string.IsNullOrWhiteSpace(r))))
        .OrderBy(l => l.CodeArticle).ThenBy(l => l.NumeroLot)
        .ToList();

    public Task<List<BlExpedie>> GetBlsExpediesAsync(string numeroOf) =>
        _context.MagExpeditionBlScanOfs.AsNoTracking()
            .Where(s => s.NumeroOfscanne == numeroOf)
            .Select(s => s.ExpeditionBl.NumeroBlNavigation)
            .Distinct()
            .OrderBy(b => b.NumeroBl)
            .Select(b => new BlExpedie(b.NumeroBl, b.CodeClient, b.DateExpedition, b.NumeroCommande,
                b.NumeroCommandeNavigation != null ? b.NumeroCommandeNavigation.ReferenceCommandeClient : null))
            .ToListAsync();

    public Task<List<DocumentExterneEnVigueur>> GetEnVigueurParNumerosAsync(NumerosRecherches numeros)
    {
        var (rapports, articles, commandesFournisseur, bls, referencesClient, numeroOf) =
            (numeros.Rapports, numeros.Articles, numeros.CommandesFournisseur, numeros.Bls, numeros.ReferencesClient, numeros.NumeroOf);
        return _context.ExternalDocuments.AsNoTracking()
            .Where(d => d.SupersededAt == null
                && ((d.ControlReportNumber != null && rapports.Contains(d.ControlReportNumber))
                    || (d.ArticleCode != null && articles.Contains(d.ArticleCode))
                    || (d.SupplierOrderNumber != null && commandesFournisseur.Contains(d.SupplierOrderNumber))
                    || (d.DeliveryNoteNumber != null && bls.Contains(d.DeliveryNoteNumber))
                    || (d.CustomerOrderReference != null && referencesClient.Contains(d.CustomerOrderReference))
                    || (numeroOf != null && d.ManufacturingOrderNumber == numeroOf)))
            .Select(d => new DocumentExterneEnVigueur(d.Id, d.TypeCode, d.FileName!, d.ControlReportNumber, d.ArticleCode, d.DeliveryNoteNumber,
                d.ManufacturingOrderNumber, d.SupplierOrderNumber, d.CustomerOrderReference, d.UploadedAt, d.UploadedByNavigation.Intusr0))
            .ToListAsync();
    }

    public Task<string?> GetArticleProduitFiniAsync(string numeroOf) =>
        _context.MfgheadOrdreFabrications.AsNoTracking()
            .Where(o => o.NumeroOf == numeroOf && o.CodeArticleNavigation.TypeArticle == "PF")
            .Select(o => o.CodeArticle)
            .FirstOrDefaultAsync();

    private static readonly Expression<Func<ExternalDocument, DocumentExterneDto>> VersDto = d => new DocumentExterneDto
    {
        Id = d.Id,
        TypeCode = d.TypeCode,
        TypeLibelle = d.TypeCodeNavigation.Label,
        NomFichier = d.FileName!,
        NumeroRapport = d.ControlReportNumber,
        CodeArticle = d.ArticleCode,
        NumeroBl = d.DeliveryNoteNumber,
        NumeroOf = d.ManufacturingOrderNumber,
        NumeroCommandeFournisseur = d.SupplierOrderNumber,
        NumeroCommande = d.SalesOrderNumber,
        ReferenceCommandeClient = d.CustomerOrderReference,
        NomFichierOrigine = d.OriginalFileName,
        EmpreinteSha256 = d.ContentSha256,
        DeposePar = d.UploadedBy,
        DeposeParNom = d.UploadedByNavigation.Intusr0,
        DeposeLe = d.UploadedAt,
        RemplaceLe = d.SupersededAt,
        RemplaceDocumentId = d.ReplacesDocumentId,
        MotifRemplacement = d.ReplacementReason
    };
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SopalTrace.Application.DTOs.Rapports;
using SopalTrace.Application.Interfaces.Rapports;
using SopalTrace.Application.Rapports;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;
using SopalTrace.Infrastructure.Data;

namespace SopalTrace.Infrastructure.Repositories.Rapports;

public class DocumentGenereRepository : IDocumentGenereRepository
{
    private readonly SopalTraceDbContext _context;

    public DocumentGenereRepository(SopalTraceDbContext context) => _context = context;

    public Task<DocumentEntete?> GetDocumentCompletAsync(Guid documentEnteteId) =>
        _context.DocumentEntetes
            .AsNoTracking()
            .AsSplitQuery()
            .Include(d => d.Formulaire)
            .Include(d => d.FamilleProduitFiniCodeNavigation!).ThenInclude(f => f.TypeRobinetCodeNavigation)
            .Include(d => d.NatureArticleCodeNavigation)
            .Include(d => d.PosteCodeNavigation)
            .Include(d => d.OperationCodeNavigation)
            .Include(d => d.DocumentSections).ThenInclude(s => s.TypeSection)
            .Include(d => d.DocumentSections).ThenInclude(s => s.Periodicite)
            .Include(d => d.DocumentSections).ThenInclude(s => s.Nqa)
            .Include(d => d.DocumentSections).ThenInclude(s => s.DocumentLignes).ThenInclude(l => l.Caracteristique!).ThenInclude(c => c.TypeCaracteristique)
            .Include(d => d.DocumentSections).ThenInclude(s => s.DocumentLignes).ThenInclude(l => l.TypeCaracteristique)
            .Include(d => d.DocumentSections).ThenInclude(s => s.DocumentLignes).ThenInclude(l => l.TypeControle)
            .Include(d => d.DocumentSections).ThenInclude(s => s.DocumentLignes).ThenInclude(l => l.MoyenControle)
            .Include(d => d.DocumentSections).ThenInclude(s => s.DocumentLignes).ThenInclude(l => l.InstrumentCodeNavigation)
            .Include(d => d.DocumentSections).ThenInclude(s => s.DocumentLignes).ThenInclude(l => l.Periodicite)
            .Include(d => d.DocumentSections).ThenInclude(s => s.DocumentLignes).ThenInclude(l => l.RisqueDefaut)
            .Include(d => d.DocumentSections).ThenInclude(s => s.DocumentLignes).ThenInclude(l => l.DocumentLigneExtraColonnes)
            .FirstOrDefaultAsync(d => d.Id == documentEnteteId);

    public Task<FormulaireDuDocument?> GetFormulaireAsync(Guid formulaireId) =>
        _context.RefFormulaires.AsNoTracking()
            .Where(f => f.Id == formulaireId)
            .Select(f => new FormulaireDuDocument(f.Id, f.CodeReference, f.Version, f.VersionGroupId))
            .FirstOrDefaultAsync();

    public async Task<SourceDocumentQualite?> GetSourceDocumentAsync(Guid documentEnteteId) =>
        (await SourcesDocumentsAsync(_context.DocumentEntetes.Where(d => d.Id == documentEnteteId), 1)).FirstOrDefault();

    public async Task<SourceDocumentExecution?> GetSourceExecutionAsync(Guid execControleDocumentStatutId) =>
        (await SourcesExecutionsAsync(_context.ExecControleDocumentStatuts.Where(s => s.Id == execControleDocumentStatutId), 1)).FirstOrDefault();

    public Task<List<SourceDocumentQualite>> GetSourcesDocumentsAsync(string? typeDocumentCode, string? recherche, int nombreMaximal) =>
        SourcesDocumentsAsync(_context.DocumentEntetes.Where(d =>
            (typeDocumentCode == null || d.TypeDocumentCode == typeDocumentCode)
            && (recherche == null || d.Nom.Contains(recherche) || (d.Formulaire != null && d.Formulaire.CodeReference.Contains(recherche)))), nombreMaximal);

    public Task<List<SourceDocumentExecution>> GetSourcesExecutionsAsync(string? typeExecution, string? numeroOf, string? recherche, int nombreMaximal) =>
        SourcesExecutionsAsync(_context.ExecControleDocumentStatuts.Where(s =>
            // Seuls les documents d'exécution qu'un modèle met en forme ; un registre de poste n'a qu'un PDF (son premier document).
            _context.ReportTemplates.Any(t => t.ExecutionDocumentTypeCode == s.TypeDocument)
            && (s.TypeDocument != TypesDocumentExecution.RegistreTracabilite || !_context.ExecControleDocumentStatuts.Any(o =>
                o.ExecControleOfId == s.ExecControleOfId && o.PosteCode == s.PosteCode && o.TypeDocument == s.TypeDocument && o.CreationOrder < s.CreationOrder))
            && (typeExecution == null || s.TypeDocument == typeExecution)
            && (numeroOf == null || s.ExecControleOf.NumeroOf == numeroOf)
            && (recherche == null || s.ExecControleOf.NumeroOf.Contains(recherche) || (s.PosteCode != null && s.PosteCode.Contains(recherche))
                || (s.MachineCode != null && s.MachineCode.Contains(recherche)))), nombreMaximal);

    public Task<List<SourceDocumentExecution>> GetFichesDeLOfAsync(string numeroOf) =>
        SourcesExecutionsAsync(_context.ExecControleDocumentStatuts.Where(s =>
            s.ExecControleOf.NumeroOf == numeroOf
            // Les clôtures de poste sont des marqueurs du parcours, pas des fiches.
            && s.TypeDocument != TypesDocumentExecution.ClotureVerifMachinePoste
            && s.TypeDocument != TypesDocumentExecution.ClotureResultatControlePoste
            && (s.TypeDocument != TypesDocumentExecution.RegistreTracabilite || !_context.ExecControleDocumentStatuts.Any(o =>
                o.ExecControleOfId == s.ExecControleOfId && o.PosteCode == s.PosteCode && o.TypeDocument == s.TypeDocument && o.CreationOrder < s.CreationOrder))),
            int.MaxValue);

    public async Task<List<SourceDocumentQualite>> GetDocumentsAppliquesAsync(string numeroOf)
    {
        var documents = await SourcesDocumentsAsync(_context.DocumentEntetes.Where(d =>
            _context.ExecControleOfs.Any(o => o.NumeroOf == numeroOf && o.PlanSourceId == d.Id)
            || _context.ExecControleDocumentStatuts.Any(s => s.ExecControleOf.NumeroOf == numeroOf && s.DocId == d.Id)), int.MaxValue);
        // Document sans opération propre (plan d'assemblage, contrôle au poste…) : celle de l'OF où il est appliqué.
        var operations = await _context.ExecControleOfs.AsNoTracking()
            .Where(o => o.NumeroOf == numeroOf)
            .Select(o => new
            {
                Documents = o.ExecControleDocumentStatuts.Where(s => s.DocId != null).Select(s => s.DocId!.Value).ToList(),
                o.PlanSourceId,
                Operation = new OperationDocument(o.OperationCode, o.OperationCodeNavigation.Libelle)
            })
            .ToListAsync();
        return documents.Select(d => d.Operation != null ? d : d with
        {
            Operation = operations.FirstOrDefault(o => o.PlanSourceId == d.Id || o.Documents.Contains(d.Id))?.Operation
        }).ToList();
    }

    public Task<bool> OfExisteAsync(string numeroOf) => _context.ExecControleOfs.AnyAsync(o => o.NumeroOf == numeroOf);

    public Task<List<ModeleDuType>> GetModelesDesTypesAsync() =>
        _context.ReportTemplates.AsNoTracking()
            .Select(t => new ModeleDuType(t.Code, t.Name, t.DocumentTypeCode, t.ExecutionDocumentTypeCode, t.FormRole))
            .ToListAsync();

    public Task<GeneratedDocument?> GetGenereAsync(SourceRapport source, string empreinteDonnees, Guid versionModeleId) =>
        DeLaSource(source).AsNoTracking()
            .FirstOrDefaultAsync(g => g.SourceDataSha256 == empreinteDonnees && g.ReportTemplateVersionId == versionModeleId);

    public async Task<bool> AjouterAsync(GeneratedDocument document)
    {
        _context.GeneratedDocuments.Add(document);
        try
        {
            await _context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
        {
            _context.Entry(document).State = EntityState.Detached;
            return false;
        }
    }

    public Task<List<DocumentGenereDto>> GetParSourceAsync(SourceRapport source) =>
        DeLaSource(source).AsNoTracking()
            .OrderByDescending(g => g.GeneratedAt)
            .Select(g => new DocumentGenereDto
            {
                Id = g.Id,
                DocumentEnteteId = g.DocumentEnteteId,
                ExecControleDocumentStatutId = g.ExecControleDocumentStatutId,
                NomFichier = g.FileName,
                CodeModele = g.ReportTemplateVersion.ReportTemplateCode,
                VersionModele = g.ReportTemplateVersion.VersionNumber,
                VersionModeleId = g.ReportTemplateVersionId,
                EmpreintePdfSha256 = g.PdfSha256,
                GenereLe = g.GeneratedAt,
                DemandePar = g.RequestedBy,
                DemandeParNom = g.RequestedByNavigation != null ? g.RequestedByNavigation.Intusr0 : null
            })
            .ToListAsync();

    public Task<GeneratedDocument?> GetAsync(Guid id) =>
        _context.GeneratedDocuments.AsNoTracking().FirstOrDefaultAsync(g => g.Id == id);

    private IQueryable<GeneratedDocument> DeLaSource(SourceRapport source) => source.EstExecution
        ? _context.GeneratedDocuments.Where(g => g.ExecControleDocumentStatutId == source.ExecutionId)
        : _context.GeneratedDocuments.Where(g => g.DocumentEnteteId == source.DocumentEnteteId);

    private static async Task<List<SourceDocumentQualite>> SourcesDocumentsAsync(IQueryable<DocumentEntete> documents, int nombreMaximal) =>
        (await documents.AsNoTracking()
            .OrderByDescending(d => d.CreationOrder)
            .Take(nombreMaximal)
            .Select(d => new
            {
                d.Id,
                d.TypeDocumentCode,
                TypeLibelle = d.TypeDocumentCodeNavigation.Libelle,
                d.Nom,
                d.Version,
                d.Statut,
                d.PosteCode,
                d.FormulaireId,
                CodeFormulaire = d.Formulaire != null ? d.Formulaire.CodeReference : null,
                VersionFormulaire = d.Formulaire != null ? (int?)d.Formulaire.Version : null,
                GroupeFormulaire = d.Formulaire != null ? (Guid?)d.Formulaire.VersionGroupId : null,
                d.CreationOrder,
                d.CreeLe,
                d.OperationCode,
                OperationLibelle = d.OperationCodeNavigation != null ? d.OperationCodeNavigation.Libelle : null
            })
            .ToListAsync())
        .Select(d => new SourceDocumentQualite(d.Id, d.TypeDocumentCode, d.TypeLibelle, d.Nom, d.Version, d.Statut, d.PosteCode,
            Formulaire(d.FormulaireId, d.CodeFormulaire, d.VersionFormulaire, d.GroupeFormulaire), d.CreationOrder, d.CreeLe)
        {
            Operation = d.OperationCode == null ? null : new OperationDocument(d.OperationCode, d.OperationLibelle ?? d.OperationCode)
        })
        .ToList();

    /// <summary>Formulaire d'un document d'exécution : celui des résultats d'assemblage, sinon celui du document appliqué.</summary>
    private static async Task<List<SourceDocumentExecution>> SourcesExecutionsAsync(IQueryable<ExecControleDocumentStatut> statuts, int nombreMaximal) =>
        (await statuts.AsNoTracking()
            .OrderByDescending(s => s.CreationOrder)
            .Take(nombreMaximal)
            .Select(s => new
            {
                s.Id,
                s.TypeDocument,
                s.TypeDocumentNavigation.Label,
                s.ExecControleOf.NumeroOf,
                StatutOf = s.ExecControleOf.Statut,
                s.EstTermine,
                s.PosteCode,
                s.MachineCode,
                FormulaireId = s.FormId ?? (s.Doc != null ? s.Doc.FormulaireId : null),
                CodeFormulaire = s.Form != null ? s.Form.CodeReference : s.Doc != null && s.Doc.Formulaire != null ? s.Doc.Formulaire.CodeReference : null,
                VersionFormulaire = s.Form != null ? (int?)s.Form.Version : s.Doc != null && s.Doc.Formulaire != null ? (int?)s.Doc.Formulaire.Version : null,
                GroupeFormulaire = s.Form != null ? (Guid?)s.Form.VersionGroupId : s.Doc != null && s.Doc.Formulaire != null ? (Guid?)s.Doc.Formulaire.VersionGroupId : null,
                Date = s.DateExecution ?? s.ExecControleOf.DateDebut,
                s.CreationOrder,
                OrdreModele = s.ExecControleOf.ExecControleDocumentStatuts
                    .Where(o => (o.FormId ?? (o.Doc != null ? o.Doc.FormulaireId : null)) == (s.FormId ?? (s.Doc != null ? s.Doc.FormulaireId : null)))
                    .Min(o => (long?)o.CreationOrder) ?? s.CreationOrder,
                s.DocId,
                s.ExecControleOf.OperationCode,
                OperationLibelle = s.ExecControleOf.OperationCodeNavigation.Libelle,
                // Une fiche est remplie dès qu'elle porte une réponse (le registre n'est créé qu'à la confirmation des lots ;
                // les résultats en cours de fabrication sont portés par l'OF : tranches et pièces types).
                Remplie = s.TypeDocument == TypesDocumentExecution.RegistreTracabilite
                    || (s.TypeDocument == TypesDocumentExecution.ResultatControleCf
                        && (s.ExecControleOf.ExecControleTranches.Any() || s.ExecControleOf.ExecPieceTypes.Any()))
                    || s.ExecControleLigneReponses.Any() || s.ExecEchantillonnages.Any() || s.ExecRcPosteReponses.Any()
                    || s.ExecRcPosteBilans.Any() || s.ExecVerifMachineReponses.Any()
            })
            .ToListAsync())
        .Select(s => new SourceDocumentExecution(s.Id, s.TypeDocument, s.Label, s.NumeroOf, s.StatutOf, s.EstTermine, s.PosteCode, s.MachineCode,
            Formulaire(s.FormulaireId, s.CodeFormulaire, s.VersionFormulaire, s.GroupeFormulaire), s.Date, s.CreationOrder, s.DocId)
        {
            Operation = new OperationDocument(s.OperationCode, s.OperationLibelle),
            OrdreModele = s.OrdreModele,
            Remplie = s.Remplie
        })
        .ToList();

    private static FormulaireDuDocument? Formulaire(Guid? id, string? code, int? version, Guid? groupe) =>
        id is { } valeur && code != null ? new FormulaireDuDocument(valeur, code, version ?? 0, groupe ?? Guid.Empty) : null;
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SopalTrace.Application.Interfaces.Rapports;
using SopalTrace.Application.Rapports.Execution;
using SopalTrace.Domain.Constants;
using SopalTrace.Infrastructure.Data;

namespace SopalTrace.Infrastructure.Repositories.Rapports;

public class DonneesExecutionRepository : IDonneesExecutionRepository
{
    private readonly SopalTraceDbContext _context;

    public DonneesExecutionRepository(SopalTraceDbContext context) => _context = context;

    public Task<StatutExecutionRapport?> GetStatutAsync(Guid execControleDocumentStatutId) =>
        _context.ExecControleDocumentStatuts
            .AsNoTracking()
            .Where(s => s.Id == execControleDocumentStatutId)
            .Select(s => new StatutExecutionRapport(
                s.Id,
                s.ExecControleOfId,
                s.TypeDocument,
                s.PosteCode,
                s.PosteCodeNavigation != null ? s.PosteCodeNavigation.Libelle : null,
                s.MachineCode,
                s.Equipe,
                s.DateExecution,
                s.DateTermine,
                s.EstTermine,
                s.MatriculeOperateur,
                s.ExecControleOf.NumeroOf,
                s.ExecControleOf.NumeroOfNavigation.CodeArticle,
                s.ExecControleOf.NumeroOfNavigation.CodeArticleNavigation.Designation,
                _context.ProduitFinis.Where(p => p.CodeArticle == s.ExecControleOf.NumeroOfNavigation.CodeArticle).Select(p => p.FamilleProduitFiniCode).FirstOrDefault(),
                _context.ProduitFinis.Where(p => p.CodeArticle == s.ExecControleOf.NumeroOfNavigation.CodeArticle).Select(p => p.FamilleProduitFiniCodeNavigation.Designation).FirstOrDefault(),
                _context.ProduitFinis.Where(p => p.CodeArticle == s.ExecControleOf.NumeroOfNavigation.CodeArticle).Select(p => p.FamilleProduitFiniCodeNavigation.TypeRobinetCode).FirstOrDefault(),
                s.ExecControleOf.NumeroOfNavigation.QuantiteLancee,
                (DateTime?)s.ExecControleOf.DateDebut,
                // Formulaire de la fiche (résultats d'assemblage) sinon celui du document appliqué : une ligne figée.
                s.Form != null ? s.Form.CodeReference : s.Doc != null && s.Doc.Formulaire != null ? s.Doc.Formulaire.CodeReference : null,
                s.Form != null ? (int?)s.Form.Version : s.Doc != null && s.Doc.Formulaire != null ? (int?)s.Doc.Formulaire.Version : null,
                s.Doc != null ? s.Doc.Nom : null,
                s.SessionId))
            .FirstOrDefaultAsync();

    public Task<StatutExecutionRapport?> GetStatutViergeAsync(Guid documentEnteteId, string typeExecution, FormulaireDuDocument? formulaire)
    {
        var codeFormulaire = formulaire?.CodeReference;
        var versionFormulaire = formulaire?.Version;
        return _context.DocumentEntetes
            .AsNoTracking()
            .Where(d => d.Id == documentEnteteId)
            .Select(d => new StatutExecutionRapport(
                Guid.Empty,
                Guid.Empty,
                typeExecution,
                d.PosteCode,
                d.PosteCodeNavigation != null ? d.PosteCodeNavigation.Libelle : null,
                d.MachineCode,
                null, null, null, false, null,
                "",
                null, null,
                d.FamilleProduitFiniCode,
                d.FamilleProduitFiniCodeNavigation != null ? d.FamilleProduitFiniCodeNavigation.Designation : null,
                d.FamilleProduitFiniCodeNavigation != null ? d.FamilleProduitFiniCodeNavigation.TypeRobinetCode : null,
                0m,
                null,
                codeFormulaire,
                versionFormulaire,
                d.Nom,
                null))
            .FirstOrDefaultAsync();
    }

    public Task<Guid?> GetFormulaireDocumentAsync(Guid documentEnteteId) =>
        _context.DocumentEntetes.AsNoTracking().Where(d => d.Id == documentEnteteId).Select(d => d.FormulaireId).FirstOrDefaultAsync();

    public async Task<Guid?> GetFormulaireResultatsPlanAsync(Guid documentEnteteId)
    {
        var plan = await _context.DocumentEntetes.AsNoTracking()
            .Where(d => d.Id == documentEnteteId && d.TypeDocumentCode == TypesDocument.PlanAssemblage)
            .Select(d => new
            {
                d.PosteCode,
                d.FamilleProduitFiniCode,
                TypeRobinetCode = d.FamilleProduitFiniCodeNavigation != null ? d.FamilleProduitFiniCodeNavigation.TypeRobinetCode : null
            })
            .FirstOrDefaultAsync();
        return plan == null ? null
            : await Execution.FormulairesResultats.TrouverAsync(_context, plan.PosteCode, plan.FamilleProduitFiniCode, plan.TypeRobinetCode);
    }

    public Task<ParametresEchantillonnageRapport?> GetParametresEchantillonnageAsync(Guid documentEnteteId) =>
        _context.SamplingPlans.AsNoTracking()
            .Where(p => p.DocumentId == documentEnteteId)
            .Select(p => new ParametresEchantillonnageRapport(p.InspectionLevelCode, p.SamplingType, p.InspectionSeverity,
                (decimal?)p.AcceptableQualityLevel.ValeurNqa, p.AcceptanceNumber, p.RejectionNumber))
            .FirstOrDefaultAsync();

    public async Task<List<SectionPlanRapport>> GetSectionsPlanAsync(Guid documentEnteteId)
    {
        var sections = await _context.DocumentSections.AsNoTracking()
            .Where(s => s.EnteteId == documentEnteteId)
            .OrderBy(s => s.OrdreAffiche)
            .Select(s => new
            {
                s.Id, s.OrdreAffiche, s.LibelleSection,
                Type = s.TypeSection != null ? s.TypeSection.TypeNormalise : null,
                Lignes = s.DocumentLignes.OrderBy(l => l.OrdreAffiche)
                    .Select(l => new { l.Id, l.OrdreAffiche, Libelle = l.LibelleAffiche ?? (l.Caracteristique != null ? l.Caracteristique.Libelle : "") })
                    .ToList()
            })
            .ToListAsync();
        return sections.Select(s => new SectionPlanRapport(s.Id, s.OrdreAffiche, s.LibelleSection, s.Type,
            s.Lignes.Select(l => new LignePlanRapport(l.Id, l.OrdreAffiche, l.Libelle ?? "")).ToList())).ToList();
    }

    public Task<List<ReponseLigneRapport>> GetReponsesLignesAsync(Guid execControleDocumentStatutId) =>
        _context.ExecControleLigneReponses.AsNoTracking()
            .Where(r => r.ExecControleDocumentStatutId == execControleDocumentStatutId)
            .Select(r => new ReponseLigneRapport(r.DocumentLigneId, r.Contexte, r.NumeroReglage, r.ResultatType, r.DetailsNc,
                r.ActionsCorrection, r.MatriculeOperateur, r.DateSaisie))
            .ToListAsync();

    public Task<List<OccurrenceRapport>> GetOccurrencesAsync(Guid execControleOfId, string posteCode, Guid? sessionId) =>
        _context.ExecPrelevementIntermediaires.AsNoTracking()
            .Where(o => o.ExecControleOfid == execControleOfId && o.PosteCode == posteCode && o.SessionId == sessionId && o.EstRepondu)
            .Select(o => new OccurrenceRapport(o.TrancheHoraire, o.Resultat, o.HeureReponse))
            .ToListAsync();

    public Task<List<TrancheRapport>> GetTranchesAsync(Guid execControleOfId, Guid? sessionId) =>
        _context.ExecControleTranches.AsNoTracking()
            .Where(t => t.ExecControleOfid == execControleOfId && t.SessionId == sessionId)
            .Select(t => new TrancheRapport(t.TrancheHoraire, t.ResultatFinal, t.DetailsNc, t.ActionsCorrection, t.MatriculeApprobateur))
            .ToListAsync();

    public async Task<PlanVerifMachineRapport?> GetPlanVerifMachineAsync(Guid documentEnteteId)
    {
        var entete = await _context.DocumentEntetes.AsNoTracking()
            .Where(d => d.Id == documentEnteteId)
            .Select(d => new
            {
                MachineCode = d.MachineCode ?? "",
                Libelle = d.MachineCodeNavigation != null ? d.MachineCodeNavigation.Libelle : null,
                Modele = d.MachineCodeNavigation != null ? d.MachineCodeNavigation.VerifModele : null,
                TablesEtalonSeparees = d.MachineCodeNavigation != null && d.MachineCodeNavigation.VerifTablesEtalonSeparees
            })
            .FirstOrDefaultAsync();
        if (entete == null) return null;

        var familles = await _context.DocumentVerifMachineFamilles.AsNoTracking()
            .Where(f => f.EnteteId == documentEnteteId)
            .OrderBy(f => f.OrdreAffiche)
            .Select(f => new FamilleVerifMachine(f.Id, f.OrdreAffiche, f.RefFamilleCorps.Code, f.RefFamilleCorps.Designation))
            .ToListAsync();

        var lignes = await _context.DocumentLignes.AsNoTracking()
            .Where(l => l.EnteteId == documentEnteteId)
            .OrderBy(l => l.OrdreAffiche)
            .Select(l => new
            {
                l.Id, l.OrdreAffiche, l.TypeLigne,
                Libelle = l.LibelleAffiche ?? (l.Caracteristique != null ? l.Caracteristique.Libelle : ""),
                l.Instruction,
                Echeances = l.DocumentVerifMachineEcheances
                    .OrderBy(g => g.PeriodiciteMachine.OrdreAffichage).ThenBy(g => g.OrdreAffiche)
                    .Select(g => new
                    {
                        g.Id, g.OrdreAffiche,
                        Periodicite = g.PeriodiciteMachine.Libelle,
                        OrdrePeriodicite = g.PeriodiciteMachine.OrdreAffichage,
                        Moyen = g.RefMoyenDetection != null ? g.RefMoyenDetection.Designation : null,
                        Pieces = g.DocumentVerifMachineMatricePieces
                            .Select(m => new { m.RoleVerif, m.FamilleId, Code = m.PieceRef != null ? m.PieceRef.Code : null })
                            .ToList()
                    })
                    .ToList()
            })
            .ToListAsync();

        var points = lignes.Select(l => new PointVerifMachine(l.Id, l.OrdreAffiche, l.TypeLigne, l.Libelle ?? "", l.Instruction,
            l.Echeances.Select(g => new EcheanceVerifMachine(g.Id, g.OrdreAffiche, g.Periodicite, g.OrdrePeriodicite, g.Moyen,
                g.Pieces.Select(p => new PieceVerifMachine(p.RoleVerif, p.FamilleId, p.Code)).ToList())).ToList())).ToList();
        return new PlanVerifMachineRapport(entete.MachineCode, entete.Libelle, entete.Modele, entete.TablesEtalonSeparees, familles, points);
    }

    public async Task<List<ReponseVerifMachine>> GetReponsesVerifMachineAsync(Guid execControleDocumentStatutId)
    {
        // Historique en ajout seul : seule la dernière saisie de chaque échéance compte (comme l'écran).
        var reponses = await _context.ExecVerifMachineReponses.AsNoTracking()
            .Where(r => r.ExecControleDocumentStatutId == execControleDocumentStatutId)
            .Select(r => new ReponseVerifMachine(r.DocumentVerifMachineEcheanceId, r.DateExecution, r.MatriculeOperateur,
                r.PressionEntree, r.FuiteAffichee, r.Conforme, r.Observation))
            .ToListAsync();
        return reponses.GroupBy(r => r.EcheanceId).Select(g => g.OrderByDescending(r => r.DateExecution).First()).ToList();
    }

    public Task<Guid?> GetDocumentAppliqueAsync(Guid execControleDocumentStatutId) =>
        _context.ExecControleDocumentStatuts.AsNoTracking()
            .Where(s => s.Id == execControleDocumentStatutId)
            .Select(s => s.DocId)
            .FirstOrDefaultAsync();

    public Task<Guid?> GetPremierStatutRegistreAsync(Guid execControleOfId, string posteCode) =>
        _context.ExecControleDocumentStatuts.AsNoTracking()
            .Where(s => s.ExecControleOfId == execControleOfId && s.PosteCode == posteCode && s.TypeDocument == TypesDocumentExecution.RegistreTracabilite)
            .OrderBy(s => s.CreationOrder)
            .Select(s => (Guid?)s.Id)
            .FirstOrDefaultAsync();

    public async Task<List<EntreeRegistreRapport>> GetRegistreAsync(Guid execControleOfId, string posteCode)
    {
        var lignes = await _context.ExecRegistreTracabilites.AsNoTracking()
            .Where(r => r.ExecControleOfId == execControleOfId && r.PosteCode == posteCode && r.CancelledAt == null)
            .OrderBy(r => r.DateHeure).ThenBy(r => r.Version)
            .Select(r => new
            {
                r.Version, r.DateHeure, r.Corps, r.Volant, r.Equipe,
                Composants = r.ExecRegistreTracabiliteComposants
                    .Select(c => new ComposantRegistreRapport(c.CodeArticle, c.DesignationComposant, c.LotSelectionne))
                    .ToList()
            })
            .ToListAsync();
        return lignes.Select(l => new EntreeRegistreRapport(l.Version, l.DateHeure, l.Corps, l.Volant, l.Equipe, l.Composants)).ToList();
    }

    public Task<List<EtiquetteRapport>> GetEtiquettesAsync(string numeroOf) =>
        _context.MagPreparationOfLots.AsNoTracking()
            .Where(l => l.PreparationOf.NumeroOf == numeroOf && l.PreparationOf.Statut != StatutsPreparationMagasin.Annule)
            .OrderBy(l => l.DateScan)
            .Select(l => new EtiquetteRapport(l.CodeArticle, l.CodeArticleNavigation.Designation, l.NumeroLotScanne, l.Quantite,
                l.QualityControlReport, l.DateScan))
            .ToListAsync();

    public async Task<Dictionary<string, string>> GetNomsAsync(IEnumerable<string> matricules)
    {
        var liste = matricules.Where(m => !string.IsNullOrWhiteSpace(m)).Distinct().ToList();
        if (liste.Count == 0) return new Dictionary<string, string>();
        return await _context.Autilis
            .AsNoTracking()
            .Where(a => liste.Contains(a.Usr0))
            .ToDictionaryAsync(a => a.Usr0, a => a.Intusr0);
    }
}

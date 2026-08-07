using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SopalTrace.Application.Interfaces.Execution;
using SopalTrace.Domain.Entities;
using SopalTrace.Infrastructure.Data;

namespace SopalTrace.Infrastructure.Repositories.Execution;

public class OperateurRepository : IOperateurRepository
{
    private readonly SopalTraceDbContext _context;

    public OperateurRepository(SopalTraceDbContext context)
    {
        _context = context;
    }

    public async Task<MfgheadOrdreFabrication?> GetOfAsync(string numeroOf)
    {
        return await _context.MfgheadOrdreFabrications
            .Include(o => o.CodeArticleNavigation)
            .FirstOrDefaultAsync(o => o.NumeroOf == numeroOf);
    }

    public async Task<MagPreparationOf?> GetMagPreparationOfAsync(string numeroOf)
    {
        return await _context.MagPreparationOfs
            .FirstOrDefaultAsync(p => p.NumeroOf == numeroOf);
    }

    public async Task<PlanFabricationEntete?> GetPlanActifAsync(string codeArticle, string? operationCode = null)
    {
        var query = _context.PlanFabricationEntetes
            .Include(p => p.PlanFabricationSections)
                .ThenInclude(s => s.PlanFabricationLignes)
            .Include(p => p.PlanFabricationSections)
                .ThenInclude(s => s.TypeSection)
            .Where(p => p.Statut == "ACTIF" && 
                        (p.CodeArticleSageVersionne == codeArticle || 
                         p.CodeArticleSageVersionne.StartsWith(codeArticle + ".")));

        if (!string.IsNullOrEmpty(operationCode))
        {
            query = query.Where(p => p.OperationCode == operationCode);
        }

        return await query.FirstOrDefaultAsync();
    }

    public async Task<ExecControleOf?> GetExecOfByIdAsync(Guid execOfId)
    {
        return await _context.ExecControleOfs.FindAsync(execOfId);
    }

    public async Task<ExecControleOf?> GetExecOfWithIntermediairesAsync(Guid execOfId)
    {
        return await _context.ExecControleOfs
            .Include(x => x.ExecPrelevementIntermediaires)
            .FirstOrDefaultAsync(x => x.Id == execOfId);
    }

    public async Task<IEnumerable<string>> GetPostesForExecutionAsync(Guid execOfId)
    {
        return await _context.ExecControleOfPostes
            .Where(x => x.ExecControleOfId == execOfId)
            .Select(x => x.PosteCode)
            .ToListAsync();
    }

    public void AddExecControleOf(ExecControleOf execOf)
    {
        _context.ExecControleOfs.Add(execOf);
    }



    public async Task<bool> HasReglageSectionsAsync(Guid planSourceId)
    {
        return await _context.PlanFabricationSections
            .Include(s => s.TypeSection)
            .AnyAsync(s => s.PlanEnteteId == planSourceId && s.TypeSection != null &&
                           (s.TypeSection.Code == "REGLAGE" || s.TypeSection.Code == "REGLAGE_PROD"));
    }



    public async Task<IEnumerable<PosteTravail>> GetPostesDisponiblesAsync()
    {
        return await _context.PosteTravails
            .Where(p => p.Actif)
            .ToListAsync();
    }

    public async Task<PosteTravail?> GetPosteWithMachinesAsync(string posteCode)
    {
        return await _context.PosteTravails
            .Include(p => p.CodeMachines)
            .FirstOrDefaultAsync(p => p.CodePoste == posteCode);
    }

    public async Task<IEnumerable<SopalTrace.Application.DTOs.Execution.Operateur.OperateurOfDto>> GetAllOfOperationsDisponiblesAsync()
    {
        // On récupère uniquement les OFs qui ont été préparés par le magasin (MagPreparationOf)
        var ofs = await _context.MagPreparationOfs
            .Include(m => m.NumeroOfNavigation)
                .ThenInclude(of => of.CodeArticleNavigation)
                    .ThenInclude(a => a.NatureArticleCodeNavigation)
                        .ThenInclude(na => na.NatureArticleOperations)
                            .ThenInclude(nao => nao.OperationCodeNavigation)
            .Where(m => m.Statut == "EN_COURS" || m.Statut == "PLANIFIE" || m.Statut == "EN_PAUSE")
            .ToListAsync();

        var result = new List<SopalTrace.Application.DTOs.Execution.Operateur.OperateurOfDto>();

        foreach (var magOf in ofs)
        {
            var of = magOf.NumeroOfNavigation;
            var article = of.CodeArticleNavigation;

            var activeExecs = await _context.ExecControleOfs
                .Where(e => e.NumeroOf == of.NumeroOf)
                .ToListAsync();

            var dto = new SopalTrace.Application.DTOs.Execution.Operateur.OperateurOfDto
            {
                NumeroOf = of.NumeroOf,
                StatutOf = of.StatutOf,
                CodeArticle = of.CodeArticle,
                DesignationArticle = article?.Designation ?? "",
                QuantitePrevue = of.QuantitePrevue,
                QuantiteLancee = of.QuantiteLancee,
                DateDebut = of.DateDebut
            };

            if (article?.NatureArticleCodeNavigation?.NatureArticleOperations != null)
            {
                foreach (var op in article.NatureArticleCodeNavigation.NatureArticleOperations.OrderBy(o => o.OrdreGamme))
                {
                    var machine = await _context.Machines.FirstOrDefaultAsync(m => m.OperationCode == op.OperationCode);
                    var execForOp = activeExecs.OrderByDescending(e => e.DateDebut).FirstOrDefault(e => e.OperationCode == op.OperationCode);
                    
                    bool aDesControlesReglage = false;
                    string? legendeMoyens = null;
                    string? remarques = null;

                    if (execForOp != null && execForOp.PlanSourceId.HasValue)
                    {
                        aDesControlesReglage = await HasReglageSectionsAsync(execForOp.PlanSourceId.Value);
                        var plan = await _context.PlanFabricationEntetes.FirstOrDefaultAsync(p => p.Id == execForOp.PlanSourceId.Value);
                        if (plan != null)
                        {
                            legendeMoyens = plan.LegendeMoyens;
                            remarques = plan.Remarques;
                        }
                    }

                    dto.GammeOperatoire.Add(new SopalTrace.Application.DTOs.Execution.Operateur.OperateurOperationDto
                    {
                        OperationCode = op.OperationCode,
                        Libelle = op.OperationCodeNavigation?.Libelle ?? op.OperationCode,
                        MachinePrevueCode = machine?.CodeMachine ?? "",
                        ActiveExecControleOfId = execForOp?.Id,
                        ActiveExecStatut = execForOp?.Statut,
                        ActiveMachineCode = execForOp?.MachineCode,
                        EstEnReglage = execForOp?.EstEnReglage,
                        A_Des_Controles_Reglage = execForOp != null ? aDesControlesReglage : (bool?)null,
                        LegendeMoyens = legendeMoyens,
                        Remarques = remarques
                    });
                }
            }

            result.Add(dto);
        }

        return result;
    }

    public async Task<PlanFabricationLigne?> GetPlanLigneAsync(Guid ligneId)
    {
        return await _context.PlanFabricationLignes.FirstOrDefaultAsync(l => l.Id == ligneId);
    }

    public async Task<bool> AreAllOperationsClosedAsync(string numeroOf)
    {
        var of = await _context.MfgheadOrdreFabrications
            .Include(o => o.CodeArticleNavigation)
                .ThenInclude(a => a.NatureArticleCodeNavigation)
                    .ThenInclude(na => na.NatureArticleOperations)
            .FirstOrDefaultAsync(o => o.NumeroOf == numeroOf);

        if (of?.CodeArticleNavigation?.NatureArticleCodeNavigation?.NatureArticleOperations == null) 
            return false;

        var requiredOperations = of.CodeArticleNavigation.NatureArticleCodeNavigation.NatureArticleOperations
            .Select(o => o.OperationCode)
            .ToList();
        
        var execs = await _context.ExecControleOfs
            .Where(e => e.NumeroOf == numeroOf)
            .ToListAsync();

        foreach (var reqOp in requiredOperations)
        {
            var exec = execs.OrderByDescending(e => e.DateDebut).FirstOrDefault(e => e.OperationCode == reqOp);
            // Si une opération requise n'a jamais été commencée ou n'est pas clôturée, c'est faux.
            if (exec == null || exec.Statut != "CLOTURE")
                return false;
        }

        return true;
    }

    public async Task<bool> CanStartOperationAsync(string numeroOf, string operationCode)
    {
        var of = await _context.MfgheadOrdreFabrications
            .Include(o => o.CodeArticleNavigation)
                .ThenInclude(a => a.NatureArticleCodeNavigation)
                    .ThenInclude(na => na.NatureArticleOperations)
            .FirstOrDefaultAsync(o => o.NumeroOf == numeroOf);

        if (of?.CodeArticleNavigation?.NatureArticleCodeNavigation?.NatureArticleOperations == null) 
            return true;

        var operationsGamme = of.CodeArticleNavigation.NatureArticleCodeNavigation.NatureArticleOperations
            .OrderBy(o => o.OrdreGamme)
            .ToList();

        var index = operationsGamme.FindIndex(o => o.OperationCode == operationCode);
        if (index <= 0) return true; // C'est la première opération ou l'opération n'est pas dans la gamme

        var previousOp = operationsGamme[index - 1];

        // Vérifier si l'opération précédente a commencé
        var execPrecedente = await _context.ExecControleOfs
            .Where(e => e.NumeroOf == numeroOf && e.OperationCode == previousOp.OperationCode)
            .OrderByDescending(e => e.DateDebut)
            .FirstOrDefaultAsync();

        return execPrecedente != null;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    public async Task<DocumentEntete?> GetPlanAssemblageActifAsync(string codeArticle)
    {
        var pf = await _context.ProduitFinis.FirstOrDefaultAsync(p => p.CodeArticle == codeArticle);
        var familleCode = pf?.FamilleProduitFiniCode;

        bool isSoupape = pf != null && !string.IsNullOrEmpty(pf.FamilleProduitFiniCode) && pf.FamilleProduitFiniCode.IndexOf("soupape", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         !string.IsNullOrEmpty(codeArticle) && (codeArticle.IndexOf("PAS", StringComparison.OrdinalIgnoreCase) >= 0 || codeArticle.IndexOf("soupape", StringComparison.OrdinalIgnoreCase) >= 0);

        var plans = await _context.Set<DocumentEntete>()
            .Include(p => p.DocumentSections)
                .ThenInclude(s => s.DocumentLignes)
            .Include(p => p.Formulaire)
            .Where(p => p.Statut == "ACTIF")
            .OrderByDescending(p => p.CreeLe)
            .ToListAsync();

        var plansAss = plans.Where(p => p.TypeDocumentCode == "PLAN_ASS" || p.TypeDocumentCode == "PLAN_ASSEMBLAGE" || p.TypeDocumentCode == "PLAN_FAB" || p.TypeDocumentCode == "MODELE_FAB").ToList();
        if (!plansAss.Any())
        {
            plansAss = plans.Where(p => p.TypeDocumentCode == "RESULTAT_CF" || p.TypeDocumentCode == "RCCF" || p.TypeDocumentCode == "CTRL_POSTE" || p.TypeDocumentCode == "RESULTAT_CONTROLE_POSTE").ToList();
        }
        
        DocumentEntete? match = null;
        if (isSoupape)
        {
            match = plansAss.FirstOrDefault(p => p.FamilleProduitFiniCode == codeArticle || (p.Nom != null && p.Nom.Contains(codeArticle, StringComparison.OrdinalIgnoreCase)) || (p.Designation != null && p.Designation.Contains(codeArticle, StringComparison.OrdinalIgnoreCase)) || (p.Formulaire != null && p.Formulaire.CodeReference != null && p.Formulaire.CodeReference.Contains(codeArticle, StringComparison.OrdinalIgnoreCase)));
            if (match == null)
            {
                match = plansAss.FirstOrDefault(p => (p.Nom != null && (p.Nom.Contains("soupape", StringComparison.OrdinalIgnoreCase) || p.Nom.Contains("PAS", StringComparison.OrdinalIgnoreCase))) ||
                                                     (p.Designation != null && (p.Designation.Contains("soupape", StringComparison.OrdinalIgnoreCase) || p.Designation.Contains("PAS", StringComparison.OrdinalIgnoreCase))) ||
                                                     (p.Formulaire != null && p.Formulaire.CodeReference != null && (p.Formulaire.CodeReference.Contains("soupape", StringComparison.OrdinalIgnoreCase) || p.Formulaire.CodeReference.Contains("PAS", StringComparison.OrdinalIgnoreCase))) ||
                                                     (p.Formulaire != null && p.Formulaire.Designation != null && (p.Formulaire.Designation.Contains("soupape", StringComparison.OrdinalIgnoreCase) || p.Formulaire.Designation.Contains("PAS", StringComparison.OrdinalIgnoreCase))) ||
                                                     (p.FamilleProduitFiniCode != null && (p.FamilleProduitFiniCode.Contains("soupape", StringComparison.OrdinalIgnoreCase) || p.FamilleProduitFiniCode.Contains("PAS", StringComparison.OrdinalIgnoreCase))));
            }
        }
        if (match == null) match = plansAss.FirstOrDefault(p => p.FamilleProduitFiniCode == codeArticle || (p.Nom != null && p.Nom.Contains(codeArticle, StringComparison.OrdinalIgnoreCase)) || (p.Designation != null && p.Designation.Contains(codeArticle, StringComparison.OrdinalIgnoreCase)));
        if (match == null && !string.IsNullOrEmpty(familleCode)) match = plansAss.FirstOrDefault(p => p.FamilleProduitFiniCode == familleCode);
        if (match == null) match = plansAss.FirstOrDefault(p => p.FamilleProduitFiniCode == "GEN" || string.IsNullOrEmpty(p.FamilleProduitFiniCode));

        return match ?? plansAss.FirstOrDefault();
    }

    public async Task<DocumentEntete?> GetPlanControlePosteAsync(string posteCode, string codeArticle)
    {
        var pf = await _context.ProduitFinis.FirstOrDefaultAsync(p => p.CodeArticle == codeArticle);
        bool isSoupape = pf != null && !string.IsNullOrEmpty(pf.FamilleProduitFiniCode) && pf.FamilleProduitFiniCode.IndexOf("soupape", StringComparison.OrdinalIgnoreCase) >= 0;

        var plans = await _context.DocumentEntetes
            .Include(p => p.Formulaire)
            .Where(p => (string.IsNullOrEmpty(posteCode) || p.PosteCode == posteCode || p.PosteCode == "TOUS") &&
                        (p.TypeDocumentCode == "CTRL_POSTE" || p.TypeDocumentCode == "RESULTAT_CONTROLE_POSTE" || p.TypeDocumentCode == "RESULTAT_CF") && 
                        p.Statut == "ACTIF")
            .ToListAsync();
            
        if (isSoupape)
        {
            return plans.FirstOrDefault(p => p.Formulaire != null && p.Formulaire.CodeReference != null && p.Formulaire.CodeReference.Contains("SOUPAPE", StringComparison.OrdinalIgnoreCase)) ?? plans.FirstOrDefault();
        }
        else
        {
            return plans.FirstOrDefault(p => p.Formulaire == null || p.Formulaire.CodeReference == null || !p.Formulaire.CodeReference.Contains("SOUPAPE", StringComparison.OrdinalIgnoreCase)) ?? plans.FirstOrDefault();
        }
    }

    public async Task<IEnumerable<ExecControleDocumentStatut>> GetDocumentStatutsAsync(Guid execControleOfId)
    {
        return await _context.Set<ExecControleDocumentStatut>()
            .Where(d => d.ExecControleOfId == execControleOfId)
            .ToListAsync();
    }

    public async Task<ExecControleDocumentStatut?> GetDocumentStatutByIdAsync(Guid statutId)
    {
        return await _context.Set<ExecControleDocumentStatut>()
            .Include(d => d.ExecControleOf)
                .ThenInclude(e => e.NumeroOfNavigation)
                    .ThenInclude(o => o.CodeArticleNavigation)
            .FirstOrDefaultAsync(d => d.Id == statutId);
    }

    public void AddExecControleDocumentStatut(ExecControleDocumentStatut statut)
    {
        _context.ExecControleDocumentStatuts.Add(statut);
    }

    public void UpdateExecControleDocumentStatut(ExecControleDocumentStatut statut)
    {
        _context.ExecControleDocumentStatuts.Update(statut);
    }

    public async Task<IEnumerable<RefFormulaire>> GetFormulairesPourPosteAsync(string posteCode, string role)
    {
        // Pour un poste donné et un rôle (ex: RESULTAT_CONTROLE_POSTE)
        return await _context.RefFormulaires
            .Where(f => f.Statut == "ACTIF" && f.Role == role && f.CodeReference.Contains(posteCode))
            .ToListAsync();
    }

    public async Task<IEnumerable<RefFormulaire>> GetFormulairesPourMachineAsync(string machineCode, string role)
    {
        var codeTrim = machineCode?.Trim();
        var planActif = await _context.DocumentVerifMachineEntetes
            .Include(p => p.Formulaire)
            .Where(p => p.MachineCode != null && p.MachineCode.Trim() == codeTrim && (p.Statut == "ACTIF" || p.Statut == "Actif"))
            .FirstOrDefaultAsync();

        if (planActif == null)
        {
            throw new Exception($"Aucun plan de vérification actif n'a été trouvé pour cette machine ({machineCode}). Veuillez signaler au superviseur pour le créer.");
        }

        return new List<RefFormulaire> { new RefFormulaire { Id = planActif.Id } };
    }

    public async Task<IEnumerable<RefFormulaire>> GetFormulairesPourArticleAsync(string codeArticle, string role)
    {
        if (role == "PLAN_ASS" || role == "PLAN_ASSEMBLAGE")
        {
            var planAss = await GetPlanAssemblageActifAsync(codeArticle);
            if (planAss != null)
            {
                return new List<RefFormulaire>
                {
                    new RefFormulaire
                    {
                        Id = planAss.Id,
                        CodeReference = planAss.TypeDocumentCode,
                        Designation = planAss.Nom ?? planAss.Designation ?? "Plan Assemblage",
                        Statut = "ACTIF"
                    }
                };
            }
            return new List<RefFormulaire>();
        }

        // Pour un article et un rôle (ex: PRODUIT_FINI, RESULTAT_CONTROLE_CF, ECHANTILLONNAGE)
        // La logique ici peut dépendre de la famille, mais comme c'est un repo on peut faire une recherche par mot clé
        // Ou utiliser les plans directement si ça existe.
        // Ici on simplifie pour correspondre aux RefFormulaires
        var of = await _context.Articles.Include(a => a.NatureArticleCodeNavigation).FirstOrDefaultAsync(a => a.CodeArticle == codeArticle);
        var famillePF = _context.ProduitFinis.FirstOrDefault(p => p.CodeArticle == codeArticle)?.FamilleProduitFiniCode;

        if (string.IsNullOrEmpty(famillePF)) return new List<RefFormulaire>();

        return await _context.RefFormulaires
            .Where(f => f.Statut == "ACTIF" && f.Role == role && (f.CodeReference.Contains(famillePF) || f.CodeReference.Contains(codeArticle)))
            .ToListAsync();
    }

    public async Task<RefFormulaire?> GetFormulaireGlobalAsync(string role)
    {
        return await _context.RefFormulaires
            .FirstOrDefaultAsync(f => f.Statut == "ACTIF" && f.Role == role);
    }

    public async Task<Dictionary<Guid, string>> GetFormulaireDesignationsAsync(IEnumerable<Guid> ids)
    {
        var idList = ids.Distinct().ToList();
        if (!idList.Any()) return new Dictionary<Guid, string>();

        var refForms = await _context.RefFormulaires
            .Where(f => idList.Contains(f.Id))
            .ToDictionaryAsync(f => f.Id, f => f.Designation ?? f.CodeReference);

        var docEntetes = await _context.DocumentEntetes
            .Where(d => idList.Contains(d.Id))
            .Select(d => new { d.Id, Designation = !string.IsNullOrEmpty(d.Designation) ? d.Designation : d.Nom })
            .ToDictionaryAsync(d => d.Id, d => d.Designation);

        var result = new Dictionary<Guid, string>(refForms);
        foreach (var kvp in docEntetes)
        {
            if (!result.ContainsKey(kvp.Key) && !string.IsNullOrEmpty(kvp.Value))
            {
                result[kvp.Key] = kvp.Value;
            }
        }
        return result;
    }

    public async Task<IEnumerable<ExecControleOf>> GetExecsAssemblageEnCoursAsync()
    {
        return await _context.ExecControleOfs
            .Include(e => e.ExecControleOfPostes)
            .Where(e => e.TypeOf == "ASS" && e.Statut != "CLOTURE" && e.Statut != "TERMINE")
            .ToListAsync();
    }

    public void AddExecControleOfPoste(ExecControleOfPoste poste)
    {
        _context.ExecControleOfPostes.Add(poste);
    }

    public async Task<IEnumerable<Machine>> GetMachinesByPosteAsync(string posteCode)
    {
        var poste = await _context.PosteTravails
            .Include(p => p.CodeMachines)
            .FirstOrDefaultAsync(p => p.CodePoste == posteCode);
            
        if (poste == null) return new List<Machine>();
        return poste.CodeMachines.Where(m => m.Actif).ToList();
    }

    public async Task<IEnumerable<Machine>> GetAllMachinesAsync()
    {
        return await _context.Machines.Where(m => m.Actif).ToListAsync();
    }

    public async Task<Dictionary<string, string>> GetMachineLabelsAsync(IEnumerable<string> machineCodes)
    {
        return await _context.Machines
            .Where(m => machineCodes.Contains(m.CodeMachine))
            .ToDictionaryAsync(m => m.CodeMachine, m => m.Libelle);
    }

    public async Task<List<string>> GetMachinesWithDemarragePlanAsync()
    {
        var plans = await _context.DocumentVerifMachineEntetes
            .Include(e => e.DocumentVerifMachineLignes)
                .ThenInclude(l => l.DocumentVerifMachineEcheances)
                    .ThenInclude(g => g.PeriodiciteMachine)
            .Where(e => e.Statut == "ACTIF" || e.Statut == "Actif")
            .Where(e => e.MachineCode != null)
            .ToListAsync();

        return plans
            .Where(p => p.DocumentVerifMachineLignes.Any(l => l.DocumentVerifMachineEcheances.Any(g => g.PeriodiciteMachine != null && g.PeriodiciteMachine.Libelle.ToLower().Contains("démarrage"))))
            .Select(p => p.MachineCode!)
            .Distinct()
            .ToList();
    }

    public async Task<Dictionary<string, List<string>>> GetMachinesPlanPeriodicitesAsync()
    {
        var plans = await _context.DocumentVerifMachineEntetes
            .Include(e => e.DocumentVerifMachineLignes)
                .ThenInclude(l => l.DocumentVerifMachineEcheances)
                    .ThenInclude(g => g.PeriodiciteMachine)
            .Where(e => e.Statut == "ACTIF" || e.Statut == "Actif")
            .Where(e => e.MachineCode != null)
            .ToListAsync();

        var result = new Dictionary<string, List<string>>();

        foreach (var p in plans)
        {
            if (p.MachineCode == null) continue;
            
            var periodicites = p.DocumentVerifMachineLignes
                .SelectMany(l => l.DocumentVerifMachineEcheances)
                .Where(e => e.PeriodiciteMachine != null)
                .Select(e => e.PeriodiciteMachine.Libelle.ToLower())
                .Distinct()
                .ToList();

            if (!result.ContainsKey(p.MachineCode))
            {
                result[p.MachineCode] = new List<string>();
            }

            result[p.MachineCode].AddRange(periodicites);
            result[p.MachineCode] = result[p.MachineCode].Distinct().ToList();
        }

        return result;
    }

    public async Task<bool> AjouterPostesAsync(Guid execControleOfId, List<string> posteCodes)
    {
        var existingPostes = await _context.ExecControleOfPostes
            .Where(p => p.ExecControleOfId == execControleOfId)
            .Select(p => p.PosteCode)
            .ToListAsync();

        var newPostes = posteCodes.Except(existingPostes).ToList();
        foreach(var p in newPostes)
        {
            _context.ExecControleOfPostes.Add(new ExecControleOfPoste
            {
                ExecControleOfId = execControleOfId,
                PosteCode = p
            });
        }
        await SaveChangesAsync();
        return true;
    }


    public void AddExecVerifMachineReponses(IEnumerable<ExecVerifMachineReponse> reponses)
    {
        _context.ExecVerifMachineReponses.AddRange(reponses);
    }

    public void RemoveExecVerifMachineReponses(IEnumerable<ExecVerifMachineReponse> reponses)
    {
        _context.ExecVerifMachineReponses.RemoveRange(reponses);
    }

    public async Task<List<ExecVerifMachineReponse>> GetExecVerifMachineReponsesAsync(Guid statutId, Guid? periodiciteId = null)
    {
        var query = _context.ExecVerifMachineReponses
            .Where(r => r.ExecControleDocumentStatutId == statutId);

        if (periodiciteId.HasValue)
        {
            query = query.Where(r => r.DocumentVerifMachineEcheance.PeriodiciteMachineId == periodiciteId.Value);
        }

        return await query.ToListAsync();
    }

    public async Task<List<ExecControleDocumentStatut>> GetDocumentStatutsForMachineAsync(Guid execControleOfId, string machineCode)
    {
        return await _context.ExecControleDocumentStatuts
            .Where(s => s.ExecControleOfId == execControleOfId && s.MachineCode == machineCode && s.TypeDocument == "VERIF_MACHINE")
            .OrderByDescending(s => s.DateExecution)
            .ThenBy(s => s.Equipe)
            .ToListAsync();
    }

    public async Task<IEnumerable<PeriodiciteMachine>> GetPeriodicitesMachineAsync()
    {
        return await _context.PeriodiciteMachines
            .Where(p => p.Actif)
            .OrderBy(p => p.OrdreAffichage)
            .ToListAsync();
    }
}

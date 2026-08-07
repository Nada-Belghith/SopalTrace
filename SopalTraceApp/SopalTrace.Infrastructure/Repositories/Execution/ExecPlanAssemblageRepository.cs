using Microsoft.EntityFrameworkCore;
using SopalTrace.Application.Interfaces.Execution;
using SopalTrace.Domain.Entities;
using SopalTrace.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SopalTrace.Infrastructure.Repositories.Execution
{
    public class ExecPlanAssemblageRepository : IExecPlanAssemblageRepository
    {
        private readonly SopalTraceDbContext _context;

        public ExecPlanAssemblageRepository(SopalTraceDbContext context)
        {
            _context = context;
        }

        public async Task<ExecControleOf?> GetExecOfAsync(Guid id)
        {
            return await _context.ExecControleOfs
                .Include(e => e.NumeroOfNavigation)
                    .ThenInclude(o => o.CodeArticleNavigation)
                .Include(e => e.ExecControleOfPostes)
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<int> GetPreviousExecOfsCountAsync(string numeroOf, DateTime? beforeDate = null)
        {
            var query = _context.ExecControleOfs.Where(e => e.NumeroOf == numeroOf);
            if (beforeDate.HasValue)
            {
                query = query.Where(e => e.DateDebut < beforeDate.Value);
            }
            return await query.CountAsync();
        }

        public async Task<int> GetPreviousEquipeExecCountAsync(string numeroOf, string equipe, DateTime? beforeDate = null)
        {
            var query = _context.ExecControleDocumentStatuts
                .Include(s => s.ExecControleOf)
                .Where(s => s.ExecControleOf.NumeroOf == numeroOf && s.Equipe == equipe);
            if (beforeDate.HasValue)
            {
                query = query.Where(s => s.ExecControleOf.DateDebut < beforeDate.Value);
            }
            return await query.CountAsync();
        }

        public async Task<DocumentEntete?> GetPlanAssemblageActifAsync(string codeArticle)
        {
            var (planAss, docRes) = await GetDocumentsAssemblageEtResultatAsync(codeArticle);
            return planAss ?? docRes;
        }

        public async Task<(DocumentEntete? PlanAss, DocumentEntete? DocRes)> GetDocumentsAssemblageEtResultatAsync(string codeArticle, string? posteCode = null)
        {
            var pf = await _context.ProduitFinis.FirstOrDefaultAsync(p => p.CodeArticle == codeArticle);
            var familleCode = pf?.FamilleProduitFiniCode;

            bool isSoupape = pf != null && !string.IsNullOrEmpty(pf.FamilleProduitFiniCode) && pf.FamilleProduitFiniCode.IndexOf("soupape", StringComparison.OrdinalIgnoreCase) >= 0 ||
                             !string.IsNullOrEmpty(codeArticle) && (codeArticle.IndexOf("PAS", StringComparison.OrdinalIgnoreCase) >= 0 || codeArticle.IndexOf("soupape", StringComparison.OrdinalIgnoreCase) >= 0);

            var plans = await _context.Set<DocumentEntete>()
                .Include(p => p.DocumentSections)
                    .ThenInclude(s => s.TypeSection)
                .Include(p => p.DocumentSections)
                    .ThenInclude(s => s.RegleEchantillonnage)
                .Include(p => p.DocumentSections)
                    .ThenInclude(s => s.DocumentLignes)
                        .ThenInclude(l => l.Caracteristique)
                .Include(p => p.DocumentSections)
                    .ThenInclude(s => s.DocumentLignes)
                        .ThenInclude(l => l.TypeControle)
                .Include(p => p.DocumentSections)
                    .ThenInclude(s => s.DocumentLignes)
                        .ThenInclude(l => l.MoyenControle)
                .Include(p => p.Formulaire)
                .Where(p => p.Statut == "ACTIF")
                .OrderByDescending(p => p.CreeLe)
                .ToListAsync();

            // 1. Plan d'assemblage (PLAN_ASS, PLAN_ASSEMBLAGE, PLAN_FAB)
            var plansAss = plans.Where(p => p.TypeDocumentCode == "PLAN_ASS" || p.TypeDocumentCode == "PLAN_ASSEMBLAGE" || p.TypeDocumentCode == "PLAN_FAB" || p.TypeDocumentCode == "MODELE_FAB").ToList();
            DocumentEntete? planAss = null;
            
            if (!string.IsNullOrEmpty(posteCode))
            {
                // Recherche STRICTE par poste : on cherche un doc qui contient explicitement le code poste.
                // Si aucun doc spécifique au poste n'existe (ex: PAS72 sans PLAN_ASS), on retourne null.
                planAss = plansAss
                    .Where(p => p.Statut == "ACTIF")
                    .FirstOrDefault(p =>
                        (p.Nom != null && p.Nom.Contains(posteCode, StringComparison.OrdinalIgnoreCase)) ||
                        (p.Designation != null && p.Designation.Contains(posteCode, StringComparison.OrdinalIgnoreCase)) ||
                        (p.Formulaire != null && p.Formulaire.CodeReference != null && p.Formulaire.CodeReference.Contains(posteCode, StringComparison.OrdinalIgnoreCase)));
                // Si posteCode fourni mais aucun doc trouvé → null (pas de fallback vers un autre poste)
            }
            else
            {
                // Pas de poste spécifié → comportement générique
                planAss = plansAss.FirstOrDefault(p => p.FamilleProduitFiniCode == codeArticle || (p.Nom != null && p.Nom.Contains(codeArticle, StringComparison.OrdinalIgnoreCase)) || (p.Designation != null && p.Designation.Contains(codeArticle, StringComparison.OrdinalIgnoreCase)));
                if (planAss == null && isSoupape)
                {
                    planAss = plansAss.FirstOrDefault(p =>
                        (p.Nom != null && (p.Nom.Contains("soupape", StringComparison.OrdinalIgnoreCase) || p.Nom.Contains("PAS", StringComparison.OrdinalIgnoreCase))) ||
                        (p.Designation != null && (p.Designation.Contains("soupape", StringComparison.OrdinalIgnoreCase) || p.Designation.Contains("PAS", StringComparison.OrdinalIgnoreCase))) ||
                        (p.Formulaire != null && p.Formulaire.CodeReference != null && (p.Formulaire.CodeReference.Contains("soupape", StringComparison.OrdinalIgnoreCase) || p.Formulaire.CodeReference.Contains("PAS", StringComparison.OrdinalIgnoreCase))) ||
                        (p.Formulaire != null && p.Formulaire.Designation != null && (p.Formulaire.Designation.Contains("soupape", StringComparison.OrdinalIgnoreCase) || p.Formulaire.Designation.Contains("PAS", StringComparison.OrdinalIgnoreCase))) ||
                        (p.FamilleProduitFiniCode != null && (p.FamilleProduitFiniCode.Contains("soupape", StringComparison.OrdinalIgnoreCase) || p.FamilleProduitFiniCode.Contains("PAS", StringComparison.OrdinalIgnoreCase))));
                }
                if (planAss == null && !string.IsNullOrEmpty(familleCode)) planAss = plansAss.FirstOrDefault(p => p.FamilleProduitFiniCode == familleCode);
                if (planAss == null) planAss = plansAss.FirstOrDefault(p => p.FamilleProduitFiniCode == "GEN" || string.IsNullOrEmpty(p.FamilleProduitFiniCode));
                if (planAss == null) planAss = plansAss.FirstOrDefault();
            }

            // 2. Document Résultat en cours d'assemblage (RESULTAT_CF, RCCF, CTRL_POSTE, RESULTAT_CONTROLE_POSTE, ou PLAN_ASS s'il contient des contrôles)
            var plansRes = plans.Where(p => p.TypeDocumentCode == "RESULTAT_CF" || p.TypeDocumentCode == "RCCF" || p.TypeDocumentCode == "CTRL_POSTE" || p.TypeDocumentCode == "RESULTAT_CONTROLE_POSTE").ToList();
            if (!plansRes.Any())
            {
                plansRes = plans.Where(p => p.TypeDocumentCode == "PLAN_ASS" || p.TypeDocumentCode == "PLAN_ASSEMBLAGE").ToList();
            }
            DocumentEntete? docResultat = null;

            if (!string.IsNullOrEmpty(posteCode))
            {
                // Recherche STRICTE par poste : on cherche un doc qui contient explicitement le code poste.
                // Si aucun doc spécifique au poste n'existe, on retourne null (pas de fallback).
                docResultat = plansRes
                    .Where(p => p.Statut == "ACTIF")
                    .FirstOrDefault(p =>
                        (p.Nom != null && p.Nom.Contains(posteCode, StringComparison.OrdinalIgnoreCase)) ||
                        (p.Designation != null && p.Designation.Contains(posteCode, StringComparison.OrdinalIgnoreCase)) ||
                        (p.Formulaire != null && p.Formulaire.CodeReference != null && p.Formulaire.CodeReference.Contains(posteCode, StringComparison.OrdinalIgnoreCase)));
                // Si posteCode fourni mais aucun doc trouvé → null (pas de fallback vers un autre poste)
            }
            else
            {
                // Pas de poste spécifié → comportement générique
                if (isSoupape)
                {
                    docResultat = plansRes.FirstOrDefault(p => p.FamilleProduitFiniCode == codeArticle || (p.Nom != null && p.Nom.Contains(codeArticle, StringComparison.OrdinalIgnoreCase)) || (p.Designation != null && p.Designation.Contains(codeArticle, StringComparison.OrdinalIgnoreCase)) || (p.Formulaire != null && p.Formulaire.CodeReference != null && p.Formulaire.CodeReference.Contains(codeArticle, StringComparison.OrdinalIgnoreCase)));
                    if (docResultat == null)
                    {
                        docResultat = plansRes.FirstOrDefault(p =>
                            (p.Nom != null && (p.Nom.Contains("soupape", StringComparison.OrdinalIgnoreCase) || p.Nom.Contains("PAS", StringComparison.OrdinalIgnoreCase))) ||
                            (p.Designation != null && (p.Designation.Contains("soupape", StringComparison.OrdinalIgnoreCase) || p.Designation.Contains("PAS", StringComparison.OrdinalIgnoreCase))) ||
                            (p.Formulaire != null && p.Formulaire.CodeReference != null && (p.Formulaire.CodeReference.Contains("soupape", StringComparison.OrdinalIgnoreCase) || p.Formulaire.CodeReference.Contains("PAS", StringComparison.OrdinalIgnoreCase))) ||
                            (p.Formulaire != null && p.Formulaire.Designation != null && (p.Formulaire.Designation.Contains("soupape", StringComparison.OrdinalIgnoreCase) || p.Formulaire.Designation.Contains("PAS", StringComparison.OrdinalIgnoreCase))) ||
                            (p.FamilleProduitFiniCode != null && (p.FamilleProduitFiniCode.Contains("soupape", StringComparison.OrdinalIgnoreCase) || p.FamilleProduitFiniCode.Contains("PAS", StringComparison.OrdinalIgnoreCase))));
                    }
                }
                if (docResultat == null) docResultat = plansRes.FirstOrDefault(p => p.FamilleProduitFiniCode == codeArticle || (p.Nom != null && p.Nom.Contains(codeArticle, StringComparison.OrdinalIgnoreCase)) || (p.Designation != null && p.Designation.Contains(codeArticle, StringComparison.OrdinalIgnoreCase)));
                if (docResultat == null && !string.IsNullOrEmpty(familleCode)) docResultat = plansRes.FirstOrDefault(p => p.FamilleProduitFiniCode == familleCode);
                if (docResultat == null) docResultat = plansRes.FirstOrDefault(p => p.FamilleProduitFiniCode == "GEN" || string.IsNullOrEmpty(p.FamilleProduitFiniCode));
                if (docResultat == null) docResultat = plansRes.FirstOrDefault();
            }

            return (planAss, docResultat);
        }

        public async Task<DocumentEntete?> GetDocumentByIdAsync(Guid docId)
        {
            return await _context.DocumentEntetes
                .Include(p => p.DocumentSections)
                    .ThenInclude(s => s.TypeSection)
                .Include(p => p.DocumentSections)
                    .ThenInclude(s => s.RegleEchantillonnage)
                .Include(p => p.DocumentSections)
                    .ThenInclude(s => s.DocumentLignes)
                        .ThenInclude(l => l.Caracteristique)
                .Include(p => p.DocumentSections)
                    .ThenInclude(s => s.DocumentLignes)
                        .ThenInclude(l => l.TypeControle)
                .Include(p => p.DocumentSections)
                    .ThenInclude(s => s.DocumentLignes)
                        .ThenInclude(l => l.MoyenControle)
                .Include(p => p.Formulaire)
                .FirstOrDefaultAsync(p => p.Id == docId);
        }

        public async Task<ExecEchantillonnage?> GetExecEchantillonnageAsync(Guid execControleOfId)
        {
            return await _context.ExecEchantillonnages
                .Include(e => e.ExecControleDocumentStatut)
                .FirstOrDefaultAsync(e => e.ExecControleDocumentStatut.ExecControleOfId == execControleOfId);
        }

        public async Task<List<ExecControleTranche>> GetTranchesAsync(Guid execControleOfId)
        {
            return await _context.ExecControleTranches
                .Where(t => t.ExecControleOfid == execControleOfId)
                .OrderBy(t => t.HeureDebut)
                .ToListAsync();
        }

        public async Task<ExecControleTranche?> GetTrancheByIdAsync(Guid trancheId)
        {
            return await _context.ExecControleTranches
                .FirstOrDefaultAsync(t => t.Id == trancheId);
        }

        public void AddTranche(ExecControleTranche tranche)
        {
            _context.ExecControleTranches.Add(tranche);
        }

        public void RemoveTranches(IEnumerable<ExecControleTranche> tranches)
        {
            _context.ExecControleTranches.RemoveRange(tranches);
        }

        public async Task<ExecControleDocumentStatut?> GetStatutPlanAssAsync(Guid execControleOfId, string? posteCode = null)
        {
            var query = _context.ExecControleDocumentStatuts
                .Where(s => s.ExecControleOfId == execControleOfId && 
                           (s.TypeDocument == "PLAN_ASS" || s.TypeDocument == "PLAN_ASSEMBLAGE"));
            
            if (!string.IsNullOrEmpty(posteCode))
                query = query.Where(s => s.PosteCode == posteCode);
                
            return await query.FirstOrDefaultAsync();
        }

        public async Task<ExecControleDocumentStatut?> GetStatutResultatCfAsync(Guid execControleOfId, Guid docId, string? posteCode = null)
        {
            var query = _context.ExecControleDocumentStatuts
                .Where(s => s.ExecControleOfId == execControleOfId && 
                           (s.DocId == docId || s.TypeDocument == "RESULTAT_CF" || s.TypeDocument == "RCCF" || s.TypeDocument == "CTRL_POSTE"));
                           
            if (!string.IsNullOrEmpty(posteCode))
                query = query.Where(s => s.PosteCode == posteCode);
                
            return await query.FirstOrDefaultAsync();
        }

        public async Task<List<ExecControleDocumentStatut>> GetStatutsAssemblageAsync(Guid execControleOfId)
        {
            return await _context.ExecControleDocumentStatuts
                .Where(s => s.ExecControleOfId == execControleOfId && 
                           (s.TypeDocument == "PLAN_ASS" || s.TypeDocument == "PLAN_ASSEMBLAGE" || s.TypeDocument == "RESULTAT_CF" || s.TypeDocument == "RCCF" || s.TypeDocument == "CTRL_POSTE"))
                .ToListAsync();
        }

        public void AddStatut(ExecControleDocumentStatut statut)
        {
            _context.ExecControleDocumentStatuts.Add(statut);
        }

        public async Task<Guid?> GetUtilisateurIdByMatriculeAsync(string? matricule)
        {
            if (string.IsNullOrEmpty(matricule)) return null;
            var user = await _context.UtilisateursApps.FirstOrDefaultAsync(u => u.Matricule == matricule || u.NomComplet == matricule);
            return user?.Id;
        }

        public async Task<Guid> GetDefaultUtilisateurIdAsync()
        {
            var user = await _context.UtilisateursApps.FirstOrDefaultAsync();
            return user?.Id ?? Guid.Empty;
        }

        public void AddLigneReponses(IEnumerable<ExecControleLigneReponse> reponses)
        {
            _context.ExecControleLigneReponses.AddRange(reponses);
        }

        public async Task<List<ExecControleLigneReponse>> GetLigneReponsesAsync(Guid execControleOfId)
        {
            return await _context.ExecControleLigneReponses
                .Where(r => r.ExecControleOfid == execControleOfId)
                .ToListAsync();
        }

        public async Task<ExecControleLigneReponse?> GetLigneReponseByLigneIdAsync(Guid execControleOfId, Guid ligneId, string contexte)
        {
            return await _context.ExecControleLigneReponses
                .Where(r => r.ExecControleOfid == execControleOfId && r.DocumentLigneId == ligneId && r.Contexte == contexte)
                .OrderByDescending(r => r.DateSaisie)
                .FirstOrDefaultAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}

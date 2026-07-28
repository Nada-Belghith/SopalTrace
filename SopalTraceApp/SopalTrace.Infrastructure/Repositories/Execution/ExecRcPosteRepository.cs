using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SopalTrace.Application.Interfaces.Execution;
using SopalTrace.Domain.Entities;
using SopalTrace.Infrastructure.Data;

namespace SopalTrace.Infrastructure.Repositories.Execution
{
    public class ExecRcPosteRepository : IExecRcPosteRepository
    {
        private readonly SopalTraceDbContext _context;

        public ExecRcPosteRepository(SopalTraceDbContext context)
        {
            _context = context;
        }

        public async Task<ExecControleDocumentStatut?> GetStatutWithDetailsAsync(Guid execControleOfId, string posteCode, string equipe, DateTime dateExecution)
        {
            return await _context.ExecControleDocumentStatuts
                .Include(s => s.ExecRcPosteHeures)
                .Include(s => s.ExecRcPosteReponses)
                .Include(s => s.ExecRcPosteBilans)
                .FirstOrDefaultAsync(s => s.ExecControleOfId == execControleOfId 
                                          && s.PosteCode == posteCode 
                                          && s.TypeDocument == "RESULTAT_CONTROLE_POSTE"
                                          && s.DateExecution == dateExecution);
        }

        public async Task<List<ExecControleDocumentStatut>> GetAllStatutsAsync(Guid execControleOfId, string posteCode)
        {
            return await _context.ExecControleDocumentStatuts
                .Where(s => s.ExecControleOfId == execControleOfId 
                            && s.PosteCode == posteCode 
                            && s.TypeDocument == "RESULTAT_CONTROLE_POSTE")
                .OrderByDescending(s => s.DateExecution)
                .ToListAsync();
        }

        public async Task<ExecControleDocumentStatut?> GetStatutByIdWithDetailsAsync(Guid id)
        {
            return await _context.ExecControleDocumentStatuts
                .Include(s => s.ExecRcPosteHeures)
                .Include(s => s.ExecRcPosteReponses)
                .Include(s => s.ExecRcPosteBilans)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<DocumentEntete?> GetPlanByIdAsync(Guid id)
        {
            return await _context.DocumentEntetes
                .Include(p => p.Formulaire)
                .Include(p => p.DocumentLignes)
                    .ThenInclude(l => l.RisqueDefaut)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<DocumentEntete?> GetPlanActifByPosteAsync(string posteCode, Guid execControleOfId)
        {
            var execOf = await _context.ExecControleOfs.FirstOrDefaultAsync(e => e.Id == execControleOfId);
            var of = execOf != null ? await _context.MfgheadOrdreFabrications.FirstOrDefaultAsync(o => o.NumeroOf == execOf.NumeroOf) : null;
            var pf = of != null ? await _context.ProduitFinis.FirstOrDefaultAsync(p => p.CodeArticle == of.CodeArticle) : null;
            
            bool isSoupape = pf != null && !string.IsNullOrEmpty(pf.FamilleProduitFiniCode) && pf.FamilleProduitFiniCode.IndexOf("soupape", StringComparison.OrdinalIgnoreCase) >= 0;

            var plans = await _context.DocumentEntetes
                .Include(p => p.Formulaire)
                .Include(p => p.DocumentLignes)
                    .ThenInclude(l => l.RisqueDefaut)
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

        public void RemoveHeures(IEnumerable<ExecRcPosteHeure> heures) => _context.ExecRcPosteHeures.RemoveRange(heures);
        public void RemoveReponses(IEnumerable<ExecRcPosteReponse> reponses) => _context.ExecRcPosteReponses.RemoveRange(reponses);
        public void RemoveBilans(IEnumerable<ExecRcPosteBilan> bilans) => _context.ExecRcPosteBilans.RemoveRange(bilans);

        public void AddHeure(ExecRcPosteHeure heure) => _context.ExecRcPosteHeures.Add(heure);
        public void AddReponse(ExecRcPosteReponse reponse) => _context.ExecRcPosteReponses.Add(reponse);
        public void AddBilan(ExecRcPosteBilan bilan) => _context.ExecRcPosteBilans.Add(bilan);
        public void AddStatut(ExecControleDocumentStatut statut) => _context.ExecControleDocumentStatuts.Add(statut);

        public async Task SaveChangesAsync() => await _context.SaveChangesAsync();
    }
}

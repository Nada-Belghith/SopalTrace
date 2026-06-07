using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SopalTrace.Application.Interfaces;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;
using SopalTrace.Infrastructure.Data;

namespace SopalTrace.Infrastructure.Repositories
{
    public class PlanRccfRepository : IPlanRccfRepository
    {
        private readonly SopalTraceDbContext _context;

        public PlanRccfRepository(SopalTraceDbContext context)
            => _context = context;

        public async Task<List<PlanResultatControleCfEntete>> GetTousLesPlansAsync()
        {
            return await _context.PlanResultatControleCfEntetes
                .Include(p => p.PlanResultatControleCfSections)
                    .ThenInclude(s => s.PlanResultatControleCfLignes)
                .ToListAsync();
        }

        public async Task<PlanResultatControleCfEntete?> GetPlanAvecRelationsAsync(Guid id)
        {
            return await _context.PlanResultatControleCfEntetes
                .Include(p => p.PlanResultatControleCfSections)
                    .ThenInclude(s => s.PlanResultatControleCfLignes)
                .Include(p => p.PosteCodeNavigation)
                .Include(p => p.Formulaire)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<PlanResultatControleCfEntete?> GetPlanActifAsync(string posteCode, Guid formulaireId)
        {
            return await _context.PlanResultatControleCfEntetes
                .Include(p => p.PlanResultatControleCfSections)
                    .ThenInclude(s => s.PlanResultatControleCfLignes)
                .FirstOrDefaultAsync(p => p.PosteCode == posteCode && p.FormulaireId == formulaireId && p.Statut == StatutsPlan.Actif);
        }

        public async Task AddPlanAsync(PlanResultatControleCfEntete plan)
        {
            await _context.PlanResultatControleCfEntetes.AddAsync(plan);
        }

        public void RemoveSection(PlanResultatControleCfSection section)
        {
            _context.PlanResultatControleCfSections.Remove(section);
        }

        public void AddSection(PlanResultatControleCfSection section)
        {
            _context.PlanResultatControleCfSections.Add(section);
        }

        public void RemoveLigne(PlanResultatControleCfLigne ligne)
        {
            _context.PlanResultatControleCfLignes.Remove(ligne);
        }

        public void AddLigne(PlanResultatControleCfLigne ligne)
        {
            _context.PlanResultatControleCfLignes.Add(ligne);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}

using Microsoft.EntityFrameworkCore;
using SopalTrace.Application.Interfaces;
using SopalTrace.Domain.Entities;
using SopalTrace.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SopalTrace.Infrastructure.Repositories;

public class PlanVerifMachineRepository : IPlanVerifMachineRepository
{
    private readonly SopalTraceDbContext _context;

    public PlanVerifMachineRepository(SopalTraceDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ExistePlanActifAsync(string machineCode)
    {
        return await _context.PlanVerifMachineEntetes.AnyAsync(p =>
            p.MachineCode == machineCode &&
            p.Statut == "ACTIF");
    }

    public async Task<PlanVerifMachineEntete> GetPlanActifAsync(string machineCode)
    {
        return await _context.PlanVerifMachineEntetes.FirstOrDefaultAsync(p =>
            p.MachineCode == machineCode &&
            p.Statut == "ACTIF");
    }

    public async Task<PlanVerifMachineEntete> GetPlanAvecRelationsAsync(Guid planId)
    {
        // 4 niveaux d'Include pour ramener l'arbre complet
        return await _context.PlanVerifMachineEntetes
            .Include(p => p.PlanVerifMachineFamilles)
            .Include(p => p.PlanVerifMachineLignes)
                .ThenInclude(l => l.PlanVerifMachineEcheances)
                    .ThenInclude(e => e.PlanVerifMachineMatricePieces)
                        .ThenInclude(mp => mp.Famille)
            .FirstOrDefaultAsync(p => p.Id == planId);
    }

    public async Task<List<PlanVerifMachineEntete>> GetTousLesPlanAsync()
    {
        return await _context.PlanVerifMachineEntetes
            .Include(p => p.PlanVerifMachineFamilles)
            .Include(p => p.PlanVerifMachineLignes)
                .ThenInclude(l => l.PlanVerifMachineEcheances)
                    .ThenInclude(e => e.PlanVerifMachineMatricePieces)
                        .ThenInclude(mp => mp.Famille)
            .ToListAsync();
    }

    public async Task AddPlanAsync(PlanVerifMachineEntete plan)
    {
        await _context.PlanVerifMachineEntetes.AddAsync(plan);
    }

    public async Task<Guid> GetDefaultRefMoyenDetectionIdAsync()
    {
        var defaultMoyenId = await _context.RefMoyenDetections.Select(r => r.Id).FirstOrDefaultAsync();
        if (defaultMoyenId == Guid.Empty)
        {
            var newRef = new RefMoyenDetection { Id = Guid.NewGuid(), Code = "DEF", Designation = "Par défaut", Actif = true };
            _context.RefMoyenDetections.Add(newRef);
            await _context.SaveChangesAsync();
            defaultMoyenId = newRef.Id;
        }
        return defaultMoyenId;
    }

    public void RemoveLigne(PlanVerifMachineLigne ligne) => _context.PlanVerifMachineLignes.Remove(ligne);
    public void RemoveEcheance(PlanVerifMachineEcheance echeance) => _context.PlanVerifMachineEcheances.Remove(echeance);
    public void RemoveMatricePiece(PlanVerifMachineMatricePiece matricePiece) => _context.Remove(matricePiece);
}

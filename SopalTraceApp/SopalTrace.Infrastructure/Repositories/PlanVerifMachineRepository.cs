using Microsoft.EntityFrameworkCore;
using SopalTrace.Application.Interfaces;
using SopalTrace.Domain.Entities;
using SopalTrace.Infrastructure.Data;
using System;
using System.Threading.Tasks;

namespace SopalTrace.Infrastructure.Repositories;

public class PlanVerifMachineRepository : IPlanVerifMachineRepository
{
    private readonly SopalTraceDbContext _context;

    public PlanVerifMachineRepository(SopalTraceDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ExistePlanActifAsync(string machineCode, string typeRapport, string typeRobinetCode)
    {
        return await _context.PlanVerifMachineEntetes.AnyAsync(p =>
            p.MachineCode == machineCode &&
            p.TypeRapport == typeRapport &&
            p.TypeRobinetCode == typeRobinetCode &&
            p.Statut == "ACTIF");
    }

    public async Task<PlanVerifMachineEntete> GetPlanActifAsync(string machineCode, string typeRapport, string typeRobinetCode)
    {
        return await _context.PlanVerifMachineEntetes.FirstOrDefaultAsync(p =>
            p.MachineCode == machineCode &&
            p.TypeRapport == typeRapport &&
            p.TypeRobinetCode == typeRobinetCode &&
            p.Statut == "ACTIF");
    }

    public async Task<PlanVerifMachineEntete> GetPlanAvecRelationsAsync(Guid planId)
    {
        // 4 niveaux d'Include pour ramener l'arbre complet + jointure sur Formulaire
        return await _context.PlanVerifMachineEntetes
            .Include(p => p.Formulaire)
            .Include(p => p.PlanVerifMachineLignes)
                .ThenInclude(l => l.PlanVerifMachineEcheances)
                    .ThenInclude(e => e.PlanVerifMachinePieceRefs)
            .FirstOrDefaultAsync(p => p.Id == planId);
    }

    public async Task AddPlanAsync(PlanVerifMachineEntete plan)
    {
        await _context.PlanVerifMachineEntetes.AddAsync(plan);
    }

    public void RemoveLigne(PlanVerifMachineLigne ligne) => _context.PlanVerifMachineLignes.Remove(ligne);
    public void RemoveEcheance(PlanVerifMachineEcheance echeance) => _context.PlanVerifMachineEcheances.Remove(echeance);
    public void RemovePieceRef(PlanVerifMachinePieceRef pieceRef) => _context.PlanVerifMachinePieceRefs.Remove(pieceRef);
}

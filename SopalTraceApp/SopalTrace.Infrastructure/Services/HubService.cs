using Microsoft.EntityFrameworkCore;
using SopalTrace.Application.DTOs.QualityPlans.Hub;
using SopalTrace.Application.Interfaces;
using SopalTrace.Infrastructure.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SopalTrace.Infrastructure.Services;

public class HubService : IHubService
{
    private readonly SopalTraceDbContext _context;

    public HubService(SopalTraceDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<HubModeleDto>> GetTousLesModelesAsync()
    {
        var result = new List<HubModeleDto>();

        var fabModeles = await _context.ModeleFabEntetes
            .AsNoTracking()
            .Where(m => m.Statut != "ARCHIVE")
            .Select(m => new HubModeleDto(
                m.Id,
                "FAB",
                m.Libelle,
                m.NatureComposantCode,
                m.TypeRobinetCode,
                m.OperationCode,
                m.Version,
                "Gabarit de fabrication générique."))
            .ToListAsync();
        result.AddRange(fabModeles);

        var assModeles = await _context.PlanAssEntetes
            .AsNoTracking()
            .Where(m => m.EstModele && m.Statut != "ARCHIVE")
            .Select(m => new HubModeleDto(
                m.Id,
                "ASS",
                m.Nom,
                "N/A",
                m.TypeRobinetCode,
                m.OperationCode,
                m.Version,
                "Plan Maître d'assemblage."))
            .ToListAsync();
        result.AddRange(assModeles);

        var vmModeles = await _context.PlanVerifMachineEntetes
            .AsNoTracking()
            .Where(m => m.Statut != "ARCHIVE")
            .Select(m => new HubModeleDto(
                m.Id,
                "VM",
                m.Nom,
                "MACHINE",
                m.TypeRapport,
                m.MachineCode,
                m.Version,
                "Vérification des étalons machines."))
            .ToListAsync();
        result.AddRange(vmModeles);

        var echModeles = await _context.PlanEchantillonnageEntetes
            .AsNoTracking()
            .Where(m => m.CodeArticleSage == null && m.Statut != "ARCHIVE")
            .Join(_context.Nqas, m => m.NqaId, n => n.Id, (m, n) => new HubModeleDto(
                m.Id,
                "ECH",
                "Profil: " + m.CodeReference,
                "N/A",
                m.TypePlan,
                "NQA " + n.ValeurNqa,
                m.Version,
                "Niveau de contrôle: " + m.NiveauControle))
            .ToListAsync();
        result.AddRange(echModeles);

        return result;
    }
}
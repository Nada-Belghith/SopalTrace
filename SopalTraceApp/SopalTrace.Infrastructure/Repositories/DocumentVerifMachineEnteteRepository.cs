using Microsoft.EntityFrameworkCore;
using SopalTrace.Application.Interfaces.Repositories;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;
using SopalTrace.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SopalTrace.Infrastructure.Repositories;

/// <summary>
/// Plans de vérification machine : Document_Entete de type VERIF_MACHINE, lignes sans section,
/// extensions Famille / Échéance / MatricePiece.
/// </summary>
public class DocumentVerifMachineEnteteRepository : IDocumentVerifMachineEnteteRepository
{
    private readonly SopalTraceDbContext _context;

    public DocumentVerifMachineEnteteRepository(SopalTraceDbContext context)
    {
        _context = context;
    }

    private IQueryable<DocumentEntete> Plans =>
        _context.DocumentEntetes.Where(p => p.TypeDocumentCode == TypesDocument.VerifMachine);

    private static IQueryable<DocumentEntete> AvecStructure(IQueryable<DocumentEntete> query) => query
        .AsSplitQuery()
        .Include(p => p.Formulaire)
        .Include(p => p.DocumentVerifMachineFamilles)
        .Include(p => p.DocumentLignes)
            .ThenInclude(l => l.DocumentLigneExtraColonnes)
        .Include(p => p.DocumentLignes)
            .ThenInclude(l => l.DocumentVerifMachineEcheances)
                .ThenInclude(e => e.DocumentVerifMachineMatricePieces);

    public async Task<DocumentEntete?> GetByIdAsync(Guid id, bool includeRelations = true)
    {
        var query = includeRelations ? AvecStructure(Plans) : Plans;
        return await query.FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<IEnumerable<DocumentEntete>> GetAllWithRelationsAsync()
    {
        return await AvecStructure(Plans).ToListAsync();
    }

    public async Task<IEnumerable<DocumentEntete>> GetByMachineCodeAsync(string machineCode)
    {
        var codeTrim = machineCode?.Trim();
        return await AvecStructure(Plans.Where(p => p.MachineCode == codeTrim)).ToListAsync();
    }

    public async Task<IEnumerable<DocumentEntete>> GetByFormulaireIdAsync(Guid formulaireId)
    {
        return await Plans
            .Where(p => p.FormulaireId == formulaireId)
            .ToListAsync();
    }

    public async Task AddAsync(DocumentEntete entity)
    {
        entity.TypeDocumentCode = TypesDocument.VerifMachine;
        entity.Portee = PorteesDocument.Generique;
        await _context.DocumentEntetes.AddAsync(entity);
    }

    public Task UpdateAsync(DocumentEntete entity)
    {
        // Pas de _context.Update(entity) : il passerait toutes les entités enfants en Modified.
        // Le suivi des modifications d'EF Core détecte les changements des entités suivies.
        return Task.CompletedTask;
    }

    public Task DeleteAsync(DocumentEntete entity)
    {
        // Les lignes ne sont pas supprimées en cascade depuis l'entête (pas de section) : on les retire d'abord.
        _context.DocumentLignes.RemoveRange(entity.DocumentLignes);
        _context.DocumentEntetes.Remove(entity);
        return Task.CompletedTask;
    }

    public void RemoveFamille(DocumentVerifMachineFamille famille)
    {
        _context.DocumentVerifMachineFamilles.Remove(famille);
    }

    public void AddFamille(DocumentVerifMachineFamille famille)
    {
        _context.DocumentVerifMachineFamilles.Add(famille);
    }

    public void RemoveLigne(DocumentLigne ligne)
    {
        _context.DocumentLignes.Remove(ligne);
    }

    public void AddLigne(DocumentLigne ligne)
    {
        _context.DocumentLignes.Add(ligne);
    }
}

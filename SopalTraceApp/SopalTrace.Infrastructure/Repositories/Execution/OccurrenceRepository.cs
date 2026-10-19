using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SopalTrace.Application.Interfaces.Execution;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;
using SopalTrace.Infrastructure.Data;

namespace SopalTrace.Infrastructure.Repositories.Execution;

public class OccurrenceRepository : IOccurrenceRepository
{
    private readonly SopalTraceDbContext _context;

    public OccurrenceRepository(SopalTraceDbContext context)
    {
        _context = context;
    }

    public async Task<ExecControleOf?> GetExecControleOfWithIntermediairesAsync(Guid execControleOfId)
    {
        return await _context.ExecControleOfs
            .Include(o => o.ExecPrelevementIntermediaires)
            .Include(o => o.ExecPieceTypes)
            .Include(o => o.ExecControleTranches)
            .Include(o => o.ExecControleDocumentStatuts)
            .Include(o => o.ExecControleOfSessions)
            .Include(o => o.NumeroOfNavigation)
            .FirstOrDefaultAsync(o => o.Id == execControleOfId);
    }

    public async Task<Guid?> GetSessionIdDuPosteAsync(Guid execControleOfId, string posteCode)
    {
        return await _context.ExecSessionPosts
            .Where(p => p.ExecControleOfId == execControleOfId && p.PosteCode == posteCode && p.EndedAt == null)
            .Select(p => (Guid?)p.SessionId)
            .FirstOrDefaultAsync();
    }

    public async Task<List<DocumentSection>> GetSectionsActivesAsync(Guid planEnteteId)
    {
        return await _context.DocumentSections
            .Include(s => s.Periodicite)
            .Include(s => s.TypeSection)
            .Where(s => s.EnteteId == planEnteteId)
            .ToListAsync();
    }

    public async Task<List<DocumentSection>> GetDocumentSectionsActivesAsync(Guid execControleOfId)
    {
        return await GetDocumentSectionsActivesParPosteAsync(execControleOfId, null!);
    }

    public async Task<List<DocumentSection>> GetDocumentSectionsActivesParPosteAsync(Guid execControleOfId, string posteCode)
    {
        // Seuls les plans d'assemblage des postes portent les contrôles de production de l'OF
        // (les documents de vérification machine ou de contrôle au poste ont leur propre saisie).
        var query = _context.ExecControleDocumentStatuts
            .Where(s => s.ExecControleOfId == execControleOfId && s.DocId.HasValue &&
                        s.TypeDocument == TypesDocumentExecution.PlanAssemblage);

        if (!string.IsNullOrEmpty(posteCode))
        {
            query = query.Where(s => s.PosteCode == posteCode);
        }

        var docIds = await query
            .Select(s => s.DocId!.Value)
            .Distinct()
            .ToListAsync();

        if (!docIds.Any()) return new List<DocumentSection>();

        // DocId référence le document exécuté (clé étrangère vers Document_Entete).
        var allEnteteIds = docIds;

        return await _context.DocumentSections
            .Include(s => s.Entete)
            .Include(s => s.TypeSection)
            .Include(s => s.Periodicite)
            .Include(s => s.DocumentLignes)
                .ThenInclude(l => l.TypeControle)
            .Include(s => s.DocumentLignes)
                .ThenInclude(l => l.MoyenControle)
            .Include(s => s.DocumentLignes)
                .ThenInclude(l => l.Caracteristique)
            .Where(s => allEnteteIds.Contains(s.EnteteId))
            .ToListAsync();
    }

    public async Task<List<DocumentLigne>> GetDocumentLignesForSectionAsync(Guid sectionId)
    {
        return await _context.DocumentLignes
            .Include(l => l.TypeControle)
            .Include(l => l.MoyenControle)
            .Include(l => l.Caracteristique)
            .Include(l => l.DocumentLigneExtraColonnes)
            .Include(l => l.DocumentLineImage)
            .Where(l => l.SectionId == sectionId)
            .OrderBy(l => l.OrdreAffiche)
            .ToListAsync();
    }

    public async Task<List<ExecPrelevementIntermediaire>> GetIntermediairesActifsAsync(Guid execControleOfId)
    {
        return await _context.ExecPrelevementIntermediaires
            .Where(p => p.ExecControleOfid == execControleOfId && !p.EstRepondu)
            .ToListAsync();
    }

    public async Task<List<ExecPrelevementIntermediaire>> GetIntermediairesParOfAsync(Guid execControleOfId)
    {
        return await _context.ExecPrelevementIntermediaires
            .Where(p => p.ExecControleOfid == execControleOfId)
            .ToListAsync();
    }

    public async Task<ExecPrelevementIntermediaire?> GetIntermediaireAsync(Guid id)
    {
        return await _context.ExecPrelevementIntermediaires.FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<List<ExecPrelevementIntermediaire>> GetIntermediairesParTrancheAsync(Guid execControleOfId, string trancheHoraire)
    {
        var cleanTranche = trancheHoraire.Split('|')[0];
        return await _context.ExecPrelevementIntermediaires
            .Where(o => o.ExecControleOfid == execControleOfId && (o.TrancheHoraire == trancheHoraire || o.TrancheHoraire.StartsWith(cleanTranche)))
            .ToListAsync();
    }

    public async Task<ExecControleTranche?> GetTrancheExistanteAsync(Guid execControleOfId, string trancheHoraire)
    {
        return await _context.ExecControleTranches
            .FirstOrDefaultAsync(t => t.ExecControleOfid == execControleOfId && t.TrancheHoraire == trancheHoraire);
    }

    public async Task<ExecControleTranche?> GetDerniereTrancheAsync(Guid execControleOfId)
    {
        return await _context.ExecControleTranches
            .Where(t => t.ExecControleOfid == execControleOfId)
            .OrderByDescending(t => t.HeureDebut)
            .FirstOrDefaultAsync();
    }

    public void AddIntermediaires(IEnumerable<ExecPrelevementIntermediaire> intermediaires)
    {
        _context.ExecPrelevementIntermediaires.AddRange(intermediaires);
    }

    public void AddTranche(ExecControleTranche tranche)
    {
        _context.ExecControleTranches.Add(tranche);
    }

    public void AddPieceType(ExecPieceType pieceType)
    {
        _context.ExecPieceTypes.Add(pieceType);
    }

    public void RemoveIntermediaires(IEnumerable<ExecPrelevementIntermediaire> intermediaires)
    {
        _context.ExecPrelevementIntermediaires.RemoveRange(intermediaires);
    }

    public async Task<List<DocumentLigne>> GetLignesForSectionAsync(Guid sectionId)
    {
        return await _context.DocumentLignes
            .Include(l => l.TypeControle)
            .Include(l => l.MoyenControle)
            .Include(l => l.DocumentLigneExtraColonnes)
            .Include(l => l.DocumentLineImage)
            .Where(l => l.SectionId == sectionId)
            .OrderBy(l => l.OrdreAffiche)
            .ToListAsync();
    }

    public void AddLigneReponses(IEnumerable<ExecControleLigneReponse> reponses)
    {
        _context.ExecControleLigneReponses.AddRange(reponses);
    }

    public async Task<string> GetContexteOccurrenceAsync(Guid sectionId)
    {
        // Plans de fabrication et d'assemblage partagent Document_Section.
        var section = await _context.DocumentSections
            .Include(s => s.TypeSection)
            .FirstOrDefaultAsync(s => s.Id == sectionId);

        return section?.TypeSection?.TypeNormalise?.ToUpper() ?? "";
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SopalTrace.Application.Interfaces.Execution;
using SopalTrace.Application.Services.Execution;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;
using SopalTrace.Infrastructure.Data;

namespace SopalTrace.Infrastructure.Repositories.Execution;

public class DemarragePosteRepository : IDemarragePosteRepository
{
    private readonly SopalTraceDbContext _context;

    public DemarragePosteRepository(SopalTraceDbContext context)
    {
        _context = context;
    }

    private IQueryable<ExecControleOfSession> SessionsOuvertes(Guid execControleOfId) => _context.ExecControleOfSessions
        .Where(s => s.ExecControleOfId == execControleOfId && (s.Statut == StatutsSession.EnCours || s.Statut == StatutsSession.EnPause));

    public async Task<List<DemarragePoste>> GetDemarragesAsync(Guid execControleOfId)
    {
        return await _context.ExecSessionPosts
            .AsNoTracking()
            .Where(d => d.ExecControleOfId == execControleOfId)
            .OrderBy(d => d.StartedAt)
            .Select(d => new DemarragePoste(d.SessionId, d.PosteCode, d.StartedAt, d.Session.Equipe, d.ClosingRequestedAt, d.EndedAt))
            .ToListAsync();
    }

    public async Task<bool> EstEnCoursAsync(Guid sessionId, string posteCode) =>
        await _context.ExecSessionPosts.AnyAsync(d => d.SessionId == sessionId && d.PosteCode == posteCode && d.EndedAt == null);

    public async Task<ExecSessionPost?> GetPosteSessionAsync(Guid sessionId, string posteCode) =>
        await _context.ExecSessionPosts.FirstOrDefaultAsync(d => d.SessionId == sessionId && d.PosteCode == posteCode);

    public async Task<List<ExecSessionPost>> GetPostesEnCoursAsync(Guid sessionId) =>
        await _context.ExecSessionPosts.Where(d => d.SessionId == sessionId && d.EndedAt == null).ToListAsync();

    public async Task<List<ExecControleOfSession>> GetSessionsOuvertesAsync(Guid execControleOfId) =>
        await SessionsOuvertes(execControleOfId).OrderBy(s => s.DateDebut).ToListAsync();

    public async Task<ExecControleOfSession?> GetSessionAsync(Guid execControleOfId, string? posteCode, string? equipe = null)
    {
        if (!string.IsNullOrWhiteSpace(equipe))
            return await SessionsOuvertes(execControleOfId).FirstOrDefaultAsync(s => s.Equipe == equipe.Trim());

        if (!string.IsNullOrWhiteSpace(posteCode))
        {
            // Une session terminée ne tient plus aucun poste (TR_ExecSessionPost_Consistency) : celle qui tient le
            // poste est ouverte ; une seule à la fois (UX_ExecSessionPost_OneTeamPerPoste).
            var tenant = await _context.ExecSessionPosts
                .Where(d => d.ExecControleOfId == execControleOfId && d.PosteCode == posteCode && d.EndedAt == null)
                .Select(d => d.Session)
                .FirstOrDefaultAsync();
            if (tenant != null) return tenant;
        }

        // Clôture demandée : l'équipe est seule sur l'OF (TR_ExecControleOfSession_Consistency).
        return await SessionsOuvertes(execControleOfId).FirstOrDefaultAsync(s => s.ClosingRequest == DemandesCloture.FinOf);
    }

    public void Ajouter(ExecSessionPost poste) => _context.ExecSessionPosts.Add(poste);

    public async Task SaveChangesAsync() => await _context.SaveChangesAsync();
}

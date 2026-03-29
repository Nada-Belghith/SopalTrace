using SopalTrace.Application.Interfaces;
using SopalTrace.Domain.Entities;
using SopalTrace.Infrastructure.Data;

namespace SopalTrace.Infrastructure.Repositories;

public class JournalConnexionRepository : IJournalConnexionRepository
{
    private readonly SopalTraceDbContext _context;

    public JournalConnexionRepository(SopalTraceDbContext context)
    {
        _context = context;
    }

    public async Task LogActionAsync(string matricule, string action, string details)
    {
        _context.JournalConnexions.Add(new JournalConnexion
        {
            Matricule = matricule,
            Action = action,
            Details = details,
            DateAction = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
    }
}
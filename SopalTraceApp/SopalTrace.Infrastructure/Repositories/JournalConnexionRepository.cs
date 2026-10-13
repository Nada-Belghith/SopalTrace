using System;
using System.Threading.Tasks;
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
            Id = Guid.NewGuid(),
            // Longueurs des colonnes : un matricule saisi à l'inscription n'est pas garanti valide.
            Matricule = Couper(matricule, 20),
            Action = action,
            Details = Couper(details, 255),
            DateAction = DateTime.Now
        });
        await _context.SaveChangesAsync();
    }

    private static string Couper(string? texte, int longueur)
        => string.IsNullOrEmpty(texte) || texte.Length <= longueur ? texte ?? string.Empty : texte[..longueur];
}

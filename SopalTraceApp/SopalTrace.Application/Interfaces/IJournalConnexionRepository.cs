namespace SopalTrace.Application.Interfaces;

public interface IJournalConnexionRepository
{
    Task LogActionAsync(string matricule, string action, string details);
}
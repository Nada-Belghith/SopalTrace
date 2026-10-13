namespace SopalTrace.Application.Interfaces;

public interface ICurrentUserService
{
    /// <summary>Matricule de l'utilisateur authentifié, ou null hors requête authentifiée.</summary>
    string? Matricule { get; }
    string? NomComplet { get; }

    /// <summary>Matricule de l'utilisateur authentifié ; lève une exception s'il est absent.</summary>
    string MatriculeRequis { get; }
}

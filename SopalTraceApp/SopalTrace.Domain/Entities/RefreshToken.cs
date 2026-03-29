namespace SopalTrace.Domain.Entities;

public class RefreshToken
{
    public Guid Id { get; set; } // En DB First avec newid(), c'est un Guid
    public string Token { get; set; } = string.Empty;
    public string JwtId { get; set; } = string.Empty; // <-- LA PROPRIÉTÉ MANQUANTE EST LÀ !
    public DateTime DateExpiration { get; set; }
    public bool EstRevoque { get; set; } // <-- Avec un 'q' comme dans la DB
    public DateTime DateCreation { get; set; }

    public Guid UtilisateurId { get; set; }
    public virtual UtilisateursApp Utilisateur { get; set; } = null!;

    // Règle métier pure
    public bool EstActif => !EstRevoque && DateExpiration > DateTime.UtcNow;
}
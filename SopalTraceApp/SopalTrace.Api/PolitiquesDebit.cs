namespace SopalTrace.Api;

/// <summary>Noms des politiques de limitation de débit.</summary>
public static class PolitiquesDebit
{
    /// <summary>Connexion, inscription, mot de passe : 10 requêtes par minute et par adresse IP (anti force brute).</summary>
    public const string Authentification = "authentification";
}

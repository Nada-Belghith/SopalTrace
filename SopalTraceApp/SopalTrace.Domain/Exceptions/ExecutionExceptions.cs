namespace SopalTrace.Domain.Exceptions;

// =================================================================
// EXCEPTIONS DU MODULE "EXÉCUTION OPÉRATEUR"
// Le message est destiné à l'opérateur : il ne doit contenir aucun
// détail technique (stack, SQL, nom de table...).
// =================================================================

/// <summary>Ressource demandée inexistante (HTTP 404).</summary>
public class RessourceIntrouvableException : DomainException
{
    public RessourceIntrouvableException(string message) : base(message) { }
}

/// <summary>Règle métier violée par la demande de l'opérateur (HTTP 400).</summary>
public class RegleMetierException : DomainException
{
    public RegleMetierException(string message) : base(message) { }
}

/// <summary>L'état actuel de l'exécution empêche l'action demandée (HTTP 409).</summary>
public class EtatExecutionInvalideException : DomainException
{
    public EtatExecutionInvalideException(string message) : base(message) { }
}

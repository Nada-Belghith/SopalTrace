using System;

namespace SopalTrace.Domain.Exceptions;

// =================================================================
// EXCEPTIONS SPÉCIFIQUES AU MODULE "PLANS DE FABRICATION / QUALITÉ"
// Toutes ces classes héritent de DomainException pour être attrapées 
// automatiquement par votre ExceptionMiddleware.
// =================================================================

public class ModeleIntrouvableException : DomainException
{
    public ModeleIntrouvableException()
        : base("Le modèle source est introuvable ou inactif.") { }

    public ModeleIntrouvableException(Guid id)
        : base($"Le modèle avec l'identifiant {id} est introuvable.") { }
}

public class ModeleGeneriqueException : DomainException
{
    public ModeleGeneriqueException(Guid id)
        : base($"Impossible d'instancier un plan de fabrication : le modèle source {id} est lié à une nature de composant générique.") { }
}

public class PlanIntrouvableException : DomainException
{
    public PlanIntrouvableException(Guid id)
        : base($"Le plan de contrôle avec l'identifiant {id} est introuvable.") { }
}

public class DoublonModeleException : DomainException
{
    public DoublonModeleException()
        : base("Un modèle ACTIF existe déjà pour cette combinaison (Type/Nature/Opération).") { }
}

public class DoublonPlanException : DomainException
{
    public DoublonPlanException(string codeArticleSage)
        : base($"Un plan de contrôle actif existe déjà pour l'article SAGE '{codeArticleSage}'.") { }
}

public class PlanArchiveException : DomainException
{
    public PlanArchiveException()
        : base("Action impossible : Ce plan est déjà archivé.") { }
}

public class PlanSourceNonActifException : DomainException
{
    public PlanSourceNonActifException(Guid id)
        : base($"Action impossible : le plan source {id} doit être au statut ACTIF.") { }
}

public class ArticleSageIntrouvableException : DomainException
{
    public ArticleSageIntrouvableException(string codeArticle)
        : base($"L'article SAGE '{codeArticle}' n'existe pas dans le référentiel ERP.") { }
}

public class ConflitConcurrenceException : DomainException
{
    public ConflitConcurrenceException(string details)
        : base($"Conflit de concurrence détecté. {details} Merci de recharger puis réessayer.") { }
}
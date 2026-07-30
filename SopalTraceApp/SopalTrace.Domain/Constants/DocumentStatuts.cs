namespace SopalTrace.Domain.Constants;

/// <summary>
/// Constantes des statuts possibles d'un DocumentEntete.
/// Seuls ACTIF et ARCHIVE sont valides pour les types de documents centralisés.
/// Le statut BROUILLON est réservé aux plans de fabrication (PlanFabricationService).
/// </summary>
public static class DocumentStatuts
{
    /// <summary>Document en production — visible des opérateurs.</summary>
    public const string Actif = "ACTIF";

    /// <summary>Document archivé — conservé pour historique uniquement.</summary>
    public const string Archive = "ARCHIVE";
}

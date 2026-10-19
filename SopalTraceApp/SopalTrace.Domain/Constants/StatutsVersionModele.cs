namespace SopalTrace.Domain.Constants;

/// <summary>
/// Statuts d'une version de modèle de rapport (ReportTemplateVersion.Status) : envoyée en brouillon (vérifiée, aperçu
/// possible), puis publiée par sa première affectation. Une version publiée ne change plus.
/// </summary>
public static class StatutsVersionModele
{
    public const string Brouillon = "DRAFT";
    public const string Publiee = "PUBLISHED";
}

/// <summary>Nature d'une affectation de modèle (ReportTemplateAssignment.Kind).</summary>
public static class TypesAffectationModele
{
    /// <summary>Modèle par défaut de son type : documents d'exécution, documents dont le formulaire n'a pas de modèle propre.</summary>
    public const string ParDefaut = "DEFAULT";
    /// <summary>Modèle conçu pour une version de formulaire (nouvelle structure) : vaut aussi pour ses documents déjà créés.</summary>
    public const string StructureFormulaire = "FORM_STRUCTURE";
    /// <summary>Révision (logo…) d'un modèle pour un formulaire : seulement pour les documents créés après.</summary>
    public const string RevisionFormulaire = "FORM_REVISION";
}

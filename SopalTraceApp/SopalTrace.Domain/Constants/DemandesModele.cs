namespace SopalTrace.Domain.Constants;

/// <summary>Demandes de modèle (table ReportTemplateRequest) : origine, choix de version du superviseur, état.</summary>
public static class DemandesModele
{
    /// <summary>Nouveau code de formulaire : son premier modèle est à rattacher.</summary>
    public const string NouveauFormulaire = "NEW_FORM";
    /// <summary>Nouvelle version dont les colonnes changent : un nouveau modèle est obligatoire.</summary>
    public const string NouvelleStructure = "NEW_STRUCTURE";
    /// <summary>Demande du superviseur qualité (logo, mise en page…), avec son choix de version.</summary>
    public const string Superviseur = "SUPERVISOR";

    /// <summary>Même version : le nouveau modèle vaut pour les documents créés après ; les autres gardent l'ancien.</summary>
    public const string MemeVersion = "SAME_VERSION";
    /// <summary>Version suivante : le formulaire passe à sa version suivante avec ce modèle.</summary>
    public const string VersionSuivante = "NEXT_VERSION";

    public const string EnAttente = "PENDING";
    public const string Traitee = "DONE";
    public const string Annulee = "CANCELLED";
}

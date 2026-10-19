namespace SopalTrace.Domain.Constants;

/// <summary>
/// Constantes des codes des types de documents gérés par DocumentService.
/// Ces 4 types partagent la même logique de création, de versioning et de correction mineure.
/// </summary>
public static class TypesDocument
{
    public const string ControlePoste   = "CTRL_POSTE";
    public const string ResultatCF      = "RESULTAT_CF";
    public const string PlanAssemblage  = "PLAN_ASS";
    public const string PlanProduitFini = "PLAN_PF";

    /// <summary>Modèle de fabrication : document générique (nature + opération + famille), source des plans.</summary>
    public const string ModeleFabrication = "MODELE_FAB";
    /// <summary>Plan de fabrication : document spécifique à un code article Sage versionné.</summary>
    public const string PlanFabrication   = "PLAN_FAB";
    /// <summary>Plan de vérification machine : MachineCode obligatoire, lignes sans section (TypeLigne).</summary>
    public const string VerifMachine      = "VERIF_MACHINE";
    /// <summary>Plan d'échantillonnage ISO 2859-1 : un seul ACTIF, paramètres dans SamplingPlan.</summary>
    public const string PlanEchantillonnage = "PLAN_ECHANTILLONNAGE";

    /// <summary>
    /// Ensemble des types gérés de manière centralisée par DocumentService.
    /// Tous créent directement en ACTIF V1, sans BROUILLON.
    /// </summary>
    public static readonly IReadOnlySet<string> TypesCentralises = new HashSet<string>
    {
        ControlePoste,
        ResultatCF,
        PlanAssemblage,
        PlanProduitFini
    };
}

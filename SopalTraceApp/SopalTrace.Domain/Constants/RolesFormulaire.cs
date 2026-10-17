namespace SopalTrace.Domain.Constants;

/// <summary>
/// Rôles de formulaire utilisés par le code (Ref_Formulaire.Role → table FormRole, qui porte la liste complète, les
/// libellés, l'ordre et les critères permis).
/// </summary>
public static class RolesFormulaire
{
    public const string EnCoursDeFabrication = "EN_COURS_DE_FABRICATION";
    public const string EnCoursDeAssemblage = "EN_COURS_DE_ASSEMBLAGE";
    public const string ProduitFini = "PRODUIT_FINI";
    public const string ResultatControleCf = "RESULTAT_CONTROLE_CF";
    /// <summary>Résultats du contrôle en cours d'assemblage remplis par les opérateurs (FE-RC-ENCF-ASS-*).</summary>
    public const string ResultatControleAssemblage = "RESULTAT_CONTROLE_ASSEMBLAGE";
    public const string ResultatControlePoste = "RESULTAT_CONTROLE_POSTE";
    public const string VerifMachine = "VERIF_MACHINE";
    public const string Echantillonnage = "ECHANTILLONNAGE";
    /// <summary>Registre de traçabilité (FE0129) : fiche de réponse remplie par les opérateurs.</summary>
    public const string RegistreTracabilite = "REGISTRE_TRACABILITE";

    /// <summary>Rôle donné au formulaire créé pour un type de document.</summary>
    public static string PourTypeDocument(string typeDocumentCode) => typeDocumentCode switch
    {
        TypesDocument.ControlePoste => ResultatControlePoste,
        TypesDocument.ResultatCF => ResultatControleCf,
        TypesDocument.PlanAssemblage => EnCoursDeAssemblage,
        TypesDocument.PlanProduitFini => ProduitFini,
        _ => throw new System.ArgumentOutOfRangeException(nameof(typeDocumentCode), typeDocumentCode, "Type de document sans formulaire associé.")
    };
}

/// <summary>Codes métier de l'annuaire Sage (AUTILIS.CODMET_0) utilisés pour les destinataires d'alertes.</summary>
public static class CodesMetierErp
{
    public const string ManagerProduction = "MANAGER_PROD";
    public const string SuperviseurProduction = "SUPERVISEUR_PROD";
}

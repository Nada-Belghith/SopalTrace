namespace SopalTrace.Domain.Constants;

/// <summary>Rôle d'un formulaire de référence (Ref_Formulaire.Role, contrainte CHECK).</summary>
public static class RolesFormulaire
{
    public const string EnCoursDeFabrication = "EN_COURS_DE_FABRICATION";
    public const string EnCoursDeAssemblage = "EN_COURS_DE_ASSEMBLAGE";
    public const string ProduitFini = "PRODUIT_FINI";
    public const string ResultatControleCf = "RESULTAT_CONTROLE_CF";
    public const string ResultatControlePoste = "RESULTAT_CONTROLE_POSTE";
    public const string VerifMachine = "VERIF_MACHINE";
    public const string Echantillonnage = "ECHANTILLONNAGE";

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

namespace SopalTrace.Domain.Constants;

public static class RolesApp
{
    public const string Admin = "ADMIN";
    public const string ResponsableDI = "RESPONSABLE_DI";
    public const string ResponsableQualite = "RESPONSABLE_QUALITE";
    public const string Operateur = "OPERATEUR";
    public const string Magasinier = "MAGASINIER";
    public const string SuperviseurQualite = "SUPERVISEUR_QUALITE";

    // Services qui déposent les documents externes (achats, commercial, qualité, développement industriel), chacun ses
    // types de documents (table ExternalDocumentType).
    public const string ServiceAchats = "SERVICE_ACHATS";
    public const string ServiceCommercial = "SERVICE_COMMERCIAL";
    public const string ServiceQualite = "SERVICE_QUALITE";
    public const string ServiceDI = "SERVICE_DI";

    public static readonly string[] RolesDepotDocumentsExternes = { ServiceAchats, ServiceCommercial, ServiceQualite, ServiceDI };

    public static readonly string[] TousLesRolesAutorises =
    {
        Admin,
        ResponsableDI,
        ResponsableQualite,
        Operateur,
        Magasinier,
        SuperviseurQualite,
        ServiceAchats,
        ServiceCommercial,
        ServiceQualite,
        ServiceDI
    };
}

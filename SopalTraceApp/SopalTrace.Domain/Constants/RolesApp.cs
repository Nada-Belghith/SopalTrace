namespace SopalTrace.Domain.Constants;

public static class RolesApp
{
    public const string Admin = "ADMIN";
    public const string ResponsableDI = "RESPONSABLE_DI";
    public const string ResponsableQualite = "RESPONSABLE_QUALITE";
    public const string Operateur = "OPERATEUR";
    public const string Magasinier = "MAGASINIER";
    public const string SuperviseurQualite = "SUPERVISEUR_QUALITE";

    public static readonly string[] TousLesRolesAutorises = 
    { 
        Admin, 
        ResponsableDI, 
        ResponsableQualite, 
        Operateur, 
        Magasinier,
        SuperviseurQualite
    };
}

namespace SopalTrace.Domain.Constants;

public static class RolesApp
{
    public const string Admin = "ADMIN";
    public const string Qualite = "QUAL"; // Correspond au code ERP
    public const string Magasin = "MAG"; // Correspond au code ERP
    public const string Direction = "DI"; // Correspond au code ERP
    public const string ProdGaz = "PROD_GAZ"; // Correspond au code ERP
    public const string Responsable = "RESPONSABLE";
    
    public static readonly string[] TousLesRolesAutorises = 
    { 
        Admin, 
        Qualite, 
        Magasin, 
        Direction, 
        ProdGaz, 
        Responsable,
        "QUALITE" // Fallback pour compatibilité
    };
}

namespace SopalTrace.Domain.Constants;

/// <summary>Types de documents externes (table ExternalDocumentType), déposés par les services et retrouvés pour chaque OF.</summary>
public static class TypesDocumentExterne
{
    /// <summary>Rapport de contrôle d'un lot reçu : NumRapport-NumCdeFrs (le n° de rapport est sur l'étiquette scannée).</summary>
    public const string RapportControle = "CONTROL_REPORT";
    /// <summary>Certificat de conformité d'un article d'une commande fournisseur : CodeArticle-NumCdeFrs.</summary>
    public const string CertificatConformite = "CONFORMITY_CERTIFICATE";
    /// <summary>Bon de commande fournisseur : NumCdeFrs.</summary>
    public const string CommandeFournisseur = "SUPPLIER_ORDER";
    /// <summary>Bon de livraison : NumBL-RefCdeClient (référence client de la commande Sage du BL).</summary>
    public const string BonLivraison = "DELIVERY_NOTE";
    /// <summary>Bon de commande client : RefCdeClient (référence de commande du client, portée par la commande Sage).</summary>
    public const string CommandeClient = "CUSTOMER_ORDER";
    /// <summary>Rapport de contrôle rapide du produit fini : NumOF.</summary>
    public const string RapportProduitFini = "FINISHED_PRODUCT_CONTROL_REPORT";
    /// <summary>Plan de définition du produit fini (dessin d'ensemble technique livré par le service DI) : CodeArticle.</summary>
    public const string PlanDefinitionProduitFini = "FINISHED_PRODUCT_DEFINITION_PLAN";
}

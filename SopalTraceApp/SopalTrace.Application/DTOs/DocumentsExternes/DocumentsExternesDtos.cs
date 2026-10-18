using System;
using System.Collections.Generic;

namespace SopalTrace.Application.DTOs.DocumentsExternes;

/// <summary>
/// Numéros qui identifient un document externe, dans l'ordre où ils forment son nom normalisé
/// (ex. rapport de contrôle : NumeroRapport-NumeroCommandeFournisseur.pdf).
/// </summary>
public static class ChampsDocumentExterne
{
    public const string NumeroRapport = "numeroRapport";
    public const string CodeArticle = "codeArticle";
    public const string NumeroBl = "numeroBl";
    public const string NumeroOf = "numeroOf";
    public const string NumeroCommandeFournisseur = "numeroCommandeFournisseur";
    /// <summary>Commande client Sage (commande ouverte) livrée par un BL ; elle ne fait pas partie du nom.</summary>
    public const string NumeroCommande = "numeroCommande";
    public const string ReferenceCommandeClient = "referenceCommandeClient";
}

public class ChampDocumentExterneDto
{
    public string Code { get; set; } = "";
    public string Libelle { get; set; } = "";
    public int LongueurMaximale { get; set; }
    /// <summary>CODE : majuscules, chiffres, « . », « _ », « - » ; REFERENCE : texte libre tel que dans Sage.</summary>
    public string Format { get; set; } = FormatsChamp.Code;
    /// <summary>Le numéro forme le nom du fichier (la commande Sage d'un BL sert seulement à retrouver sa référence client).</summary>
    public bool DansLeNom { get; set; } = true;
}

public static class FormatsChamp
{
    public const string Code = "CODE";
    public const string Reference = "REFERENCE";
}

/// <summary>Type de document que l'utilisateur peut déposer, avec les numéros à saisir.</summary>
public class TypeDocumentExterneDto
{
    public string Code { get; set; } = "";
    public string Libelle { get; set; } = "";
    public List<ChampDocumentExterneDto> Champs { get; set; } = new();
    /// <summary>La référence client qui termine le nom est reprise de la commande Sage saisie (BL).</summary>
    public bool ReferenceClientDeLaCommande { get; set; }
}

/// <summary>Dépôt d'un document : type, numéros, fichier PDF ; remplacement d'un document en vigueur avec un motif.</summary>
public class DepotDocumentExterneRequest
{
    public string TypeCode { get; set; } = "";
    public string? NumeroRapport { get; set; }
    public string? CodeArticle { get; set; }
    public string? NumeroBl { get; set; }
    public string? NumeroOf { get; set; }
    public string? NumeroCommandeFournisseur { get; set; }
    public string? NumeroCommande { get; set; }
    public string? ReferenceCommandeClient { get; set; }
    public bool Remplacer { get; set; }
    public string? MotifRemplacement { get; set; }
    public string NomFichierOrigine { get; set; } = "";
    public byte[] Contenu { get; set; } = Array.Empty<byte>();
}

/// <summary>Document externe déposé (sans le contenu du fichier).</summary>
public class DocumentExterneDto
{
    public Guid Id { get; set; }
    public string TypeCode { get; set; } = "";
    public string TypeLibelle { get; set; } = "";
    /// <summary>Nom normalisé formé par la base.</summary>
    public string NomFichier { get; set; } = "";
    public string? NumeroRapport { get; set; }
    public string? CodeArticle { get; set; }
    public string? NumeroBl { get; set; }
    public string? NumeroOf { get; set; }
    public string? NumeroCommandeFournisseur { get; set; }
    public string? NumeroCommande { get; set; }
    public string? ReferenceCommandeClient { get; set; }
    public string NomFichierOrigine { get; set; } = "";
    public string? EmpreinteSha256 { get; set; }
    public string DeposePar { get; set; } = "";
    public string? DeposeParNom { get; set; }
    public DateTime DeposeLe { get; set; }
    /// <summary>Document remplacé par un dépôt plus récent (il reste consultable).</summary>
    public DateTime? RemplaceLe { get; set; }
    public Guid? RemplaceDocumentId { get; set; }
    public string? MotifRemplacement { get; set; }
}

/// <summary>Document externe attendu pour un OF : disponible (déposé) ou manquant, avec la raison.</summary>
public class CarteDocumentExterneDto
{
    public string TypeCode { get; set; } = "";
    public string TypeLibelle { get; set; } = "";
    /// <summary>Nom du fichier déposé, ou nom attendu quand ses numéros sont connus.</summary>
    public string? NomFichier { get; set; }
    public string Etat { get; set; } = "";
    public string? RaisonManquant { get; set; }
    public Guid? DocumentId { get; set; }
    public DateTime? DeposeLe { get; set; }
    public string? DeposeParNom { get; set; }
}

/// <summary>Lot servi à l'OF (étiquette scannée par le magasinier) et ses documents fournisseur.</summary>
public class LotFournisseurDto
{
    public string CodeArticle { get; set; } = "";
    public string? DesignationArticle { get; set; }
    public string NumeroLot { get; set; } = "";
    public string? NumeroRapport { get; set; }
    /// <summary>Commande fournisseur lue dans le rapport de contrôle déposé.</summary>
    public string? NumeroCommandeFournisseur { get; set; }
    public List<CarteDocumentExterneDto> Documents { get; set; } = new();
}

/// <summary>BL sur lequel l'OF a été expédié (scan du magasinier) et ses documents client.</summary>
public class LivraisonClientDto
{
    public string NumeroBl { get; set; } = "";
    public string CodeClient { get; set; } = "";
    public DateOnly DateExpedition { get; set; }
    /// <summary>Commande Sage livrée par le BL (commande ouverte) et la référence de commande du client qu'elle porte.</summary>
    public string? NumeroCommande { get; set; }
    public string? ReferenceCommandeClient { get; set; }
    public List<CarteDocumentExterneDto> Documents { get; set; } = new();
}

/// <summary>Documents externes d'un OF : fournisseurs (par lot servi), clients (par BL), produit fini (plan de définition, rapport).</summary>
public class DossierExterneOfDto
{
    public List<LotFournisseurDto> Fournisseurs { get; set; } = new();
    public List<LivraisonClientDto> Clients { get; set; } = new();
    public List<CarteDocumentExterneDto> ProduitFini { get; set; } = new();
}

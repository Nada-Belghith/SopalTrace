using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SopalTrace.Application.DTOs.DocumentsExternes;
using SopalTrace.Domain.Entities;

namespace SopalTrace.Application.Interfaces;

/// <summary>Lot servi à un OF : étiquette scannée par le magasinier (article, lot, n° du rapport de contrôle).</summary>
public sealed record LotServi(string CodeArticle, string? DesignationArticle, string NumeroLot, string? NumeroRapport);

/// <summary>
/// BL sur lequel un OF a été expédié : la commande Sage qu'il livre (commande ouverte) et la référence de commande du
/// client portée par cette commande.
/// </summary>
public sealed record BlExpedie(string NumeroBl, string CodeClient, DateOnly DateExpedition, string? NumeroCommande, string? ReferenceCommandeClient);

/// <summary>Commande client Sage (commande ouverte) et la référence de commande du client qu'elle porte (null si absente).</summary>
public sealed record CommandeClientSage(string NumeroCommande, string? ReferenceCommandeClient);

/// <summary>BL connu de Sage, sa commande Sage et la référence client de cette commande (null si absentes).</summary>
public sealed record BlSage(string NumeroBl, string? NumeroCommande, string? ReferenceCommandeClient);

/// <summary>Document externe en vigueur (sans son fichier), pour retrouver les documents d'un OF.</summary>
public sealed record DocumentExterneEnVigueur(Guid Id, string TypeCode, string NomFichier, string? NumeroRapport, string? CodeArticle,
    string? NumeroBl, string? NumeroOf, string? NumeroCommandeFournisseur, string? ReferenceCommandeClient, DateTime DeposeLe, string? DeposeParNom);

/// <summary>Fichier d'un document externe et son type (contrôle d'accès par service).</summary>
public sealed record FichierDocumentExterne(string TypeCode, string NomFichier, byte[] Contenu);

public interface IDocumentExterneRepository
{
    Task<List<ExternalDocumentType>> GetTypesAsync();

    Task<bool> ArticleExisteAsync(string codeArticle);
    Task<bool> OfExisteAsync(string numeroOf);
    Task<bool> CommandeFournisseurExisteAsync(string numeroCommande);
    /// <summary>Référence de commande client telle qu'écrite dans Sage (comparaison sans casse), ou null si inconnue.</summary>
    Task<string?> GetReferenceCommandeClientAsync(string reference);
    Task<CommandeClientSage?> GetCommandeClientAsync(string numeroCommande);
    Task<BlSage?> GetBlAsync(string numeroBl);

    /// <summary>Document en vigueur de ce type et de cet identifiant (ExternalDocument.DocumentKey), sans son fichier.</summary>
    Task<DocumentExterneDto?> GetEnVigueurAsync(string typeCode, string cle);

    /// <summary>Enregistre le document ; s'il en remplace un, l'ancien est marqué remplacé dans la même transaction.</summary>
    Task AjouterAsync(ExternalDocument document);

    Task<List<DocumentExterneDto>> ListerAsync(IReadOnlyCollection<string> types, string? recherche, int nombreMaximal);
    Task<DocumentExterneDto?> GetAsync(Guid id);
    Task<FichierDocumentExterne?> GetFichierAsync(Guid id);

    // ── Documents d'un OF ──
    Task<List<LotServi>> GetLotsServisAsync(string numeroOf);
    Task<List<BlExpedie>> GetBlsExpediesAsync(string numeroOf);

    /// <summary>Documents en vigueur qui portent l'un de ces numéros.</summary>
    Task<List<DocumentExterneEnVigueur>> GetEnVigueurParNumerosAsync(NumerosRecherches numeros);

    /// <summary>Article de l'OF s'il fabrique un produit fini (plan de définition, rapport de contrôle rapide), sinon null.</summary>
    Task<string?> GetArticleProduitFiniAsync(string numeroOf);
}

/// <summary>Numéros cherchés parmi les documents en vigueur (une liste vide ne cherche rien).</summary>
public sealed record NumerosRecherches
{
    public IReadOnlyCollection<string> Rapports { get; init; } = Array.Empty<string>();
    public IReadOnlyCollection<string> Articles { get; init; } = Array.Empty<string>();
    public IReadOnlyCollection<string> CommandesFournisseur { get; init; } = Array.Empty<string>();
    public IReadOnlyCollection<string> Bls { get; init; } = Array.Empty<string>();
    public IReadOnlyCollection<string> ReferencesClient { get; init; } = Array.Empty<string>();
    public string? NumeroOf { get; init; }
}

/// <summary>Copie des documents déposés dans le répertoire partagé des services (facultatif : la base reste la référence).</summary>
public interface IRepertoirePartage
{
    Task CopierAsync(string sousDossier, string nomFichier, byte[] contenu);
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SopalTrace.Application.DTOs.DocumentsExternes;
using SopalTrace.Application.DTOs.Rapports;
using SopalTrace.Application.Interfaces;
using SopalTrace.Application.Settings;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;
using SopalTrace.Domain.Exceptions;

namespace SopalTrace.Application.Services;

public interface IDocumentExterneService
{
    /// <summary>Types de documents que ce rôle dépose.</summary>
    Task<List<TypeDocumentExterneDto>> GetTypesAsync(string role);
    Task<DocumentExterneDto> DeposerAsync(DepotDocumentExterneRequest demande, string role);
    Task<List<DocumentExterneDto>> ListerAsync(string role, string? typeCode, string? recherche);
    Task<FichierDto> GetFichierAsync(Guid id, string role);
    /// <summary>Documents externes d'un OF, retrouvés en cascade à partir des scans du magasinier.</summary>
    Task<DossierExterneOfDto> GetDossierOfAsync(string numeroOf);
    /// <summary>Commande client Sage et sa référence client (aperçu du nom d'un BL au dépôt).</summary>
    Task<CommandeClientSage> GetCommandeClientAsync(string numeroCommande);
}

/// <summary>
/// Documents externes : chaque service dépose ses documents en saisissant les numéros exigés par leur type ; la base
/// forme le nom normalisé (NumRapport-NumCdeFrs, CodeArticle-NumCdeFrs, NumCdeFrs, NumBL-RefCdeClient, RefCdeClient,
/// NumOF, CodeArticle). Pour un BL, le commercial saisit le n° du BL et celui de la commande Sage (commande ouverte) :
/// la référence client est reprise de la commande, sans recherche de sa part. Ces numéros permettent ensuite de retrouver les documents de chaque OF : étiquettes des lots
/// servis (n° de rapport) → rapport → commande fournisseur → bon de commande et certificats ; BL de l'expédition →
/// commande Sage (ouverte) → référence de commande du client → bon de commande client ; OF de produit fini → plan de
/// définition de son article et rapport de contrôle rapide.
/// </summary>
public class DocumentExterneService : IDocumentExterneService
{
    public const string Disponible = EtatsTracabilite.Disponible;
    public const string Manquant = EtatsTracabilite.Manquant;
    private const int NombreMaximalListe = 200;
    private static readonly Regex CodeValide = new("^[A-Z0-9._-]+$", RegexOptions.Compiled);
    // Caractères interdits dans un nom de fichier ; espaces et « / » d'une référence client deviennent « _ » dans le nom.
    private static readonly Regex ReferenceInvalide = new(@"[\\:*?""<>|\p{Cc}]", RegexOptions.Compiled);

    private readonly IDocumentExterneRepository _depot;
    private readonly IRepertoirePartage _repertoire;
    private readonly ICurrentUserService _utilisateur;
    private readonly DocumentsExternesSettings _parametres;
    private readonly ILogger<DocumentExterneService> _journal;

    public DocumentExterneService(IDocumentExterneRepository depot, IRepertoirePartage repertoire, ICurrentUserService utilisateur,
        DocumentsExternesSettings parametres, ILogger<DocumentExterneService> journal)
    {
        _depot = depot;
        _repertoire = repertoire;
        _utilisateur = utilisateur;
        _parametres = parametres;
        _journal = journal;
    }

    // ── Types et nommage ─────────────────────────────────────────────────

    /// <summary>Numéros exigés par un type, dans l'ordre où ils forment le nom (même ordre que la base).</summary>
    public static List<ChampDocumentExterneDto> Champs(ExternalDocumentType type)
    {
        var champs = new List<ChampDocumentExterneDto>();
        void Ajouter(bool exige, string code, string libelle, int longueur, string format = FormatsChamp.Code, bool dansLeNom = true)
        {
            if (exige) champs.Add(new ChampDocumentExterneDto { Code = code, Libelle = libelle, LongueurMaximale = longueur, Format = format, DansLeNom = dansLeNom });
        }
        Ajouter(type.RequiresControlReportNumber, ChampsDocumentExterne.NumeroRapport, "N° du rapport de contrôle", 100);
        Ajouter(type.RequiresArticleCode, ChampsDocumentExterne.CodeArticle, "Code article", 30);
        Ajouter(type.RequiresDeliveryNoteNumber, ChampsDocumentExterne.NumeroBl, "N° du bon de livraison", 30);
        Ajouter(type.RequiresManufacturingOrderNumber, ChampsDocumentExterne.NumeroOf, "N° de l'OF", 30);
        Ajouter(type.RequiresSupplierOrderNumber, ChampsDocumentExterne.NumeroCommandeFournisseur, "N° de la commande fournisseur", 30);
        Ajouter(type.RequiresSalesOrderNumber, ChampsDocumentExterne.NumeroCommande, "N° de la commande client dans Sage (commande ouverte)", 30, dansLeNom: false);
        // Avec une commande Sage, la référence client est reprise de cette commande : elle n'est pas saisie.
        Ajouter(type.RequiresCustomerOrderReference && !type.RequiresSalesOrderNumber, ChampsDocumentExterne.ReferenceCommandeClient,
            "Référence de la commande client", 50, FormatsChamp.Reference);
        return champs;
    }

    /// <summary>Nom normalisé (sans extension) : les numéros dans l'ordre fixe, séparés par « - » (comme la base).</summary>
    public static string Nom(string? rapport, string? article, string? bl, string? of, string? commandeFournisseur, string? referenceClient) =>
        string.Join("-", new[] { rapport, article, bl, of, commandeFournisseur, referenceClient?.Replace(' ', '_').Replace('/', '_') }
            .Where(n => !string.IsNullOrEmpty(n)));

    /// <summary>Identifiant d'un document dans son type (comme ExternalDocument.DocumentKey) : rapport, BL, sinon le nom.</summary>
    public static string Cle(string typeCode, string? rapport, string? article, string? bl, string? of, string? commandeFournisseur, string? referenceClient) =>
        typeCode switch
        {
            TypesDocumentExterne.RapportControle => rapport ?? "",
            TypesDocumentExterne.BonLivraison => bl ?? "",
            _ => Nom(rapport, article, bl, of, commandeFournisseur, referenceClient)
        };

    /// <summary>Numéro saisi : espaces retirés, en majuscules ; null s'il est vide.</summary>
    public static string? Normaliser(string? numero) =>
        string.IsNullOrWhiteSpace(numero) ? null : numero.Trim().ToUpperInvariant();

    public async Task<List<TypeDocumentExterneDto>> GetTypesAsync(string role) =>
        (await _depot.GetTypesAsync())
            .Where(t => t.UploadRole == role)
            .OrderBy(t => t.DisplayOrder)
            .Select(t => new TypeDocumentExterneDto
            {
                Code = t.Code, Libelle = t.Label, Champs = Champs(t),
                ReferenceClientDeLaCommande = t.RequiresSalesOrderNumber && t.RequiresCustomerOrderReference
            })
            .ToList();

    // ── Dépôt ────────────────────────────────────────────────────────────

    public async Task<DocumentExterneDto> DeposerAsync(DepotDocumentExterneRequest demande, string role)
    {
        var type = (await _depot.GetTypesAsync()).FirstOrDefault(t => t.Code == demande.TypeCode)
            ?? throw new RegleMetierException("Type de document inconnu.");
        if (type.UploadRole != role)
            throw new RegleMetierException($"Les documents « {type.Label} » sont déposés par un autre service.");

        var valeurs = new Dictionary<string, string?>
        {
            [ChampsDocumentExterne.NumeroRapport] = Normaliser(demande.NumeroRapport),
            [ChampsDocumentExterne.CodeArticle] = Normaliser(demande.CodeArticle),
            [ChampsDocumentExterne.NumeroBl] = Normaliser(demande.NumeroBl),
            [ChampsDocumentExterne.NumeroOf] = Normaliser(demande.NumeroOf),
            [ChampsDocumentExterne.NumeroCommandeFournisseur] = Normaliser(demande.NumeroCommandeFournisseur),
            [ChampsDocumentExterne.NumeroCommande] = Normaliser(demande.NumeroCommande),
            // La référence client garde son écriture (elle est remplacée par celle de Sage à la vérification).
            [ChampsDocumentExterne.ReferenceCommandeClient] = string.IsNullOrWhiteSpace(demande.ReferenceCommandeClient) ? null : demande.ReferenceCommandeClient.Trim()
        };
        var champs = Champs(type);
        foreach (var champ in champs)
        {
            var valeur = valeurs[champ.Code];
            if (valeur == null)
                throw new RegleMetierException($"Saisissez le champ « {champ.Libelle} ».");
            if (valeur.Length > champ.LongueurMaximale)
                throw new RegleMetierException($"« {champ.Libelle} » : {champ.LongueurMaximale} caractères au plus.");
            if (champ.Format == FormatsChamp.Code && !CodeValide.IsMatch(valeur))
                throw new RegleMetierException($"« {champ.Libelle} » : lettres, chiffres, « . », « _ » et « - » seulement.");
            if (champ.Format == FormatsChamp.Reference && ReferenceInvalide.IsMatch(valeur))
                throw new RegleMetierException($"« {champ.Libelle} » : les caractères \\ : * ? \" < > | sont interdits.");
        }
        // Les numéros que le type ne fait pas saisir sont ignorés (la référence client d'un BL est reprise de sa commande).
        foreach (var code in valeurs.Keys.Where(c => champs.All(ch => ch.Code != c)).ToList())
            valeurs[code] = null;

        await VerifierDansSageAsync(valeurs);
        VerifierFichier(demande);

        var (rapport, article, bl, of) = (valeurs[ChampsDocumentExterne.NumeroRapport], valeurs[ChampsDocumentExterne.CodeArticle],
            valeurs[ChampsDocumentExterne.NumeroBl], valeurs[ChampsDocumentExterne.NumeroOf]);
        var (commandeFournisseur, referenceClient) = (valeurs[ChampsDocumentExterne.NumeroCommandeFournisseur], valeurs[ChampsDocumentExterne.ReferenceCommandeClient]);

        var enVigueur = await _depot.GetEnVigueurAsync(type.Code, Cle(type.Code, rapport, article, bl, of, commandeFournisseur, referenceClient));
        string? motif = null;
        if (enVigueur != null)
        {
            if (!demande.Remplacer)
                throw new ConflitConcurrenceException(
                    $"« {enVigueur.NomFichier} » a déjà été déposé le {enVigueur.DeposeLe:dd/MM/yyyy à HH:mm} par {enVigueur.DeposeParNom ?? enVigueur.DeposePar}. "
                    + "Pour le corriger, remplacez-le en indiquant le motif (l'ancien fichier reste consultable).");
            motif = string.IsNullOrWhiteSpace(demande.MotifRemplacement)
                ? throw new RegleMetierException("Indiquez le motif du remplacement.")
                : demande.MotifRemplacement.Trim();
            if (motif.Length > 300) throw new RegleMetierException("Le motif du remplacement fait 300 caractères au plus.");
        }

        var document = new ExternalDocument
        {
            Id = Guid.NewGuid(),
            TypeCode = type.Code,
            ControlReportNumber = rapport,
            ArticleCode = article,
            DeliveryNoteNumber = bl,
            ManufacturingOrderNumber = of,
            SupplierOrderNumber = commandeFournisseur,
            SalesOrderNumber = valeurs[ChampsDocumentExterne.NumeroCommande],
            CustomerOrderReference = referenceClient,
            OriginalFileName = NomOrigine(demande.NomFichierOrigine),
            Content = demande.Contenu,
            UploadedBy = _utilisateur.MatriculeRequis,
            UploadedAt = DateTime.Now,
            ReplacesDocumentId = enVigueur?.Id,
            ReplacementReason = motif
        };
        await _depot.AjouterAsync(document);

        var depose = await _depot.GetAsync(document.Id) ?? throw new RessourceIntrouvableException("Document introuvable après son dépôt.");
        await CopierDansLeRepertoirePartageAsync(type.SharedFolder, depose.NomFichier, demande.Contenu);
        return depose;
    }

    /// <summary>
    /// Les numéros sont ceux que connaît Sage (copies des tables). La référence client est reprise telle qu'écrite dans
    /// Sage ; un BL livre la commande Sage saisie (celle que Sage lui donne) et prend la référence client de cette commande.
    /// </summary>
    private async Task VerifierDansSageAsync(IDictionary<string, string?> valeurs)
    {
        if (valeurs[ChampsDocumentExterne.CodeArticle] is { } article && !await _depot.ArticleExisteAsync(article))
            throw new RegleMetierException($"L'article {article} est inconnu de Sage.");
        if (valeurs[ChampsDocumentExterne.NumeroOf] is { } of && !await _depot.OfExisteAsync(of))
            throw new RegleMetierException($"L'OF {of} est inconnu de Sage.");
        if (valeurs[ChampsDocumentExterne.NumeroCommandeFournisseur] is { } commandeFournisseur && !await _depot.CommandeFournisseurExisteAsync(commandeFournisseur))
            throw new RegleMetierException($"La commande fournisseur {commandeFournisseur} est inconnue de Sage.");
        if (valeurs[ChampsDocumentExterne.ReferenceCommandeClient] is { } saisie)
            valeurs[ChampsDocumentExterne.ReferenceCommandeClient] = await _depot.GetReferenceCommandeClientAsync(saisie)
                ?? throw new RegleMetierException($"Aucune commande client de référence « {saisie} » dans Sage.");
        if (valeurs[ChampsDocumentExterne.NumeroCommande] is { } numeroCommande)
        {
            var commande = await GetCommandeClientAsync(numeroCommande);
            valeurs[ChampsDocumentExterne.ReferenceCommandeClient] = commande.ReferenceCommandeClient
                ?? throw new RegleMetierException($"La commande {numeroCommande} n'a pas de référence client dans Sage : complétez-la dans Sage.");
        }
        if (valeurs[ChampsDocumentExterne.NumeroBl] is { } numeroBl)
        {
            var bl = await _depot.GetBlAsync(numeroBl) ?? throw new RegleMetierException($"Le bon de livraison {numeroBl} est inconnu de Sage.");
            if (bl.NumeroCommande != null && valeurs[ChampsDocumentExterne.NumeroCommande] is { } commandeSaisie && commandeSaisie != bl.NumeroCommande)
                throw new RegleMetierException($"Le bon de livraison {numeroBl} livre la commande {bl.NumeroCommande} (Sage), pas la commande {commandeSaisie}.");
        }
    }

    public async Task<CommandeClientSage> GetCommandeClientAsync(string numeroCommande) =>
        await _depot.GetCommandeClientAsync(Normaliser(numeroCommande) ?? "")
            ?? throw new RegleMetierException($"La commande client {Normaliser(numeroCommande)} est inconnue de Sage.");

    /// <summary>Un PDF (signature « %PDF- »), non vide, dans la taille autorisée.</summary>
    private void VerifierFichier(DepotDocumentExterneRequest demande)
    {
        if (demande.Contenu.Length == 0)
            throw new RegleMetierException("Choisissez le fichier PDF du document.");
        if (demande.Contenu.Length > _parametres.TailleMaximaleMo * 1024L * 1024L)
            throw new RegleMetierException($"Le fichier dépasse {_parametres.TailleMaximaleMo} Mo.");
        if (demande.Contenu.Length < 5 || demande.Contenu[0] != '%' || demande.Contenu[1] != 'P' || demande.Contenu[2] != 'D'
            || demande.Contenu[3] != 'F' || demande.Contenu[4] != '-')
            throw new RegleMetierException("Le fichier doit être un PDF (document scanné ou exporté en PDF).");
    }

    private static string NomOrigine(string? nom)
    {
        var court = System.IO.Path.GetFileName(nom ?? "").Trim();
        if (court.Length == 0) court = "document.pdf";
        return court.Length > 260 ? court[..260] : court;
    }

    /// <summary>Copie facultative : une erreur du répertoire partagé n'annule jamais le dépôt (la base est la référence).</summary>
    private async Task CopierDansLeRepertoirePartageAsync(string sousDossier, string nomFichier, byte[] contenu)
    {
        try
        {
            await _repertoire.CopierAsync(sousDossier, nomFichier, contenu);
        }
        catch (Exception ex)
        {
            _journal.LogWarning(ex, "Copie de {Fichier} dans le répertoire partagé impossible (le document est enregistré en base).", nomFichier);
        }
    }

    // ── Consultation ─────────────────────────────────────────────────────

    public async Task<List<DocumentExterneDto>> ListerAsync(string role, string? typeCode, string? recherche)
    {
        var types = await TypesAccessiblesAsync(role);
        if (typeCode != null) types = types.Where(t => t == typeCode).ToList();
        return types.Count == 0 ? new() : await _depot.ListerAsync(types, string.IsNullOrWhiteSpace(recherche) ? null : recherche.Trim(), NombreMaximalListe);
    }

    public async Task<FichierDto> GetFichierAsync(Guid id, string role)
    {
        var fichier = await _depot.GetFichierAsync(id);
        if (fichier == null || !(await TypesAccessiblesAsync(role)).Contains(fichier.TypeCode))
            throw new RessourceIntrouvableException("Document introuvable.");
        return new FichierDto(fichier.Contenu, "application/pdf", fichier.NomFichier);
    }

    /// <summary>Le responsable qualité consulte tous les documents ; un service, ceux qu'il dépose.</summary>
    private async Task<List<string>> TypesAccessiblesAsync(string role) =>
        (await _depot.GetTypesAsync())
            .Where(t => role == RolesApp.ResponsableQualite || t.UploadRole == role)
            .Select(t => t.Code)
            .ToList();

    // ── Documents d'un OF ────────────────────────────────────────────────

    public async Task<DossierExterneOfDto> GetDossierOfAsync(string numeroOf)
    {
        var types = (await _depot.GetTypesAsync()).ToDictionary(t => t.Code, t => t.Label);
        var lots = await _depot.GetLotsServisAsync(numeroOf);
        var bls = await _depot.GetBlsExpediesAsync(numeroOf);
        var articleProduitFini = await _depot.GetArticleProduitFiniAsync(numeroOf);

        // 1er niveau : rapports des lots, BL de l'expédition et références client de leurs commandes Sage, produit fini.
        var documents = await _depot.GetEnVigueurParNumerosAsync(new NumerosRecherches
        {
            Rapports = lots.Select(l => l.NumeroRapport).OfType<string>().Distinct().ToList(),
            Bls = bls.Select(b => b.NumeroBl).Distinct().ToList(),
            ReferencesClient = bls.Select(b => b.ReferenceCommandeClient).OfType<string>().Distinct().ToList(),
            Articles = articleProduitFini == null ? Array.Empty<string>() : new[] { articleProduitFini },
            NumeroOf = articleProduitFini == null ? null : numeroOf
        });

        // 2e niveau : commandes fournisseur lues dans les rapports, références client lues dans les BL déposés.
        var commandesFournisseur = documents.Where(d => d.TypeCode == TypesDocumentExterne.RapportControle)
            .Select(d => d.NumeroCommandeFournisseur).OfType<string>().Distinct().ToList();
        var referencesClient = documents.Where(d => d.TypeCode == TypesDocumentExterne.BonLivraison)
            .Select(d => d.ReferenceCommandeClient).OfType<string>()
            .Except(bls.Select(b => b.ReferenceCommandeClient).OfType<string>()).Distinct().ToList();
        if (commandesFournisseur.Count > 0 || referencesClient.Count > 0)
            documents.AddRange((await _depot.GetEnVigueurParNumerosAsync(new NumerosRecherches { CommandesFournisseur = commandesFournisseur, ReferencesClient = referencesClient }))
                .Where(d => documents.All(x => x.Id != d.Id)));

        string Libelle(string code) => types.GetValueOrDefault(code, code);
        CarteDocumentExterneDto Carte(string typeCode, DocumentExterneEnVigueur? document, string? nomAttendu, string raison) => document == null
            ? new CarteDocumentExterneDto { TypeCode = typeCode, TypeLibelle = Libelle(typeCode), NomFichier = nomAttendu, Etat = Manquant, RaisonManquant = raison }
            : new CarteDocumentExterneDto
            {
                TypeCode = typeCode, TypeLibelle = Libelle(typeCode), NomFichier = document.NomFichier, Etat = Disponible,
                DocumentId = document.Id, DeposeLe = document.DeposeLe, DeposeParNom = document.DeposeParNom
            };

        var dossier = new DossierExterneOfDto();
        foreach (var lot in lots)
        {
            var rapport = lot.NumeroRapport == null ? null
                : documents.FirstOrDefault(d => d.TypeCode == TypesDocumentExterne.RapportControle && d.NumeroRapport == lot.NumeroRapport);
            var commande = rapport?.NumeroCommandeFournisseur;
            var ligne = new LotFournisseurDto
            {
                CodeArticle = lot.CodeArticle, DesignationArticle = lot.DesignationArticle, NumeroLot = lot.NumeroLot,
                NumeroRapport = lot.NumeroRapport, NumeroCommandeFournisseur = commande
            };
            ligne.Documents.Add(Carte(TypesDocumentExterne.RapportControle, rapport, null, lot.NumeroRapport == null
                ? "Aucun n° de rapport sur l'étiquette scannée par le magasinier."
                : $"Rapport de contrôle {lot.NumeroRapport} non déposé par le service qualité."));
            var sansCommande = lot.NumeroRapport == null
                ? "Commande fournisseur inconnue : l'étiquette ne porte pas de n° de rapport."
                : "Commande fournisseur inconnue tant que le rapport de contrôle n'est pas déposé.";
            ligne.Documents.Add(commande == null
                ? Carte(TypesDocumentExterne.CommandeFournisseur, null, null, sansCommande)
                : Carte(TypesDocumentExterne.CommandeFournisseur,
                    documents.FirstOrDefault(d => d.TypeCode == TypesDocumentExterne.CommandeFournisseur && d.NumeroCommandeFournisseur == commande),
                    Nom(null, null, null, null, commande, null) + ".pdf", $"Bon de commande fournisseur {commande} non déposé par le service achats."));
            ligne.Documents.Add(commande == null
                ? Carte(TypesDocumentExterne.CertificatConformite, null, null, sansCommande)
                : Carte(TypesDocumentExterne.CertificatConformite,
                    documents.FirstOrDefault(d => d.TypeCode == TypesDocumentExterne.CertificatConformite && d.NumeroCommandeFournisseur == commande && d.CodeArticle == lot.CodeArticle),
                    Nom(null, lot.CodeArticle, null, null, commande, null) + ".pdf",
                    $"Certificat de conformité de {lot.CodeArticle} (commande {commande}) non déposé par le service qualité."));
            dossier.Fournisseurs.Add(ligne);
        }

        foreach (var bl in bls)
        {
            var depot = documents.FirstOrDefault(d => d.TypeCode == TypesDocumentExterne.BonLivraison && d.NumeroBl == bl.NumeroBl);
            var reference = bl.ReferenceCommandeClient ?? depot?.ReferenceCommandeClient;
            var livraison = new LivraisonClientDto
            {
                NumeroBl = bl.NumeroBl, CodeClient = bl.CodeClient, DateExpedition = bl.DateExpedition,
                NumeroCommande = bl.NumeroCommande, ReferenceCommandeClient = reference
            };
            livraison.Documents.Add(Carte(TypesDocumentExterne.BonLivraison, depot,
                reference == null ? null : Nom(null, null, bl.NumeroBl, null, null, reference) + ".pdf",
                $"Bon de livraison {bl.NumeroBl} non déposé par le service commercial."));
            livraison.Documents.Add(reference == null
                ? Carte(TypesDocumentExterne.CommandeClient, null, null, bl.NumeroCommande == null
                    ? "Commande client inconnue : Sage ne donne pas la commande de ce BL."
                    : $"Commande client inconnue : la commande {bl.NumeroCommande} n'a pas de référence client dans Sage.")
                : Carte(TypesDocumentExterne.CommandeClient,
                    documents.FirstOrDefault(d => d.TypeCode == TypesDocumentExterne.CommandeClient && d.ReferenceCommandeClient == reference),
                    Nom(null, null, null, null, null, reference) + ".pdf",
                    $"Bon de commande client « {reference} » (commande {bl.NumeroCommande}) non déposé par le service commercial."));
            dossier.Clients.Add(livraison);
        }

        if (articleProduitFini != null)
        {
            dossier.ProduitFini.Add(Carte(TypesDocumentExterne.PlanDefinitionProduitFini,
                documents.FirstOrDefault(d => d.TypeCode == TypesDocumentExterne.PlanDefinitionProduitFini && d.CodeArticle == articleProduitFini),
                articleProduitFini + ".pdf", $"Plan de définition de l'article {articleProduitFini} non déposé par le service développement industriel."));
            dossier.ProduitFini.Add(Carte(TypesDocumentExterne.RapportProduitFini,
                documents.FirstOrDefault(d => d.TypeCode == TypesDocumentExterne.RapportProduitFini && d.NumeroOf == numeroOf),
                numeroOf + ".pdf", $"Rapport de contrôle rapide du produit fini de l'OF {numeroOf} non déposé par le service qualité."));
        }
        return dossier;
    }
}

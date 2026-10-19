using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SopalTrace.Application.DTOs.DocumentsExternes;
using SopalTrace.Application.Interfaces;
using SopalTrace.Application.Services;
using SopalTrace.Application.Settings;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;
using SopalTrace.Domain.Exceptions;
using Xunit;

namespace SopalTrace.Application.Tests.Services;

public class DocumentExterneServiceTests
{
    private static readonly byte[] Pdf = Encoding.ASCII.GetBytes("%PDF-1.4\n%contenu");

    private static readonly List<ExternalDocumentType> Types = new()
    {
        Type(TypesDocumentExterne.RapportControle, RolesApp.ServiceQualite, rapport: true, commandeFournisseur: true),
        Type(TypesDocumentExterne.CertificatConformite, RolesApp.ServiceQualite, article: true, commandeFournisseur: true),
        Type(TypesDocumentExterne.CommandeFournisseur, RolesApp.ServiceAchats, commandeFournisseur: true),
        Type(TypesDocumentExterne.BonLivraison, RolesApp.ServiceCommercial, bl: true, commande: true, referenceClient: true),
        Type(TypesDocumentExterne.CommandeClient, RolesApp.ServiceCommercial, referenceClient: true),
        Type(TypesDocumentExterne.RapportProduitFini, RolesApp.ServiceQualite, of: true),
        Type(TypesDocumentExterne.PlanDefinitionProduitFini, RolesApp.ServiceDI, article: true)
    };

    private readonly Mock<IDocumentExterneRepository> _depot = new();
    private readonly Mock<IRepertoirePartage> _repertoire = new();
    private readonly Mock<ICurrentUserService> _utilisateur = new();
    private readonly DocumentExterneService _service;
    private ExternalDocument? _ajoute;

    public DocumentExterneServiceTests()
    {
        _depot.Setup(d => d.GetTypesAsync()).ReturnsAsync(Types);
        _depot.Setup(d => d.ArticleExisteAsync(It.IsAny<string>())).ReturnsAsync(true);
        _depot.Setup(d => d.OfExisteAsync(It.IsAny<string>())).ReturnsAsync(true);
        _depot.Setup(d => d.CommandeFournisseurExisteAsync(It.IsAny<string>())).ReturnsAsync(true);
        // Références client de Sage (comparées sans casse, rendues telles qu'écrites dans Sage).
        _depot.Setup(d => d.GetReferenceCommandeClientAsync(It.IsAny<string>()))
            .ReturnsAsync((string r) => new[] { "OA 044803", "9400108469" }.FirstOrDefault(x => string.Equals(x, r, StringComparison.OrdinalIgnoreCase)));
        _depot.Setup(d => d.GetCommandeClientAsync("SOPCOV-1")).ReturnsAsync(new CommandeClientSage("SOPCOV-1", "OA 044803"));
        _depot.Setup(d => d.GetCommandeClientAsync("SOPCOV-2")).ReturnsAsync(new CommandeClientSage("SOPCOV-2", null));
        _depot.Setup(d => d.GetCommandeClientAsync("SOPCOV-3")).ReturnsAsync(new CommandeClientSage("SOPCOV-3", "marché 11/2025"));
        _depot.Setup(d => d.GetBlAsync("BL1")).ReturnsAsync(new BlSage("BL1", "SOPCOV-1", "OA 044803"));
        _depot.Setup(d => d.GetBlAsync("BL2")).ReturnsAsync(new BlSage("BL2", "SOPCOV-2", null));
        _depot.Setup(d => d.AjouterAsync(It.IsAny<ExternalDocument>())).Callback<ExternalDocument>(d => _ajoute = d).Returns(Task.CompletedTask);
        _depot.Setup(d => d.GetAsync(It.IsAny<Guid>())).ReturnsAsync((Guid id) => new DocumentExterneDto
        {
            Id = id, TypeCode = _ajoute!.TypeCode, RemplaceDocumentId = _ajoute.ReplacesDocumentId,
            NomFichier = DocumentExterneService.Nom(_ajoute.ControlReportNumber, _ajoute.ArticleCode, _ajoute.DeliveryNoteNumber,
                _ajoute.ManufacturingOrderNumber, _ajoute.SupplierOrderNumber, _ajoute.CustomerOrderReference) + ".pdf"
        });
        _utilisateur.Setup(u => u.MatriculeRequis).Returns("88883");
        _service = new DocumentExterneService(_depot.Object, _repertoire.Object, _utilisateur.Object,
            new DocumentsExternesSettings { TailleMaximaleMo = 1 }, NullLogger<DocumentExterneService>.Instance);
    }

    private static ExternalDocumentType Type(string code, string role, bool rapport = false, bool article = false, bool bl = false,
        bool of = false, bool commandeFournisseur = false, bool commande = false, bool referenceClient = false) => new()
    {
        Code = code, Label = code, UploadRole = role, SharedFolder = code,
        RequiresControlReportNumber = rapport, RequiresArticleCode = article, RequiresDeliveryNoteNumber = bl,
        RequiresManufacturingOrderNumber = of, RequiresSupplierOrderNumber = commandeFournisseur, RequiresSalesOrderNumber = commande,
        RequiresCustomerOrderReference = referenceClient
    };

    private static DepotDocumentExterneRequest Rapport(string numero = "rc1 ", string commande = "cf1") => new()
    {
        TypeCode = TypesDocumentExterne.RapportControle, NumeroRapport = numero, NumeroCommandeFournisseur = commande,
        NomFichierOrigine = @"C:\scans\scan 12.pdf", Contenu = Pdf
    };

    private static DepotDocumentExterneRequest Commercial(string type, string? bl, string reference) => new()
    {
        TypeCode = type, NumeroBl = bl, ReferenceCommandeClient = reference, NomFichierOrigine = "c.pdf", Contenu = Pdf
    };

    private static DepotDocumentExterneRequest Bl(string bl, string commande) => new()
    {
        TypeCode = TypesDocumentExterne.BonLivraison, NumeroBl = bl, NumeroCommande = commande, NomFichierOrigine = "bl.pdf", Contenu = Pdf
    };

    // ── Nommage ──

    [Theory]
    [InlineData(TypesDocumentExterne.RapportControle, "RC1", null, null, null, "CF1", null, "RC1-CF1", "RC1")]
    [InlineData(TypesDocumentExterne.CertificatConformite, null, "ECROU-001", null, null, "CF1", null, "ECROU-001-CF1", "ECROU-001-CF1")]
    [InlineData(TypesDocumentExterne.CommandeFournisseur, null, null, null, null, "CF1", null, "CF1", "CF1")]
    [InlineData(TypesDocumentExterne.BonLivraison, null, null, "BL1", null, null, "9400108469", "BL1-9400108469", "BL1")]
    [InlineData(TypesDocumentExterne.CommandeClient, null, null, null, null, null, "marché 11/2025", "marché_11_2025", "marché_11_2025")]
    [InlineData(TypesDocumentExterne.RapportProduitFini, null, null, null, "OF1", null, null, "OF1", "OF1")]
    [InlineData(TypesDocumentExterne.PlanDefinitionProduitFini, null, "2511A01-1", null, null, null, null, "2511A01-1", "2511A01-1")]
    public void Nom_EtCle_CommeLaBase(string type, string? rapport, string? article, string? bl, string? of, string? cf, string? reference, string nom, string cle)
    {
        Assert.Equal(nom, DocumentExterneService.Nom(rapport, article, bl, of, cf, reference));
        Assert.Equal(cle, DocumentExterneService.Cle(type, rapport, article, bl, of, cf, reference));
    }

    [Fact]
    public async Task GetTypes_SeulementCeuxDuServiceAvecLeursNumerosDansLOrdreDuNom()
    {
        var types = await _service.GetTypesAsync(RolesApp.ServiceQualite);

        Assert.Equal(new[] { TypesDocumentExterne.RapportControle, TypesDocumentExterne.CertificatConformite, TypesDocumentExterne.RapportProduitFini },
            types.Select(t => t.Code));
        Assert.Equal(new[] { ChampsDocumentExterne.CodeArticle, ChampsDocumentExterne.NumeroCommandeFournisseur }, types[1].Champs.Select(c => c.Code));
        var commercial = await _service.GetTypesAsync(RolesApp.ServiceCommercial);
        // BL : n° du BL (dans le nom) et commande Sage (hors du nom) ; la référence client est reprise de la commande.
        Assert.Equal(new[] { (ChampsDocumentExterne.NumeroBl, true), (ChampsDocumentExterne.NumeroCommande, false) },
            commercial[0].Champs.Select(c => (c.Code, c.DansLeNom)));
        Assert.True(commercial[0].ReferenceClientDeLaCommande);
        Assert.Equal(new[] { FormatsChamp.Reference }, commercial[1].Champs.Select(c => c.Format));
        Assert.Equal(new[] { TypesDocumentExterne.PlanDefinitionProduitFini }, (await _service.GetTypesAsync(RolesApp.ServiceDI)).Select(t => t.Code));
    }

    // ── Dépôt ──

    [Fact]
    public async Task Deposer_NumerosNormalises_NumerosNonExigesIgnores_CopieDansLeRepertoire()
    {
        var demande = Rapport();
        demande.CodeArticle = "ECROU-001";   // non exigé par un rapport de contrôle

        var document = await _service.DeposerAsync(demande, RolesApp.ServiceQualite);

        Assert.Equal("RC1", _ajoute!.ControlReportNumber);
        Assert.Equal("CF1", _ajoute.SupplierOrderNumber);
        Assert.Null(_ajoute.ArticleCode);
        Assert.Equal("scan 12.pdf", _ajoute.OriginalFileName);
        Assert.Equal("88883", _ajoute.UploadedBy);
        Assert.Null(_ajoute.ReplacesDocumentId);
        Assert.Equal("RC1-CF1.pdf", document.NomFichier);
        _repertoire.Verify(r => r.CopierAsync(TypesDocumentExterne.RapportControle, "RC1-CF1.pdf", Pdf));
    }

    [Fact]
    public async Task Deposer_TypeDUnAutreService_Refuse()
    {
        var ex = await Assert.ThrowsAsync<RegleMetierException>(() => _service.DeposerAsync(Rapport(), RolesApp.ServiceAchats));
        Assert.Contains("autre service", ex.Message);
    }

    [Theory]
    [InlineData("", "Saisissez")]
    [InlineData("RC 1/2", "lettres, chiffres")]
    public async Task Deposer_NumeroManquantOuInvalide_Refuse(string numero, string message)
    {
        var ex = await Assert.ThrowsAsync<RegleMetierException>(() => _service.DeposerAsync(Rapport(numero), RolesApp.ServiceQualite));
        Assert.Contains(message, ex.Message);
    }

    [Fact]
    public async Task Deposer_CommandeInconnueDeSage_Refuse()
    {
        _depot.Setup(d => d.CommandeFournisseurExisteAsync("CF9")).ReturnsAsync(false);
        var ex = await Assert.ThrowsAsync<RegleMetierException>(() => _service.DeposerAsync(Rapport(commande: "cf9"), RolesApp.ServiceQualite));
        Assert.Contains("inconnue de Sage", ex.Message);
    }

    [Fact]
    public async Task Deposer_BcClient_ReferenceRepriseTelleQueDansSage()
    {
        var document = await _service.DeposerAsync(Commercial(TypesDocumentExterne.CommandeClient, null, "  oa 044803 "), RolesApp.ServiceCommercial);

        Assert.Equal("OA 044803", _ajoute!.CustomerOrderReference);
        Assert.Equal("OA_044803.pdf", document.NomFichier);
    }

    [Fact]
    public async Task Deposer_ReferenceClientInconnueOuCaractereInterdit_Refuse()
    {
        var demande = Commercial(TypesDocumentExterne.CommandeClient, null, "REF-9");
        Assert.Contains("dans Sage", (await Assert.ThrowsAsync<RegleMetierException>(() => _service.DeposerAsync(demande, RolesApp.ServiceCommercial))).Message);
        demande.ReferenceCommandeClient = "OA*044803";
        Assert.Contains("interdits", (await Assert.ThrowsAsync<RegleMetierException>(() => _service.DeposerAsync(demande, RolesApp.ServiceCommercial))).Message);
    }

    [Fact]
    public async Task Deposer_Bl_ReferenceClientRepriseDeLaCommandeSaisie()
    {
        var demande = Bl("bl1", "sopcov-1");
        demande.ReferenceCommandeClient = "saisie ignorée";

        var document = await _service.DeposerAsync(demande, RolesApp.ServiceCommercial);

        Assert.Equal(("SOPCOV-1", "OA 044803"), (_ajoute!.SalesOrderNumber, _ajoute.CustomerOrderReference));
        Assert.Equal("BL1-OA_044803.pdf", document.NomFichier);
    }

    [Fact]
    public async Task Deposer_Bl_CommandeInconnueSansReferenceOuAutreQueSage_Refuse()
    {
        Assert.Contains("inconnue de Sage", (await Assert.ThrowsAsync<RegleMetierException>(() =>
            _service.DeposerAsync(Bl("BL1", "SOPCOV-9"), RolesApp.ServiceCommercial))).Message);
        Assert.Contains("pas de référence client", (await Assert.ThrowsAsync<RegleMetierException>(() =>
            _service.DeposerAsync(Bl("BL1", "SOPCOV-2"), RolesApp.ServiceCommercial))).Message);
        var ex = await Assert.ThrowsAsync<RegleMetierException>(() => _service.DeposerAsync(Bl("BL1", "SOPCOV-3"), RolesApp.ServiceCommercial));
        Assert.Contains("livre la commande SOPCOV-1", ex.Message);
    }

    [Fact]
    public async Task Deposer_PasUnPdfOuTropGros_Refuse()
    {
        var demande = Rapport();
        demande.Contenu = Encoding.ASCII.GetBytes("PK\u0003\u0004 classeur");
        Assert.Contains("PDF", (await Assert.ThrowsAsync<RegleMetierException>(() => _service.DeposerAsync(demande, RolesApp.ServiceQualite))).Message);

        demande.Contenu = Pdf.Concat(new byte[1024 * 1024]).ToArray();
        Assert.Contains("1 Mo", (await Assert.ThrowsAsync<RegleMetierException>(() => _service.DeposerAsync(demande, RolesApp.ServiceQualite))).Message);
    }

    [Fact]
    public async Task Deposer_DejaEnVigueur_SansRemplacement_ConflitAvecLeDocumentExistant()
    {
        _depot.Setup(d => d.GetEnVigueurAsync(TypesDocumentExterne.RapportControle, "RC1"))
            .ReturnsAsync(new DocumentExterneDto { Id = Guid.NewGuid(), NomFichier = "RC1-CF0.pdf", DeposeParNom = "Qualite Reception Test", DeposeLe = DateTime.Now });

        var ex = await Assert.ThrowsAsync<ConflitConcurrenceException>(() => _service.DeposerAsync(Rapport(), RolesApp.ServiceQualite));
        Assert.Contains("RC1-CF0.pdf", ex.Message);
        _depot.Verify(d => d.AjouterAsync(It.IsAny<ExternalDocument>()), Times.Never);
    }

    [Fact]
    public async Task Deposer_Remplacement_MotifObligatoire_PuisAncienRemplace()
    {
        var ancien = Guid.NewGuid();
        _depot.Setup(d => d.GetEnVigueurAsync(TypesDocumentExterne.RapportControle, "RC1"))
            .ReturnsAsync(new DocumentExterneDto { Id = ancien, NomFichier = "RC1-CF0.pdf", DeposeLe = DateTime.Now });
        var demande = Rapport();
        demande.Remplacer = true;
        await Assert.ThrowsAsync<RegleMetierException>(() => _service.DeposerAsync(demande, RolesApp.ServiceQualite));

        demande.MotifRemplacement = "  Mauvaise commande saisie ";
        var document = await _service.DeposerAsync(demande, RolesApp.ServiceQualite);

        Assert.Equal(ancien, _ajoute!.ReplacesDocumentId);
        Assert.Equal("Mauvaise commande saisie", _ajoute.ReplacementReason);
        Assert.Equal(ancien, document.RemplaceDocumentId);
    }

    [Fact]
    public async Task Deposer_RepertoirePartageEnErreur_DepotGarde()
    {
        _repertoire.Setup(r => r.CopierAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>())).ThrowsAsync(new System.IO.IOException("partage absent"));

        var document = await _service.DeposerAsync(Rapport(), RolesApp.ServiceQualite);

        Assert.Equal("RC1-CF1.pdf", document.NomFichier);
    }

    // ── Accès ──

    [Fact]
    public async Task GetFichier_ResponsableQualiteToutUnServiceSesTypes()
    {
        var id = Guid.NewGuid();
        _depot.Setup(d => d.GetFichierAsync(id)).ReturnsAsync(new FichierDocumentExterne(TypesDocumentExterne.BonLivraison, "BL1-OA_044803.pdf", Pdf));

        Assert.Equal("BL1-OA_044803.pdf", (await _service.GetFichierAsync(id, RolesApp.ResponsableQualite)).NomFichier);
        Assert.Equal("application/pdf", (await _service.GetFichierAsync(id, RolesApp.ServiceCommercial)).TypeContenu);
        await Assert.ThrowsAsync<RessourceIntrouvableException>(() => _service.GetFichierAsync(id, RolesApp.ServiceAchats));
    }

    // ── Documents d'un OF ──

    private static DocumentExterneEnVigueur Depose(string type, string nom, string? rapport = null, string? article = null, string? bl = null,
        string? of = null, string? cf = null, string? reference = null) =>
        new(Guid.NewGuid(), type, nom, rapport, article, bl, of, cf, reference, DateTime.Now, "Test");

    [Fact]
    public async Task GetDossierOf_CascadeDepuisLesLotsLesBlEtLArticle()
    {
        _depot.Setup(d => d.GetLotsServisAsync("OF1")).ReturnsAsync(new List<LotServi>
        {
            new("ECROU-001", "Ecrous", "L1", "RC1"),
            new("JOINT-001", "Joint", "L2", "RC2"),
            new("PISTON-001", "Piston", "L3", null)
        });
        _depot.Setup(d => d.GetBlsExpediesAsync("OF1")).ReturnsAsync(new List<BlExpedie>
        {
            new("BL1", "CLI", DateOnly.FromDateTime(DateTime.Today), "SOPCOV-1", "OA 044803"),
            new("BL2", "CLI", DateOnly.FromDateTime(DateTime.Today), "SOPCOV-2", null)
        });
        _depot.Setup(d => d.GetArticleProduitFiniAsync("OF1")).ReturnsAsync("PF-001");
        // 1er niveau : rapport RC1 (commande CF1), BL1 et plan de définition déposés ; rapport du produit fini absent.
        _depot.Setup(d => d.GetEnVigueurParNumerosAsync(It.Is<NumerosRecherches>(n => n.Rapports.Count == 2 && n.NumeroOf == "OF1"
                && n.ReferencesClient.SequenceEqual(new[] { "OA 044803" }) && n.Articles.SequenceEqual(new[] { "PF-001" }))))
            .ReturnsAsync(new List<DocumentExterneEnVigueur>
            {
                Depose(TypesDocumentExterne.RapportControle, "RC1-CF1.pdf", rapport: "RC1", cf: "CF1"),
                Depose(TypesDocumentExterne.BonLivraison, "BL1-OA_044803.pdf", bl: "BL1", reference: "OA 044803"),
                Depose(TypesDocumentExterne.PlanDefinitionProduitFini, "PF-001.pdf", article: "PF-001")
            });
        // 2e niveau : commande CF1 → certificat de l'écrou déposé, bon de commande absent.
        _depot.Setup(d => d.GetEnVigueurParNumerosAsync(It.Is<NumerosRecherches>(n => n.Rapports.Count == 0
                && n.CommandesFournisseur.SequenceEqual(new[] { "CF1" }))))
            .ReturnsAsync(new List<DocumentExterneEnVigueur> { Depose(TypesDocumentExterne.CertificatConformite, "ECROU-001-CF1.pdf", article: "ECROU-001", cf: "CF1") });

        var dossier = await _service.GetDossierOfAsync("OF1");

        string Etats(IEnumerable<CarteDocumentExterneDto> cartes) => string.Join(" ", cartes.Select(c => c.Etat[0]));
        var ecrou = dossier.Fournisseurs.Single(l => l.NumeroLot == "L1");
        Assert.Equal("CF1", ecrou.NumeroCommandeFournisseur);
        Assert.Equal("D M D", Etats(ecrou.Documents));              // rapport, bon de commande, certificat
        Assert.Equal("CF1.pdf", ecrou.Documents[1].NomFichier);       // nom attendu du document manquant
        var joint = dossier.Fournisseurs.Single(l => l.NumeroLot == "L2");
        Assert.Equal("M M M", Etats(joint.Documents));
        Assert.Contains("RC2", joint.Documents[0].RaisonManquant);
        Assert.Contains("étiquette", dossier.Fournisseurs.Single(l => l.NumeroLot == "L3").Documents[0].RaisonManquant);

        var bl1 = dossier.Clients.Single(b => b.NumeroBl == "BL1");
        Assert.Equal(("SOPCOV-1", "OA 044803"), (bl1.NumeroCommande, bl1.ReferenceCommandeClient));
        Assert.Equal("D M", Etats(bl1.Documents));
        Assert.Equal("OA_044803.pdf", bl1.Documents[1].NomFichier);
        var bl2 = dossier.Clients.Single(b => b.NumeroBl == "BL2");
        Assert.Equal("M M", Etats(bl2.Documents));
        Assert.Contains("SOPCOV-2 n'a pas de référence client", bl2.Documents[1].RaisonManquant);

        Assert.Equal(new[] { TypesDocumentExterne.PlanDefinitionProduitFini, TypesDocumentExterne.RapportProduitFini },
            dossier.ProduitFini.Select(c => c.TypeCode));
        Assert.Equal("D M", Etats(dossier.ProduitFini));
        Assert.Equal("OF1.pdf", dossier.ProduitFini[1].NomFichier);
    }
}

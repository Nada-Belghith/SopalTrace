using System.Collections.Generic;
using System.Threading.Tasks;
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

public class RelanceDocumentsExternesServiceTests
{
    private readonly Mock<IDocumentExterneRepository> _depot = new();
    private readonly Mock<IUserRepository> _utilisateurs = new();
    private readonly Mock<IEmailService> _email = new();
    private readonly RelanceDocumentsExternesService _service;

    public RelanceDocumentsExternesServiceTests()
    {
        _depot.Setup(d => d.GetTypesAsync()).ReturnsAsync(new List<ExternalDocumentType>
        {
            new()
            {
                Code = TypesDocumentExterne.CertificatConformite, Label = "Certificat de conformité", UploadRole = RolesApp.ServiceQualite,
                SharedFolder = "C", RequiresArticleCode = true, RequiresSupplierOrderNumber = true
            },
            new()
            {
                Code = TypesDocumentExterne.CommandeClient, Label = "Bon de commande client", UploadRole = RolesApp.ServiceCommercial,
                SharedFolder = "BC", RequiresCustomerOrderReference = true
            }
        });
        _utilisateurs.Setup(u => u.GetEmailsByRoleAsync(RolesApp.ServiceQualite)).ReturnsAsync(new List<string> { "q1@s.com", "q2@s.com" });
        _utilisateurs.Setup(u => u.GetEmailsByRoleAsync(RolesApp.ServiceCommercial)).ReturnsAsync(new List<string>());
        _service = new RelanceDocumentsExternesService(_depot.Object, _utilisateurs.Object, _email.Object,
            new AuthSettings { FrontendBaseUrl = "https://sopaltrace.tn/" });
    }

    [Fact]
    public async Task Relance_EnvoieAuServiceAvecNomAttenduEtLienPrerempli()
    {
        var resultat = await _service.RelancerAsync(new RelanceDocumentExterneRequest
        {
            TypeCode = TypesDocumentExterne.CertificatConformite, NumeroOf = "OF1",
            Numeros = new() { [ChampsDocumentExterne.CodeArticle] = "ECROU-001", [ChampsDocumentExterne.NumeroCommandeFournisseur] = "CF2607001" }
        });

        Assert.Equal((RolesApp.ServiceQualite, 2), (resultat.Service, resultat.Destinataires));
        _email.Verify(e => e.EnvoyerAsync("q1@s.com", It.Is<string>(s => s.Contains("OF1")), It.Is<string>(c =>
            c.Contains("ECROU-001-CF2607001.pdf")
            && c.Contains("https://sopaltrace.tn/documents-externes/depot?type=CONFORMITY_CERTIFICATE&amp;codeArticle=ECROU-001&amp;numeroCommandeFournisseur=CF2607001")),
            true), Times.Once);
        _email.Verify(e => e.EnvoyerAsync("q2@s.com", It.IsAny<string>(), It.IsAny<string>(), true), Times.Once);
    }

    [Fact]
    public async Task Relance_UnSeulNumeroConnu_EnvoyeeSansNomAttendu()
    {
        // Rapport de contrôle : seul son n° est connu (étiquette) ; la commande fournisseur est écrite dans le rapport.
        _depot.Setup(d => d.GetTypesAsync()).ReturnsAsync(new List<ExternalDocumentType>
        {
            new()
            {
                Code = TypesDocumentExterne.RapportControle, Label = "Rapport de contrôle", UploadRole = RolesApp.ServiceQualite,
                SharedFolder = "R", RequiresControlReportNumber = true, RequiresSupplierOrderNumber = true
            }
        });

        await _service.RelancerAsync(new RelanceDocumentExterneRequest
        {
            TypeCode = TypesDocumentExterne.RapportControle, Numeros = new() { [ChampsDocumentExterne.NumeroRapport] = "61946" }
        });

        _email.Verify(e => e.EnvoyerAsync("q1@s.com", It.IsAny<string>(), It.Is<string>(c =>
            c.Contains("<b>61946</b>") && !c.Contains("Nom attendu") && c.Contains("type=CONTROL_REPORT&amp;numeroRapport=61946")), true), Times.Once);
    }

    [Fact]
    public async Task Relance_AucunNumero_Refusee()
    {
        await Assert.ThrowsAsync<RegleMetierException>(() => _service.RelancerAsync(new RelanceDocumentExterneRequest
        {
            TypeCode = TypesDocumentExterne.CertificatConformite, Numeros = new() { [ChampsDocumentExterne.CodeArticle] = " " }
        }));
        _email.Verify(e => e.EnvoyerAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task Relance_EnvoiImpossible_MessageClair()
    {
        _email.Setup(e => e.EnvoyerAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ThrowsAsync(new System.Exception("SMTP non configuré"));

        var ex = await Assert.ThrowsAsync<RegleMetierException>(() => _service.RelancerAsync(new RelanceDocumentExterneRequest
        {
            TypeCode = TypesDocumentExterne.CertificatConformite,
            Numeros = new() { [ChampsDocumentExterne.CodeArticle] = "ECROU-001", [ChampsDocumentExterne.NumeroCommandeFournisseur] = "CF1" }
        }));
        Assert.Contains("n'a pas pu être envoyée", ex.Message);
    }

    [Fact]
    public async Task Relance_ServiceSansCompteActif_Refusee()
    {
        var ex = await Assert.ThrowsAsync<RegleMetierException>(() => _service.RelancerAsync(new RelanceDocumentExterneRequest
        {
            TypeCode = TypesDocumentExterne.CommandeClient,
            Numeros = new() { [ChampsDocumentExterne.ReferenceCommandeClient] = "OA 044803" }
        }));
        Assert.Contains(RolesApp.ServiceCommercial, ex.Message);
    }
}

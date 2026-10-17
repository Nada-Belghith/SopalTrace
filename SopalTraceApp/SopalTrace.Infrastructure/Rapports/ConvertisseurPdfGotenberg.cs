using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using SopalTrace.Application.Interfaces.Rapports;
using SopalTrace.Application.Settings;
using SopalTrace.Domain.Exceptions;

namespace SopalTrace.Infrastructure.Rapports;

/// <summary>
/// Conversion Excel → PDF par Gotenberg (LibreOffice dans un conteneur, joignable uniquement par l'API).
/// Le PDF reçu est contrôlé (signature %PDF) avant d'être archivé.
/// </summary>
public sealed class ConvertisseurPdfGotenberg : IConvertisseurPdf
{
    private static readonly byte[] SignaturePdf = "%PDF-"u8.ToArray();

    private readonly HttpClient _http;
    private readonly RapportsSettings _parametres;

    public ConvertisseurPdfGotenberg(HttpClient http, RapportsSettings parametres)
    {
        _http = http;
        _parametres = parametres;
    }

    public bool Configure => !string.IsNullOrWhiteSpace(_parametres.GotenbergUrl);

    public async Task<bool> DisponibleAsync(CancellationToken annulation = default)
    {
        if (!Configure) return false;
        try
        {
            using var reponse = await _http.GetAsync(Adresse("/health"), annulation);
            return reponse.IsSuccessStatusCode;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !annulation.IsCancellationRequested)
        {
            return false;
        }
    }

    public async Task<byte[]> ConvertirAsync(byte[] classeur, CancellationToken annulation = default)
    {
        if (!Configure)
            throw new ServiceConversionIndisponibleException("La conversion PDF n'est pas configurée (Rapports:GotenbergUrl).");

        using var formulaire = new MultipartFormDataContent();
        var fichier = new ByteArrayContent(classeur);
        fichier.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        formulaire.Add(fichier, "files", "document.xlsx");

        HttpResponseMessage reponse;
        try
        {
            reponse = await _http.PostAsync(Adresse("/forms/libreoffice/convert"), formulaire, annulation);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !annulation.IsCancellationRequested)
        {
            throw new ServiceConversionIndisponibleException("Le service de conversion PDF ne répond pas : la génération reprendra automatiquement.");
        }

        using (reponse)
        {
            if (!reponse.IsSuccessStatusCode)
                throw new ServiceConversionIndisponibleException($"Le service de conversion PDF a refusé le document (code {(int)reponse.StatusCode}).");
            var pdf = await reponse.Content.ReadAsByteArrayAsync(annulation);
            if (pdf.Length < SignaturePdf.Length || !pdf.AsSpan(0, SignaturePdf.Length).SequenceEqual(SignaturePdf))
                throw new ServiceConversionIndisponibleException("Le service de conversion PDF a renvoyé un fichier qui n'est pas un PDF.");
            return pdf;
        }
    }

    private Uri Adresse(string chemin) => new(_parametres.GotenbergUrl.TrimEnd('/') + chemin);
}

/// <summary>
/// Modèles à importer : fichiers {CODE}.xlsx d'un dossier choisi (Rapports:DossierModelesLivres). L'application n'en
/// livre aucun : l'administrateur envoie ses modèles ; les tests importent ainsi ceux de tests/fixtures/modeles.
/// </summary>
public sealed class ModelesLivres : IModelesLivres
{
    private readonly string _dossier;

    public ModelesLivres(string dossier) => _dossier = dossier;

    public byte[]? Lire(string codeModele)
    {
        if (string.IsNullOrWhiteSpace(_dossier)) return null;
        var chemin = System.IO.Path.Combine(_dossier, codeModele + ".xlsx");
        return System.IO.File.Exists(chemin) ? System.IO.File.ReadAllBytes(chemin) : null;
    }
}

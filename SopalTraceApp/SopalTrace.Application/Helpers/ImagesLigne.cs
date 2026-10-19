using System;
using System.Linq;
using SopalTrace.Domain.Entities;
using SopalTrace.Domain.Exceptions;

namespace SopalTrace.Application.Helpers;

/// <summary>
/// Croquis d'une ligne de document (table DocumentLineImage). Le front échange l'image sous forme de
/// data URL (« data:image/png;base64,… ») ; elle est stockée en binaire, hors de la ligne.
/// Seuls PNG, JPEG et GIF sont acceptés : le format est déterminé par le contenu réel, jamais par l'en-tête
/// déclaré (un SVG ou un script déguisé est refusé).
/// </summary>
public static class ImagesLigne
{
    public const int TailleMaximaleOctets = 2 * 1024 * 1024;

    private static readonly (string ContentType, byte[] Signature)[] Formats =
    {
        ("image/png",  new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
        ("image/jpeg", new byte[] { 0xFF, 0xD8, 0xFF }),
        ("image/gif",  new byte[] { 0x47, 0x49, 0x46, 0x38 })
    };

    /// <summary>Image de la ligne en data URL, ou null (la navigation DocumentLineImage doit être chargée).</summary>
    public static string? EnDataUrl(this DocumentLigne ligne)
        => ligne.DocumentLineImage is { } image
            ? $"data:{image.ContentType};base64,{Convert.ToBase64String(image.Content)}"
            : null;

    /// <summary>
    /// Remplace, ajoute ou retire l'image de la ligne. Sur une ligne existante, la navigation DocumentLineImage
    /// doit avoir été chargée pour que l'image actuelle soit mise à jour ou supprimée.
    /// </summary>
    public static void DefinirImage(this DocumentLigne ligne, string? dataUrl)
    {
        if (string.IsNullOrWhiteSpace(dataUrl))
        {
            ligne.DocumentLineImage = null;
            return;
        }

        var (contentType, contenu) = Decoder(dataUrl);
        if (ligne.DocumentLineImage is { } existante)
        {
            existante.ContentType = contentType;
            existante.Content = contenu;
        }
        else
        {
            ligne.DocumentLineImage = new DocumentLineImage { LineId = ligne.Id, Line = ligne, ContentType = contentType, Content = contenu };
        }
    }

    /// <summary>Image d'une nouvelle ligne (initialiseur d'objet), ou null si aucune image n'est fournie.</summary>
    public static DocumentLineImage? Creer(string? dataUrl)
    {
        if (string.IsNullOrWhiteSpace(dataUrl)) return null;
        var (contentType, contenu) = Decoder(dataUrl);
        return new DocumentLineImage { ContentType = contentType, Content = contenu };
    }

    /// <summary>Indique si la data URL est une image acceptée : sert à écarter une image importée inutilisable.</summary>
    public static bool EstAcceptee(string? dataUrl)
    {
        try { Decoder(dataUrl ?? string.Empty); return true; }
        catch (RegleMetierException) { return false; }
    }

    private static (string ContentType, byte[] Contenu) Decoder(string dataUrl)
    {
        var separateur = dataUrl.IndexOf(";base64,", StringComparison.OrdinalIgnoreCase);
        if (!dataUrl.StartsWith("data:", StringComparison.OrdinalIgnoreCase) || separateur < 0)
            throw new RegleMetierException("Image illisible : format attendu « data:image/…;base64,… ».");

        byte[] contenu;
        try { contenu = Convert.FromBase64String(dataUrl[(separateur + ";base64,".Length)..]); }
        catch (FormatException) { throw new RegleMetierException("Image illisible : contenu base64 invalide."); }

        if (contenu.Length == 0 || contenu.Length > TailleMaximaleOctets)
            throw new RegleMetierException($"Image refusée : taille maximale {TailleMaximaleOctets / (1024 * 1024)} Mo.");

        var format = Formats.FirstOrDefault(f => contenu.Length >= f.Signature.Length && contenu.AsSpan(0, f.Signature.Length).SequenceEqual(f.Signature));
        if (format.ContentType == null)
            throw new RegleMetierException("Image refusée : seuls les formats PNG, JPEG et GIF sont acceptés.");

        return (format.ContentType, contenu);
    }
}

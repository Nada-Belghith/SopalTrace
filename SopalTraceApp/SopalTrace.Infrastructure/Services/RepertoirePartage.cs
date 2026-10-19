using System;
using System.IO;
using System.Threading.Tasks;
using SopalTrace.Application.Interfaces;
using SopalTrace.Application.Settings;

namespace SopalTrace.Infrastructure.Services;

/// <summary>
/// Recopie un document déposé dans le répertoire partagé des services : un sous-dossier par type, le nom normalisé
/// formé par la base (un remplacement écrase l'ancienne copie ; l'historique reste en base). Sans répertoire configuré,
/// rien n'est copié.
/// </summary>
public class RepertoirePartage : IRepertoirePartage
{
    private readonly string _racine;

    public RepertoirePartage(DocumentsExternesSettings parametres) => _racine = parametres.RepertoirePartage?.Trim() ?? "";

    public async Task CopierAsync(string sousDossier, string nomFichier, byte[] contenu)
    {
        if (_racine.Length == 0) return;
        // Noms formés par l'application (sous-dossier du type, numéros vérifiés) : on refuse tout de même de sortir du répertoire.
        var dossier = Path.GetFullPath(Path.Combine(_racine, sousDossier));
        var chemin = Path.GetFullPath(Path.Combine(dossier, nomFichier));
        var racine = Path.GetFullPath(_racine).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!chemin.StartsWith(racine, StringComparison.OrdinalIgnoreCase) || Path.GetFileName(chemin) != nomFichier)
            throw new InvalidOperationException($"Chemin refusé dans le répertoire partagé : {sousDossier}/{nomFichier}.");

        Directory.CreateDirectory(dossier);
        var temporaire = chemin + ".tmp";
        await File.WriteAllBytesAsync(temporaire, contenu);
        File.Move(temporaire, chemin, overwrite: true);
    }
}

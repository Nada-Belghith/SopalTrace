namespace SopalTrace.Application.Settings;

/// <summary>Section « DocumentsExternes » de la configuration.</summary>
public class DocumentsExternesSettings
{
    /// <summary>
    /// Répertoire partagé des services où l'application recopie chaque document déposé, sous son nom normalisé
    /// (un sous-dossier par type). Vide : aucune copie, les documents sont seulement en base.
    /// </summary>
    public string RepertoirePartage { get; set; } = "";

    /// <summary>Taille maximale d'un fichier déposé, en mégaoctets.</summary>
    public int TailleMaximaleMo { get; set; } = 10;
}

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SopalTrace.Application.Rapports;

namespace SopalTrace.Application.Interfaces.Rapports;

/// <summary>Champ trouvé dans un modèle, avec son emplacement (« cellule B8 », « pied de page »).</summary>
public sealed record ChampTrouve(string Nom, string Emplacement);

/// <summary>Résultat de la lecture d'un modèle : erreurs de structure (bloquantes), avertissements et champs trouvés.</summary>
public sealed class AnalyseModele
{
    public List<string> Erreurs { get; } = new();
    public List<string> Avertissements { get; } = new();
    public List<ChampTrouve> Champs { get; } = new();
    public bool Valide => Erreurs.Count == 0;
}

/// <summary>
/// Lit et remplit un modèle Excel. Le modèle contient une seule feuille ; les champs {{...}} de portée Section et
/// Ligne forment un bloc répété : les lignes du titre de section (et des en-têtes de colonnes) sont répétées pour
/// chaque section, la ligne contenant les champs Ligne.* pour chaque ligne de la section. Tout le reste
/// (logo, mise en page, texte fixe, en-tête et pied de page) est conservé tel quel.
/// </summary>
public interface IMoteurModeleExcel
{
    AnalyseModele Analyser(byte[] contenu);

    /// <summary>Classeur rempli (.xlsx). Le modèle doit être valide (voir <see cref="Analyser"/>).</summary>
    byte[] Remplir(byte[] contenu, DonneesRapport donnees);
}

/// <summary>Conversion d'un classeur Excel en PDF (service Gotenberg / LibreOffice).</summary>
public interface IConvertisseurPdf
{
    /// <summary>Faux si aucune adresse de service n'est configurée.</summary>
    bool Configure { get; }
    Task<bool> DisponibleAsync(CancellationToken annulation = default);
    Task<byte[]> ConvertirAsync(byte[] classeur, CancellationToken annulation = default);
}

/// <summary>Fichiers de modèles à importer automatiquement (tests ; l'application n'en livre aucun).</summary>
public interface IModelesLivres
{
    /// <summary>Contenu du modèle livré pour ce code, ou null s'il n'y en a pas.</summary>
    byte[]? Lire(string codeModele);
}


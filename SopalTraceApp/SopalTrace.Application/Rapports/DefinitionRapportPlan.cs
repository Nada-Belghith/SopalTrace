using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SopalTrace.Application.Helpers;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;

namespace SopalTrace.Application.Rapports;

/// <summary>
/// Contrat entre les modèles Excel d'un type de document et la base : liste des champs qu'un modèle peut utiliser.
/// Un champ absent de cette liste est refusé à l'envoi du modèle ; afficher une nouvelle information demande de
/// l'ajouter ici (les colonnes propres aux formulaires, {{Ligne.Colonne.CLE}}, n'en ont pas besoin).
/// </summary>
public interface IContratRapport
{
    IReadOnlyList<ChampRapport> Champs { get; }

    /// <summary>
    /// Champs sans lesquels un fichier n'est pas un modèle de ce type de document (ex. un autre formulaire envoyé par
    /// erreur) : leur absence est un refus.
    /// </summary>
    IReadOnlyList<string> ChampsObligatoires { get; }
}

/// <summary>Contrat d'un type de document qualité (Document_Entete) et construction de ses valeurs.</summary>
public interface IDefinitionRapport : IContratRapport
{
    string TypeDocumentCode { get; }

    /// <summary>Valeurs des champs (document chargé avec ses sections, lignes, colonnes et référentiels).</summary>
    DonneesRapport Construire(DocumentEntete document);

    /// <summary>Nom du fichier généré, sans extension.</summary>
    string NomFichier(DocumentEntete document);
}

/// <summary>
/// Plans de contrôle à sections (plan d'assemblage) : en-tête du document, sections, lignes de caractéristiques et
/// colonnes propres au formulaire. Les valeurs sont celles saisies par le superviseur, sans déduction depuis un texte.
/// </summary>
public sealed class DefinitionRapportPlan : IDefinitionRapport
{
    private static readonly CultureInfo Francais = CultureInfo.GetCultureInfo("fr-FR");

    public DefinitionRapportPlan(string typeDocumentCode) => TypeDocumentCode = typeDocumentCode;

    public string TypeDocumentCode { get; }

    /// <summary>Le tableau du plan : titre de chaque section et caractéristique de chaque ligne.</summary>
    public IReadOnlyList<string> ChampsObligatoires { get; } = new[] { "Section.Libelle", "Ligne.Caracteristique" };

    public IReadOnlyList<ChampRapport> Champs { get; } = new List<ChampRapport>
    {
        new("Date", "Date de la version du document (dernière modification, sinon création)", "14/10/2026"),
        new("CodeArticle", "Article du document ; plan de famille : « type de robinet : famille » (soupape : avec le poste) ; sinon la nature d'article", "SOUP : RBGFA_SOUPAPE · poste PAS71"),
        new("Designation", "Désignation de l'article ou de la famille, sinon celle du document", "Robinet bouteille de gaz à fermeture automatique"),
        new("NomDocument", "Nom du document", "Plan ASS RBGFA-BAC-01 V1"),
        new("DesignationDocument", "Désignation saisie sur le document", "Plan de contrôle assemblage"),
        new("VersionDocument", "Version du document", "1"),
        new("CommentaireVersion", "Motif de la version du document", "Ajout du contrôle d'étanchéité"),
        new("CodeFormulaire", "Code du formulaire du document", "FE0594"),
        new("VersionFormulaire", "Version du formulaire", "0"),
        new("DesignationFormulaire", "Désignation du formulaire", "Plan ASS RBGFA-BAC-01"),
        new("Famille", "Code de la famille de produit fini", "RBGFA-BAC-01"),
        new("DesignationFamille", "Désignation de la famille de produit fini", "Robinet bouteille de gaz à fermeture automatique"),
        new("TypeRobinet", "Code du type de robinet de la famille", "SOUP"),
        new("DesignationTypeRobinet", "Libellé du type de robinet de la famille", "Automatique avec soupape"),
        new("Nature", "Code de la nature d'article", "ASS"),
        new("DesignationNature", "Libellé de la nature d'article", "Assemblage"),
        new("Poste", "Code du poste de travail", "PAS71"),
        new("DesignationPoste", "Libellé du poste de travail", "Poste d'assemblage 71"),
        new("Operation", "Libellé de l'opération", "Assemblage"),
        new("Machine", "Code de la machine", "MAS26"),
        new("Remarques", "Remarques du document (ex. NB en bas du plan)", "NB : Pour chaque lot, vérifier l'aptitude à l'emploi des matériaux."),
        new("LegendeMoyens", "Légende des moyens de contrôle", "V : visuel ; C : calibre"),

        new("Section.Numero", "Numéro de la section dans le document", "1"),
        new("Section.Libelle", "Titre de la section", "Caractéristiques à contrôler aux réglages (une série de 12 pièces)"),
        new("Section.TypeSection", "Type de la section", "aux réglages"),
        new("Section.Periodicite", "Périodicité de la section", "une série de 12 pièces"),
        new("Section.Nqa", "NQA de la section", "0,65"),
        new("Section.Notes", "Notes de la section", "Contrôle à chaque changement de série"),

        new("Ligne.Numero", "Numéro de la ligne dans la section", "1"),
        new("Ligne.Caracteristique", "Caractéristique contrôlée", "Étanchéité du clapet"),
        new("Ligne.TypeCaracteristique", "Type de caractéristique", "Fonctionnelle"),
        new("Ligne.LimiteSpecification", "Limite de spécification", "0 fuite"),
        new("Ligne.TypeControle", "Type de contrôle", "100 %"),
        new("Ligne.MoyenControle", "Moyen de contrôle", "Banc d'étanchéité"),
        new("Ligne.Instrument", "Code de l'instrument de contrôle", "BE-01"),
        new("Ligne.DesignationInstrument", "Désignation de l'instrument de contrôle", "Banc d'étanchéité n°1"),
        new("Ligne.Periodicite", "Périodicité de la ligne", "5 pièces / heure"),
        new("Ligne.Critique", "« Oui » si la caractéristique est critique, vide sinon", "Oui"),
        new("Ligne.Instruction", "Instruction de contrôle", "Immerger le robinet 30 s"),
        new("Ligne.Observations", "Observations", "Arrêt de la production si fuite"),
        new("Ligne.Machine", "Code de la machine contrôlée", "MAS26"),
        new("Ligne.RisqueDefaut", "Défaut surveillé", "Fuite clapet"),
    };

    public DonneesRapport Construire(DocumentEntete document)
    {
        var donnees = new DonneesRapport();
        var champs = donnees.Champs;
        champs["Date"] = (document.ModifieLe ?? document.CreeLe).ToString("dd/MM/yyyy", Francais);
        champs["CodeArticle"] = CodeArticle(document);
        champs["Designation"] = Premier(document.FamilleProduitFiniCodeNavigation?.Designation, document.Designation, document.Nom);
        champs["NomDocument"] = document.Nom;
        champs["DesignationDocument"] = Texte(document.Designation);
        champs["VersionDocument"] = document.Version.ToString(Francais);
        champs["CommentaireVersion"] = Texte(document.CommentaireVersion);
        champs["CodeFormulaire"] = Texte(document.Formulaire?.CodeReference);
        champs["VersionFormulaire"] = document.Formulaire?.Version.ToString(Francais) ?? "";
        champs["DesignationFormulaire"] = Texte(document.Formulaire?.Designation);
        champs["Famille"] = Texte(document.FamilleProduitFiniCode);
        champs["DesignationFamille"] = Texte(document.FamilleProduitFiniCodeNavigation?.Designation);
        champs["TypeRobinet"] = Texte(document.FamilleProduitFiniCodeNavigation?.TypeRobinetCode);
        champs["DesignationTypeRobinet"] = Texte(document.FamilleProduitFiniCodeNavigation?.TypeRobinetCodeNavigation?.Libelle);
        champs["Nature"] = Texte(document.NatureArticleCode);
        champs["DesignationNature"] = Texte(document.NatureArticleCodeNavigation?.Libelle);
        champs["Poste"] = Texte(document.PosteCode);
        champs["DesignationPoste"] = Texte(document.PosteCodeNavigation?.Libelle);
        champs["Operation"] = Texte(document.OperationCodeNavigation?.Libelle);
        champs["Machine"] = Texte(document.MachineCode);
        champs["Remarques"] = Texte(document.Remarques);
        champs["LegendeMoyens"] = Texte(document.LegendeMoyens);

        var numeroSection = 0;
        foreach (var section in document.DocumentSections.OrderBy(s => s.OrdreAffiche))
        {
            var donneesSection = new DonneesSectionRapport();
            var s = donneesSection.Champs;
            s["Numero"] = (++numeroSection).ToString(Francais);
            s["Libelle"] = section.LibelleSection;
            s["TypeSection"] = Texte(section.TypeSection?.Libelle);
            s["Periodicite"] = Periodicite(section.Periodicite);
            s["Nqa"] = section.Nqa?.ValeurNqa.ToString("0.###", Francais) ?? "";
            s["Notes"] = Texte(section.Notes);

            var numeroLigne = 0;
            foreach (var ligne in section.DocumentLignes.OrderBy(l => l.OrdreAffiche))
            {
                var donneesLigne = new DonneesLigneRapport();
                var l = donneesLigne.Champs;
                l["Numero"] = (++numeroLigne).ToString(Francais);
                l["Caracteristique"] = Premier(ligne.LibelleAffiche, ligne.Caracteristique?.Libelle);
                l["TypeCaracteristique"] = Premier(ligne.TypeCaracteristique?.Libelle, ligne.Caracteristique?.TypeCaracteristique?.Libelle);
                l["LimiteSpecification"] = Texte(ligne.LimiteSpecTexte);
                l["TypeControle"] = Texte(ligne.TypeControle?.Libelle);
                l["MoyenControle"] = Premier(ligne.MoyenControle?.Libelle, ligne.MoyenTexteLibre);
                l["Instrument"] = Texte(ligne.InstrumentCode);
                l["DesignationInstrument"] = Texte(ligne.InstrumentCodeNavigation?.Designation);
                l["Periodicite"] = Periodicite(ligne.Periodicite);
                l["Critique"] = ligne.EstCritique ? "Oui" : "";
                l["Instruction"] = Texte(ligne.Instruction);
                l["Observations"] = Texte(ligne.Observations);
                l["Machine"] = Texte(ligne.MachineCode);
                l["RisqueDefaut"] = Texte(ligne.RisqueDefaut?.LibelleDefaut);
                foreach (var colonne in ligne.DocumentLigneExtraColonnes)
                    donneesLigne.Colonnes[colonne.CleColonne] = Texte(colonne.ValeurColonne);
                donneesSection.Lignes.Add(donneesLigne);
            }
            donnees.Sections.Add(donneesSection);
        }
        return donnees;
    }

    public string NomFichier(DocumentEntete document) =>
        TexteFichier.Nettoyer($"{document.TypeDocumentCode}_{Premier(document.CodeArticleSageVersionne, document.FamilleProduitFiniCode, document.NatureArticleCode)}_V{document.Version}");

    /// <summary>
    /// Article du document ; un plan de famille affiche « type de robinet : famille » (ex. AUTO : RBGFA-BAC-01) ; un plan
    /// de robinet avec soupape, propre à un poste, y ajoute le poste (ex. SOUP : RBGFA_SOUPAPE · poste PAS71).
    /// </summary>
    public static string CodeArticle(DocumentEntete document)
    {
        if (!string.IsNullOrWhiteSpace(document.CodeArticleSageVersionne)) return document.CodeArticleSageVersionne.Trim();
        if (!string.IsNullOrWhiteSpace(document.FamilleProduitFiniCode))
        {
            var type = document.FamilleProduitFiniCodeNavigation?.TypeRobinetCode?.Trim();
            var code = string.IsNullOrWhiteSpace(type) ? document.FamilleProduitFiniCode.Trim() : $"{type} : {document.FamilleProduitFiniCode.Trim()}";
            return type == TypesRobinet.AvecSoupape && !string.IsNullOrWhiteSpace(document.PosteCode) ? $"{code} · poste {document.PosteCode.Trim()}" : code;
        }
        return Texte(document.NatureArticleCode);
    }

    private static string Periodicite(Periodicite? periodicite) =>
        periodicite == null ? "" : LibellesSection.Periodicite(periodicite.NbrPieces, periodicite.Unite, periodicite.NbrHeures);

    private static string Texte(string? valeur) => valeur?.Trim() ?? "";

    private static string Premier(params string?[] valeurs) =>
        valeurs.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))?.Trim() ?? "";
}

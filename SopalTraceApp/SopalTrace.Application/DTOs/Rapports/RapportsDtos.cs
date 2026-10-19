using System;
using System.Collections.Generic;

namespace SopalTrace.Application.DTOs.Rapports;

/// <summary>Modèle de rapport, ses versions (sans le contenu des fichiers) et l'historique de ses affectations.</summary>
public class ModeleRapportDto
{
    public string Code { get; set; } = "";
    public string Nom { get; set; } = "";
    /// <summary>Type de document qualité mis en forme (vide pour un document d'exécution).</summary>
    public string TypeDocumentCode { get; set; } = "";
    /// <summary>Type de document d'exécution mis en forme (vide pour un document qualité).</summary>
    public string TypeExecutionCode { get; set; } = "";
    /// <summary>Rôle des formulaires dont ce modèle met en forme les documents ; vide : documents sans formulaire.</summary>
    public string? FormRole { get; set; }
    /// <summary>Version par défaut actuelle (modèle sans formulaire : dernière affectation par défaut).</summary>
    public int? VersionParDefaut { get; set; }
    public List<VersionModeleDto> Versions { get; set; } = new();
    public List<AffectationDto> Affectations { get; set; } = new();
    /// <summary>Formulaires dont les prochains documents utilisent ce modèle.</summary>
    public List<string> Formulaires { get; set; } = new();
    /// <summary>Formulaires de ce rôle sans modèle (colonnes changées ou rien de rattaché) : leurs PDF sont refusés.</summary>
    public List<string> FormulairesSansModele { get; set; } = new();
}

public class VersionModeleDto
{
    public Guid Id { get; set; }
    public string CodeModele { get; set; } = "";
    /// <summary>Formulaire auquel le modèle est dédié (identifiant commun à ses versions) ; null : modèle par défaut.</summary>
    public Guid? GroupeFormulaire { get; set; }
    /// <summary>Nom du modèle, propre à son formulaire.</summary>
    public string Nom { get; set; } = "";
    /// <summary>Numéro dans son formulaire (dans son type pour un modèle par défaut).</summary>
    public int Numero { get; set; }
    public string NomFichier { get; set; } = "";
    /// <summary>DRAFT (brouillon) ou PUBLISHED.</summary>
    public string Statut { get; set; } = "";
    public string? Commentaire { get; set; }
    public List<string> Avertissements { get; set; } = new();
    public string? EmpreinteSha256 { get; set; }
    public string? EnvoyePar { get; set; }
    public string? EnvoyeParNom { get; set; }
    public DateTime EnvoyeLe { get; set; }
    public string? PubliePar { get; set; }
    public string? PublieParNom { get; set; }
    public DateTime? PublieLe { get; set; }
}

/// <summary>Affectation d'une version de modèle (historique, jamais modifié).</summary>
public class AffectationDto
{
    public long Id { get; set; }
    public Guid VersionId { get; set; }
    public int NumeroVersion { get; set; }
    /// <summary>Nom du modèle rattaché (propre à son formulaire).</summary>
    public string NomModele { get; set; } = "";
    /// <summary>DEFAULT, FORM_STRUCTURE ou FORM_REVISION.</summary>
    public string Nature { get; set; } = "";
    public Guid? FormulaireId { get; set; }
    /// <summary>Identifiant commun aux versions (et aux changements de code) du formulaire : historique par formulaire.</summary>
    public Guid? GroupeFormulaire { get; set; }
    public string? CodeFormulaire { get; set; }
    public int? VersionFormulaire { get; set; }
    public string? Commentaire { get; set; }
    public string? AffectePar { get; set; }
    public string? AffecteParNom { get; set; }
    public DateTime AffecteLe { get; set; }
}

/// <summary>Formulaires en cours d'un rôle, et les modèles Excel qui peuvent mettre en forme leurs documents.</summary>
public class FormulairesDuRoleDto
{
    public string Role { get; set; } = "";
    public string Libelle { get; set; } = "";
    /// <summary>DOCUMENT (contenu défini par le superviseur qualité) ou RESPONSE_SHEET (fiche de réponse des opérateurs).</summary>
    public string Nature { get; set; } = "";
    /// <summary>Modèles Excel de ce rôle (vide : aucun modèle n'existe encore pour ces documents).</summary>
    public List<ModeleDuRoleDto> Modeles { get; set; } = new();
    public List<FormulaireModeleDto> Formulaires { get; set; } = new();
}

public sealed record ModeleDuRoleDto(string Code, string Nom);

/// <summary>Version de formulaire d'un type de document, ses colonnes et le modèle de ses prochains documents.</summary>
public class FormulaireModeleDto
{
    public Guid Id { get; set; }
    /// <summary>Identifiant commun à toutes les versions du formulaire (historique de ses modèles).</summary>
    public Guid GroupeId { get; set; }
    public string CodeReference { get; set; } = "";
    public int Version { get; set; }
    public string Statut { get; set; } = "";
    public string? Role { get; set; }
    /// <summary>Champ d'application (famille, poste, machine…) : ce que désigne le code choisi par le superviseur.</summary>
    public string ChampApplication { get; set; } = "";
    public List<ColonneFormulaireDto> Colonnes { get; set; } = new();
    /// <summary>Modèle d'un document créé maintenant sur cette version du formulaire.</summary>
    public ModeleDocumentDto? ModeleActuel { get; set; }
    /// <summary>Demande de modèle en attente chez l'administrateur, s'il y en a une.</summary>
    public DemandeModeleDto? DemandeEnAttente { get; set; }
    /// <summary>Sans modèle : STRUCTURE_MODIFIEE (colonnes changées) ou AUCUN_MODELE, et le message à afficher.</summary>
    public string? ModeleManquant { get; set; }
    public string? MessageModeleManquant { get; set; }
}

/// <summary>Demande de modèle : ce que l'administrateur doit rattacher à une ligne de formulaire, et pourquoi.</summary>
public class DemandeModeleDto
{
    public long Id { get; set; }
    public Guid FormulaireId { get; set; }
    public Guid GroupeFormulaire { get; set; }
    public string CodeFormulaire { get; set; } = "";
    public int VersionFormulaire { get; set; }
    public string? Role { get; set; }
    public string RoleLibelle { get; set; } = "";
    public string Nature { get; set; } = "";
    public string ChampApplication { get; set; } = "";
    /// <summary>NEW_FORM, NEW_STRUCTURE ou SUPERVISOR.</summary>
    public string Origine { get; set; } = "";
    public string? Motif { get; set; }
    /// <summary>SAME_VERSION ou NEXT_VERSION (demande du superviseur), appliqué tel quel au rattachement.</summary>
    public string? ChangementVersion { get; set; }
    /// <summary>PENDING, DONE ou CANCELLED.</summary>
    public string Statut { get; set; } = "";
    public string? DemandePar { get; set; }
    public string? DemandeParNom { get; set; }
    public DateTime DemandeLe { get; set; }
    public string? TraitePar { get; set; }
    public string? TraiteParNom { get; set; }
    public DateTime? TraiteLe { get; set; }
    /// <summary>Modèle rattaché pour satisfaire la demande (demande traitée).</summary>
    public Guid? VersionModeleId { get; set; }
    public int? NumeroModele { get; set; }
    public string? NomModele { get; set; }
}

public class ColonneFormulaireDto
{
    public string Cle { get; set; } = "";
    public string Libelle { get; set; } = "";
    /// <summary>Texte à écrire dans le modèle : {{Ligne.Colonne.CLE}}.</summary>
    public string Syntaxe => "{{Ligne.Colonne." + Cle + "}}";
}

/// <summary>Résultat de la vérification d'un fichier de modèle.</summary>
public class VerificationModeleDto
{
    public bool Accepte => Erreurs.Count == 0;
    public List<string> Erreurs { get; set; } = new();
    public List<string> Avertissements { get; set; } = new();
    /// <summary>Champs trouvés dans le fichier, avec leur emplacement.</summary>
    public List<ChampUtiliseDto> Champs { get; set; } = new();
    /// <summary>Version créée (brouillon) quand le fichier est accepté.</summary>
    public VersionModeleDto? Version { get; set; }
}

public class ChampUtiliseDto
{
    public string Nom { get; set; } = "";
    public string Emplacement { get; set; } = "";
}

/// <summary>Champ qu'un modèle peut utiliser.</summary>
public class ChampDisponibleDto
{
    public string Nom { get; set; } = "";
    /// <summary>Texte à écrire dans le modèle, ex. {{Ligne.Caracteristique}}.</summary>
    public string Syntaxe => "{{" + Nom + "}}";
    /// <summary>DOCUMENT, SECTION, LIGNE ou COLONNE (colonne propre aux formulaires).</summary>
    public string Portee { get; set; } = "";
    public string Description { get; set; } = "";
    public string Exemple { get; set; } = "";
}

/// <summary>Résultat d'une publication (par défaut, affectation à un formulaire ou révision).</summary>
public class PublicationModeleDto
{
    public VersionModeleDto Version { get; set; } = new();
    /// <summary>Formulaires qui reçoivent cette version (affectation ou révision).</summary>
    public List<string> Formulaires { get; set; } = new();
    public bool ParDefaut { get; set; }
}

/// <summary>Modèle qui met en forme un document, et pourquoi.</summary>
public class ModeleDocumentDto
{
    public Guid VersionId { get; set; }
    public string CodeModele { get; set; } = "";
    public int NumeroVersion { get; set; }
    /// <summary>Nom normalisé du modèle (désignation du formulaire — CODE.version.numéro).</summary>
    public string? NomModele { get; set; }
    /// <summary>EN_VIGUEUR_A_LA_CREATION, CONCU_POUR_LA_VERSION_DU_FORMULAIRE ou PREMIER_MODELE.</summary>
    public string Regle { get; set; } = "";
    public string Explication { get; set; } = "";
}

/// <summary>Document (qualité ou d'exécution) consultable en PDF, avec le modèle qui le met en forme.</summary>
public class SourceDocumentDto
{
    public Guid? DocumentEnteteId { get; set; }
    public Guid? ExecControleDocumentStatutId { get; set; }
    /// <summary>QUALITE ou EXECUTION.</summary>
    public string Categorie { get; set; } = "";
    public string TypeCode { get; set; } = "";
    public string TypeLibelle { get; set; } = "";
    public string Nom { get; set; } = "";
    public int? Version { get; set; }
    public string Statut { get; set; } = "";
    public string? NumeroOf { get; set; }
    public string? Poste { get; set; }
    public string? Machine { get; set; }
    /// <summary>Opération de fabrication du document (ASS, USINAG…) et son libellé.</summary>
    public string? Operation { get; set; }
    public string? OperationLibelle { get; set; }
    public string? Formulaire { get; set; }
    /// <summary>Ligne de formulaire du document (code + version), pour rattacher un modèle qui manque.</summary>
    public Guid? FormulaireId { get; set; }
    public DateTime? Date { get; set; }
    /// <summary>Les champs de ce type de document sont définis (un modèle peut le mettre en forme).</summary>
    public bool ContratDisponible { get; set; }
    /// <summary>Modèle en vigueur pour ce document ; null : aucun modèle (voir ModeleManquant).</summary>
    public ModeleDocumentDto? Modele { get; set; }
    /// <summary>Sans modèle : STRUCTURE_MODIFIEE, AUCUN_MODELE ou SANS_FORMULAIRE, et le message à afficher.</summary>
    public string? ModeleManquant { get; set; }
    public string? MessageModeleManquant { get; set; }
    /// <summary>Traçabilité d'un OF : DISPONIBLE (PDF possible) ou MANQUANT, avec la raison.</summary>
    public string? Etat { get; set; }
    public string? RaisonManquant { get; set; }
}

/// <summary>Document généré (sans le contenu des fichiers).</summary>
public class DocumentGenereDto
{
    public Guid Id { get; set; }
    public Guid? DocumentEnteteId { get; set; }
    public Guid? ExecControleDocumentStatutId { get; set; }
    public string NomFichier { get; set; } = "";
    public string CodeModele { get; set; } = "";
    public int VersionModele { get; set; }
    public Guid VersionModeleId { get; set; }
    public string? EmpreintePdfSha256 { get; set; }
    public DateTime GenereLe { get; set; }
    public string? DemandePar { get; set; }
    public string? DemandeParNom { get; set; }
}

/// <summary>Un document, le modèle qui le met en forme, le modèle actuel s'il diffère, et ses PDF déjà produits.</summary>
public class EtatDocumentDto
{
    public SourceDocumentDto Source { get; set; } = new();
    /// <summary>Modèle actuel de ce document (le plus récent), s'il diffère de son modèle : réimpression possible.</summary>
    public ModeleDocumentDto? ModeleActuel { get; set; }
    public List<DocumentGenereDto> Documents { get; set; } = new();
    /// <summary>Fiche que remplissent les opérateurs avec ce document (null : ce type de document n'en a pas).</summary>
    public FicheViergeDto? FicheVierge { get; set; }
}

/// <summary>
/// Fiche vierge d'un document qualité : la fiche d'exécution (contrôle au poste, vérification machine, échantillonnage,
/// résultats d'assemblage) mise en forme avec le modèle d'une fiche créée maintenant, réponses des opérateurs vides.
/// </summary>
public class FicheViergeDto
{
    /// <summary>Type de la fiche (TypesDocumentExecution).</summary>
    public string TypeExecution { get; set; } = "";
    public string TypeLibelle { get; set; } = "";
    /// <summary>Formulaire de la fiche (code + version), son identifiant pour rattacher un modèle qui manque.</summary>
    public string? Formulaire { get; set; }
    public Guid? FormulaireId { get; set; }
    public ModeleDocumentDto? Modele { get; set; }
    public string? ModeleManquant { get; set; }
    public string? MessageModeleManquant { get; set; }
}

/// <summary>Documents d'un OF pour le tableau de traçabilité : documents qualité appliqués et documents d'exécution.</summary>
/// <summary>État d'un document dans la traçabilité d'un OF.</summary>
public static class EtatsTracabilite
{
    public const string Disponible = "DISPONIBLE";
    public const string Manquant = "MANQUANT";
}

public class TracabiliteOfDto
{
    public string NumeroOf { get; set; } = "";
    public List<SourceDocumentDto> Documents { get; set; } = new();
    /// <summary>Documents externes de l'OF (fournisseurs, clients, produit fini), retrouvés en cascade.</summary>
    public SopalTrace.Application.DTOs.DocumentsExternes.DossierExterneOfDto Externes { get; set; } = new();
    /// <summary>
    /// OF des semi-finis (corps, volant) choisis dans le registre de traçabilité de cet OF d'assemblage : leurs documents
    /// de fabrication (tronçonnage, estampage, usinage) et les lots de matière servis.
    /// </summary>
    public List<SemiFiniTracabiliteDto> SemiFinis { get; set; } = new();
}

/// <summary>OF d'un semi-fini utilisé par l'OF d'assemblage, avec son propre dossier.</summary>
public class SemiFiniTracabiliteDto
{
    public string NumeroOf { get; set; } = "";
    public string CodeArticle { get; set; } = "";
    public string? Designation { get; set; }
    public string? Nature { get; set; }
    public List<SourceDocumentDto> Documents { get; set; } = new();
    public SopalTrace.Application.DTOs.DocumentsExternes.DossierExterneOfDto Externes { get; set; } = new();
}

/// <summary>Fichier à télécharger.</summary>
public sealed record FichierDto(byte[] Contenu, string TypeContenu, string NomFichier);

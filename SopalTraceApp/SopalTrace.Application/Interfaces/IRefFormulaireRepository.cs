using SopalTrace.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SopalTrace.Application.Interfaces;

/// <summary>Document générique non archivé d'une ligne de formulaire.</summary>
public sealed record DocumentActifFormulaire(Guid Id, string Nom, int Version, string Statut, string TypeDocument, string CodeFormulaire, int VersionFormulaire);

/// <summary>Élément d'une liste du champ d'application d'un formulaire (une famille porte aussi son type de robinet).</summary>
public sealed record ElementChampApplication(string Code, string Libelle, string? TypeRobinet = null);

/// <summary>Listes proposées pour le champ d'application d'un formulaire.</summary>
public sealed record ListesChampApplication(List<ElementChampApplication> Familles, List<ElementChampApplication> Postes,
    List<ElementChampApplication> Machines, List<ElementChampApplication> Natures, List<ElementChampApplication> TypesRobinet);

/// <summary>
/// Rôle d'un formulaire (table FormRole) : type de document, critères permis pour son champ d'application (nature et
/// type de robinet ne servent que sans famille) et nature du type : DOCUMENT (contenu défini par le superviseur qualité)
/// ou RESPONSE_SHEET (fiche de réponse remplie par les opérateurs).
/// </summary>
public sealed record RoleFormulaire(string Code, string Libelle, int Ordre, bool ParFamille, bool ParPoste, bool ParMachine,
    bool ParNature, bool ParTypeRobinet, string Nature);

public interface IRefFormulaireRepository
{
    /// <summary>Familles, postes, machines et natures actifs, pour choisir le champ d'application d'un formulaire.</summary>
    Task<ListesChampApplication> GetListesChampApplicationAsync();
    /// <summary>Rôles actifs, dans l'ordre d'affichage.</summary>
    Task<List<RoleFormulaire>> GetRolesAsync();
    /// <summary>Type de robinet de la famille, ou null si la famille n'existe pas.</summary>
    Task<string?> GetTypeRobinetDeFamilleAsync(string familleCode);
    Task<RefFormulaire?> GetByIdAsync(Guid id);
    Task UpdateAsync(RefFormulaire entity);
    Task AddAsync(RefFormulaire entity);
    Task<RefFormulaire?> GetFormulaireActifByRoleAsync(string role);
    Task<RefFormulaire?> GetFormulaireActifByCodeReferenceAsync(string codeReference);
    /// <summary>Formulaire en cours (brouillon ou actif) de ce rôle et de ce champ d'application exact, ou null.</summary>
    Task<RefFormulaire?> GetFormulaireEnCoursParChampAsync(SopalTrace.Application.Helpers.ChampFormulaire champ);
    /// <summary>Une version (même archivée) porte déjà ce code.</summary>
    Task<bool> CodeExisteAsync(string codeReference);
    /// <summary>Code utilisé par une version d'un autre formulaire (autre groupe de versions), même archivée.</summary>
    Task<bool> CodeUtiliseParUnAutreFormulaireAsync(string codeReference, Guid groupe);
    /// <summary>Une ligne de formulaire porte déjà ce code et cette version (même archivée).</summary>
    Task<bool> CodeVersionExisteAsync(string codeReference, int version);
    /// <summary>La ligne de formulaire sert à un document ou à une fiche : son identité est figée.</summary>
    Task<bool> EstUtiliseAsync(Guid formulaireId);
    /// <summary>Corrige le code d'une ligne de formulaire jamais utilisée.</summary>
    Task ChangerCodeAsync(Guid formulaireId, string ancienCode, string nouveauCode);
    /// <summary>Demande automatique de modèle à l'administrateur (nouveau formulaire, nouvelle structure), si le type a un modèle Excel.</summary>
    Task DemanderModeleAsync(Guid formulaireId, string origine, string? motif);
    /// <summary>La nouvelle ligne (même structure) reçoit le modèle actuel de l'ancienne (rattachement explicite « repris de »).</summary>
    Task<bool> RecopierModeleAsync(Guid ancienId, Guid nouveauId, string motif);
    /// <summary>La demande de modèle en attente de l'ancienne ligne suit la nouvelle.</summary>
    Task DeplacerDemandesModeleAsync(Guid ancienId, Guid nouveauId);
    /// <summary>Annule la demande en attente d'une ligne (devenue sans objet).</summary>
    Task AnnulerDemandesModeleAsync(Guid formulaireId);
    /// <summary>
    /// Changement de code d'une ligne utilisée, d'un seul tenant : ses documents génériques en cours puis la ligne sont
    /// archivés tels quels ; une nouvelle ligne (même version, même champ d'application, nouveau code) reçoit la structure,
    /// puis chaque document recréé à l'identique ; le changement est journalisé (FormCodeChange).
    /// </summary>
    Task<(Guid FormulaireId, int DocumentsRecrees)> RemplacerParNouveauCodeAsync(Guid formulaireId, string nouveauCode, string matricule);
    /// <summary>Documents génériques non archivés de la ligne de formulaire : ceux qu'un changement de code recrée.</summary>
    Task<System.Collections.Generic.List<DocumentActifFormulaire>> GetDocumentsActifsAsync(Guid formulaireId);
    Task<System.Collections.Generic.IEnumerable<RefFormulaire>> GetFormulairesByRoleAsync(string role);
    /// <summary>Plus haute version du formulaire (toutes ses versions, quel que soit leur code).</summary>
    Task<int> GetMaxVersionDuGroupeAsync(Guid groupe);
    Task UpdateStatutAsync(Guid formulaireId, string statut);
    Task SyncColonnesAsync(Guid formulaireId, System.Collections.Generic.List<SopalTrace.Application.Helpers.ColonneJsonDto> parsedCols);
    Task<System.Collections.Generic.List<RefFormulaireColonneDef>> GetColonnesActivesByFormulaireIdAsync(Guid formulaireId);
    Task<System.Collections.Generic.List<RefFormulaireEquipe>> GetEquipesActivesByFormulaireIdAsync(Guid formulaireId);
    /// <summary>Équipes de l'usine (E1, E2, E3…) actives.</summary>
    Task<System.Collections.Generic.List<Team>> GetEquipesReferenceAsync();
    Task SyncEquipesAsync(Guid formulaireId, System.Collections.Generic.List<SopalTrace.Application.Helpers.EquipeJsonDto> parsedEquipes);
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SopalTrace.Application.DTOs.QualityPlans.Referentiels;

namespace SopalTrace.Application.Interfaces;

/// <summary>Résultat d'un changement de code : la ligne de formulaire en cours et le nombre de documents recréés.</summary>
public sealed record ChangementCodeFormulaire(Guid FormulaireId, string Code, int Version, int DocumentsRecrees);

public interface IFormulaireStructureService
{
    Task<FormulaireStructureDto?> GetFormulaireByRoleAsync(string role);
    Task<FormulaireStructureDto?> GetFormulaireByIdAsync(Guid id);
    Task<FormulaireStructureDto?> GetFormulaireActifParCodeReferenceAsync(string codeReference);
    Task<IEnumerable<FormulaireReferenceItemDto>> GetFormulairesListByRoleAsync(string role);
    
    /// <summary>
    /// Archive le formulaire actif identifié par son codeReference et crée une nouvelle version active avec version+1.
    /// Si codeReference est null, utilise le role pour trouver le formulaire actif (comportement générique). La version
    /// de départ d'un formulaire se donne une seule fois, à la création de son code (page Formulaires).
    /// </summary>
    Task<(Guid Id, int Version)?> UpdateFormulaireStructureAsync(string role, string? configurationStructureJson, string? codeReference = null, bool isCorrectionMineure = false, bool forceNouvelleVersion = false);
    
    Task<bool> ActiverFormulaireAsync(Guid id);

    /// <summary>
    /// Code du formulaire d'un document. Code indiqué : ce formulaire s'il existe, sinon un nouveau formulaire créé avec
    /// ce code et ce champ d'application (refusé si le champ a déjà son formulaire). Sans code : le formulaire en cours de
    /// ce champ d'application ; refus clair s'il n'y en a pas. L'application ne fabrique jamais de code.
    /// </summary>
    Task<string> CodeFormulaireAsync(SopalTrace.Application.Helpers.ChampFormulaire champ, string? codeIndique);

    /// <summary>
    /// Le superviseur qualité change le code d'un formulaire, sans changer sa version. Formulaire déjà utilisé : il est
    /// archivé avec ses documents génériques en cours, tels quels (traçabilité) ; une nouvelle ligne (nouveau code, même
    /// version) les remplace, avec les mêmes documents recréés. Formulaire jamais utilisé : son code est simplement corrigé.
    /// Refusé si le code sert à un autre formulaire, ou si ce code et cette version existent déjà.
    /// </summary>
    Task<ChangementCodeFormulaire> ChangerCodeAsync(Guid formulaireId, string nouveauCode, string matricule);

    /// <summary>Familles, postes, machines et natures actifs : listes du champ d'application d'un nouveau formulaire.</summary>
    Task<ListesChampApplication> GetListesChampApplicationAsync();
    /// <summary>Rôles des formulaires (table FormRole) : libellé, ordre, critères permis du champ d'application.</summary>
    Task<List<RoleFormulaire>> GetRolesAsync();

    /// <summary>Documents génériques en cours du formulaire : ceux qu'un changement de code recrée.</summary>
    Task<List<DocumentActifFormulaire>> GetDocumentsActifsAsync(Guid formulaireId);

    /// <summary>Le superviseur qualité crée le formulaire d'un champ d'application (rôle, famille, poste, machine…) avec son code.</summary>
    /// <summary>Crée un code de formulaire avec sa version de départ (celle de Sopal, 0 par défaut).</summary>
    Task<Guid> CreerFormulaireAsync(SopalTrace.Application.Helpers.ChampFormulaire champ, string code, string? designation, int versionDepart = 0);

    /// <summary>Équipes de l'usine (E1, E2, E3…) pour définir les équipes d'un document.</summary>
    Task<List<EquipeReferenceDto>> GetEquipesReferenceAsync();
}

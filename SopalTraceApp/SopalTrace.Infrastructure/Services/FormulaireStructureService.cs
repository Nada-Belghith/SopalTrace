using SopalTrace.Application.Services.Execution;
using SopalTrace.Domain.Exceptions;
using SopalTrace.Domain.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SopalTrace.Application.DTOs.QualityPlans.Referentiels;
using SopalTrace.Application.Interfaces;
using SopalTrace.Domain.Entities;
using SopalTrace.Infrastructure.Data;
using SopalTrace.Application.Helpers;
using Microsoft.EntityFrameworkCore;

namespace SopalTrace.Infrastructure.Services;

public class FormulaireStructureService : IFormulaireStructureService
{
    private readonly IRefFormulaireRepository _formulaireRepository;
    private readonly IUnitOfWork _unitOfWork;
    public FormulaireStructureService(IRefFormulaireRepository formulaireRepository, IUnitOfWork unitOfWork)
    {
        _formulaireRepository = formulaireRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<FormulaireStructureDto?> GetFormulaireByRoleAsync(string role)
    {
        var formulaire = await _formulaireRepository.GetFormulaireActifByRoleAsync(role);

        if (formulaire == null) return null;
        var cols = await _formulaireRepository.GetColonnesActivesByFormulaireIdAsync(formulaire.Id);
        var equipes = await _formulaireRepository.GetEquipesActivesByFormulaireIdAsync(formulaire.Id);

        return new FormulaireStructureDto(
            formulaire.Id,
            formulaire.CodeReference,
            formulaire.Designation,
            ColonneJsonMapper.Serialize(cols, equipes),
            formulaire.Role ?? string.Empty,
            formulaire.Version
        );
    }

    public async Task<FormulaireStructureDto?> GetFormulaireByIdAsync(Guid id)
    {
        var formulaire = await _formulaireRepository.GetByIdAsync(id);

        if (formulaire == null) return null;
        var cols = await _formulaireRepository.GetColonnesActivesByFormulaireIdAsync(formulaire.Id);
        var equipes = await _formulaireRepository.GetEquipesActivesByFormulaireIdAsync(formulaire.Id);

        return new FormulaireStructureDto(
            formulaire.Id,
            formulaire.CodeReference,
            formulaire.Designation,
            ColonneJsonMapper.Serialize(cols, equipes),
            formulaire.Role ?? string.Empty,
            formulaire.Version
        );
    }

    public async Task<FormulaireStructureDto?> GetFormulaireActifParCodeReferenceAsync(string codeReference)
    {
        var formulaire = await _formulaireRepository.GetFormulaireActifByCodeReferenceAsync(codeReference);

        if (formulaire == null) return null;
        var cols = await _formulaireRepository.GetColonnesActivesByFormulaireIdAsync(formulaire.Id);
        var equipes = await _formulaireRepository.GetEquipesActivesByFormulaireIdAsync(formulaire.Id);

        return new FormulaireStructureDto(
            formulaire.Id,
            formulaire.CodeReference,
            formulaire.Designation,
            ColonneJsonMapper.Serialize(cols, equipes),
            formulaire.Role ?? string.Empty,
            formulaire.Version
        );
    }

    public async Task<IEnumerable<FormulaireReferenceItemDto>> GetFormulairesListByRoleAsync(string role)
    {
        var formulaires = await _formulaireRepository.GetFormulairesByRoleAsync(role);
        var dtos = new List<FormulaireReferenceItemDto>();
        foreach (var f in formulaires)
        {
            var cols = await _formulaireRepository.GetColonnesActivesByFormulaireIdAsync(f.Id);
            var equipes = await _formulaireRepository.GetEquipesActivesByFormulaireIdAsync(f.Id);
            dtos.Add(new FormulaireReferenceItemDto(
                f.Id,
                f.CodeReference?.Trim() ?? string.Empty,
                f.Designation?.Trim() ?? string.Empty,
                f.Role?.Trim() ?? string.Empty,
                f.Version,
                ColonneJsonMapper.Serialize(cols, equipes),
                f.Statut?.Trim() ?? string.Empty,
                f.PosteCode,
                f.FamilleProduitFiniCode,
                f.MachineCode,
                f.NatureArticleCode,
                f.TypeRobinetCode
            ));
        }
        return dtos;
    }

    /// <summary>
    /// Synchronise la structure d'un Ref_Formulaire (colonnes extra) en utilisant
    /// AsNoTracking + ExecuteUpdateAsync pour éviter tout conflit de change tracking EF Core.
    /// </summary>
    public async Task<(Guid Id, int Version)?> UpdateFormulaireStructureAsync(
        string role,
        string? configurationStructureJson,
        string? codeReference = null,
        bool isCorrectionMineure = false,
        bool forceNouvelleVersion = false)
    {
        // ── Chargement SANS tracking pour éviter les conflits EF Core ──
        var codeRefTrimmed = codeReference?.Trim();
        var roleTrimmed = role?.Trim();

        RefFormulaire? formulaireActuel = null;
        if (!string.IsNullOrWhiteSpace(codeRefTrimmed))
        {
            formulaireActuel = await _formulaireRepository.GetFormulaireActifByCodeReferenceAsync(codeRefTrimmed);
        }
        else
        {
            formulaireActuel = await _formulaireRepository.GetFormulaireActifByRoleAsync(roleTrimmed ?? string.Empty);
        }

        var parsedRoot = ColonneJsonMapper.DeserializeRoot(configurationStructureJson);
        await ValiderEquipesAsync(parsedRoot.Equipes);

        if (formulaireActuel != null)
        {
            bool forceArchive = forceNouvelleVersion;

            if (formulaireActuel.Statut?.Trim() == StatutsPlan.Brouillon && !forceArchive)
            {
                // ── Cas BROUILLON → ACTIF ──
                await _formulaireRepository.UpdateStatutAsync(formulaireActuel.Id, StatutsPlan.Actif);

                // Synchronisation des colonnes directement en DB
                await _formulaireRepository.SyncColonnesAsync(formulaireActuel.Id, parsedRoot.CustomCols);
                await _formulaireRepository.SyncEquipesAsync(formulaireActuel.Id, parsedRoot.Equipes);

                return (formulaireActuel.Id, formulaireActuel.Version);
            }
            else if (isCorrectionMineure)
            {
                // ── Cas CORRECTION MINEURE ──
                // Les documents déjà créés sur cette version gardent leur structure et leur modèle de rapport : changer
                // les colonnes (ajout, retrait, libellé, type, valeurs proposées) impose une nouvelle version.
                var colonnesActuelles = await _formulaireRepository.GetColonnesActivesByFormulaireIdAsync(formulaireActuel.Id);
                if (ColonneJsonMapper.SignatureColonnes(colonnesActuelles) != ColonneJsonMapper.SignatureColonnes(parsedRoot.CustomCols))
                    throw new RegleMetierException(MessageColonnesModifiees);

                // Colonnes inchangées : seules les équipes peuvent changer, sans toucher à la version ni au statut
                await _formulaireRepository.SyncColonnesAsync(formulaireActuel.Id, parsedRoot.CustomCols);
                await _formulaireRepository.SyncEquipesAsync(formulaireActuel.Id, parsedRoot.Equipes);

                return (formulaireActuel.Id, formulaireActuel.Version);
            }
            else
            {
                // ── Check if structure actually changed before archiving ──
                if (!forceArchive)
                {
                    var colsActuelles = await _formulaireRepository.GetColonnesActivesByFormulaireIdAsync(formulaireActuel.Id);
                    var equipesActuelles = await _formulaireRepository.GetEquipesActivesByFormulaireIdAsync(formulaireActuel.Id);
                    var structureActuelleJson = ColonneJsonMapper.Serialize(colsActuelles, equipesActuelles);

                    var newStructureJson = (!parsedRoot.Equipes.Any() && !parsedRoot.CustomCols.Any())
                        ? null
                        : (!parsedRoot.Equipes.Any())
                            ? System.Text.Json.JsonSerializer.Serialize(parsedRoot.CustomCols, new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase })
                            : System.Text.Json.JsonSerializer.Serialize(parsedRoot, new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });

                    if (string.Equals(structureActuelleJson, newStructureJson, StringComparison.OrdinalIgnoreCase))
                    {
                        // No structural changes, keep the current ACTIF form
                        return (formulaireActuel.Id, formulaireActuel.Version);
                    }
                }

                // ── Cas ACTIF → ARCHIVE + nouvelle version ──
                // Colonnes changées : nouveau modèle obligatoire (demande à l'administrateur) ; sinon le modèle est repris.
                var structureChangee = ColonneJsonMapper.SignatureColonnes(await _formulaireRepository.GetColonnesActivesByFormulaireIdAsync(formulaireActuel.Id))
                    != ColonneJsonMapper.SignatureColonnes(parsedRoot.CustomCols);

                // Les documents liés d'abord, puis le formulaire : la base refuse un document générique actif sur un
                // formulaire archivé.

                // Auto-archive the linked generic documents
                var linkedDocs = await _unitOfWork.DocumentEnteteRepository.GetByFormulaireIdAsync(formulaireActuel.Id);
                foreach(var doc in linkedDocs)
                {
                    if (doc.Statut != StatutsPlan.Archive)
                    {
                        doc.Statut = StatutsPlan.Archive;
                        await _unitOfWork.DocumentEnteteRepository.UpdateAsync(doc);
                    }
                }

                // Auto-archive the linked ModeleFabrication documents
                var linkedModels = await _unitOfWork.ModeleFabricationEnteteRepository.GetByFormulaireIdAsync(formulaireActuel.Id);
                foreach(var model in linkedModels)
                {
                    if (model.Statut != StatutsPlan.Archive)
                    {
                        model.Statut = StatutsPlan.Archive;
                        await _unitOfWork.ModeleFabricationEnteteRepository.UpdateAsync(model);
                    }
                }

                // Auto-archive the linked PlanFabrication documents
                var linkedPlans = await _unitOfWork.PlanFabricationEnteteRepository.GetByFormulaireIdAsync(formulaireActuel.Id);
                foreach(var plan in linkedPlans)
                {
                    if (plan.Statut != StatutsPlan.Archive)
                    {
                        plan.Statut = StatutsPlan.Archive;
                        await _unitOfWork.PlanFabricationEnteteRepository.UpdateAsync(plan);
                    }
                }

                // Auto-archive the linked DocumentVerifMachine documents
                var linkedVmDocs = await _unitOfWork.DocumentVerifMachineEnteteRepository.GetByFormulaireIdAsync(formulaireActuel.Id);
                foreach(var doc in linkedVmDocs)
                {
                    if (doc.Statut != StatutsPlan.Archive)
                    {
                        doc.Statut = StatutsPlan.Archive;
                        await _unitOfWork.DocumentVerifMachineEnteteRepository.UpdateAsync(doc);
                    }
                }
                await _unitOfWork.CommitAsync();
                await _formulaireRepository.UpdateStatutAsync(formulaireActuel.Id, StatutsPlan.Archive);
                var maxVersion = await _formulaireRepository.GetMaxVersionDuGroupeAsync(formulaireActuel.VersionGroupId);

                var newVersion = maxVersion + 1;

                // Créer le nouveau formulaire (entité pure, sans colonnes pour l'instant)
                var nouveauFormulaire = new RefFormulaire
                {
                    Id = Guid.NewGuid(),
                    CodeReference = formulaireActuel.CodeReference,
                    Designation = formulaireActuel.Designation,
                    Version = newVersion,
                    Statut = StatutsPlan.Actif,
                    CreeLe = DateTime.UtcNow,
                    Role = role,
                    // Une nouvelle version reste dans le groupe de versions du formulaire et garde son champ d'application.
                    VersionGroupId = formulaireActuel.VersionGroupId,
                    PosteCode = formulaireActuel.PosteCode,
                    FamilleProduitFiniCode = formulaireActuel.FamilleProduitFiniCode,
                    TypeRobinetCode = formulaireActuel.TypeRobinetCode,
                    MachineCode = formulaireActuel.MachineCode,
                    NatureArticleCode = formulaireActuel.NatureArticleCode
                };

                await _formulaireRepository.AddAsync(nouveauFormulaire);
                await _unitOfWork.CommitAsync();

                // Ajouter les colonnes au nouveau formulaire
                await _formulaireRepository.SyncColonnesAsync(nouveauFormulaire.Id, parsedRoot.CustomCols);
                await _formulaireRepository.SyncEquipesAsync(nouveauFormulaire.Id, parsedRoot.Equipes);

                // Relation « code + version → modèle » écrite avant tout document de la nouvelle version.
                if (structureChangee)
                {
                    await _formulaireRepository.AnnulerDemandesModeleAsync(formulaireActuel.Id);
                    await _formulaireRepository.DemanderModeleAsync(nouveauFormulaire.Id, DemandesModele.NouvelleStructure,
                        $"Colonnes ajoutées ou retirées : {nouveauFormulaire.CodeReference} version {newVersion}.");
                }
                else
                {
                    await _formulaireRepository.DeplacerDemandesModeleAsync(formulaireActuel.Id, nouveauFormulaire.Id);
                    await _formulaireRepository.RecopierModeleAsync(formulaireActuel.Id, nouveauFormulaire.Id,
                        $"Repris de {formulaireActuel.CodeReference}.{formulaireActuel.Version} (même structure)");
                }

                return (nouveauFormulaire.Id, newVersion);
            }
        }
        else
        {
            // ── Aucun formulaire existant : toute première création ──
            var code = !string.IsNullOrWhiteSpace(codeRefTrimmed) ? codeRefTrimmed : (roleTrimmed ?? role ?? string.Empty);
            var nouveauFormulaire = new RefFormulaire
            {
                Id = Guid.NewGuid(),
                CodeReference = code,
                Designation = $"Formulaire {code}",
                Version = 0,
                Statut = StatutsPlan.Brouillon,
                CreeLe = DateTime.UtcNow,
                Role = role
            };

            await _formulaireRepository.AddAsync(nouveauFormulaire);
            await _unitOfWork.CommitAsync();

            await _formulaireRepository.SyncColonnesAsync(nouveauFormulaire.Id, parsedRoot.CustomCols);
            await _formulaireRepository.SyncEquipesAsync(nouveauFormulaire.Id, parsedRoot.Equipes);
            await _formulaireRepository.DemanderModeleAsync(nouveauFormulaire.Id, DemandesModele.NouveauFormulaire, null);

            return (nouveauFormulaire.Id, 0);
        }
    }

    public const string MessageColonnesModifiees =
        "La configuration des colonnes a changé : une correction mineure ne peut pas modifier la structure du document. " +
        "Enregistrez une nouvelle version (les documents déjà créés gardent leur structure et leur modèle).";

    public async Task<List<EquipeReferenceDto>> GetEquipesReferenceAsync() =>
        (await _formulaireRepository.GetEquipesReferenceAsync()).Select(t => new EquipeReferenceDto(t.Code, t.Label)).ToList();

    /// <summary>
    /// Équipes d'un document (contrôle au poste) : chacune rattachée à une équipe connue, horaires valides,
    /// aucune heure couverte par deux équipes (chaque tranche horaire n'a qu'une équipe qui la saisit).
    /// </summary>
    private async Task ValiderEquipesAsync(List<EquipeJsonDto> equipes)
    {
        var configurees = equipes.Where(e => !string.IsNullOrWhiteSpace(e.Nom)).ToList();
        if (configurees.Count == 0) return;
        var codes = (await _formulaireRepository.GetEquipesReferenceAsync()).Select(t => t.Code).ToList();
        var erreur = EquipesControlePoste.Erreur(
            configurees.Select((e, i) => new EquipePoste(
                string.IsNullOrWhiteSpace(e.Code) ? Equipes.PourPosition(i) : e.Code.Trim().ToUpperInvariant(), e.Nom, e.Debut, e.Fin, i)).ToList(),
            codes);
        if (erreur != null) throw new RegleMetierException(erreur);
    }

    public async Task<string> CodeFormulaireAsync(ChampFormulaire champ, string? codeIndique)
    {
        var code = codeIndique?.Trim();
        if (string.IsNullOrEmpty(code))
        {
            var formulaire = await _formulaireRepository.GetFormulaireEnCoursParChampAsync(champ)
                ?? throw new RegleMetierException(
                    $"Aucun formulaire pour {champ} : le superviseur qualité doit d'abord le créer en indiquant son code.");
            return formulaire.CodeReference;
        }

        if (await _formulaireRepository.GetFormulaireActifByCodeReferenceAsync(code) != null) return code;

        // Nouveau formulaire : son code est celui indiqué, son champ d'application celui du document.
        champ = await ChampVerifieAsync(champ);
        if (await _formulaireRepository.GetFormulaireEnCoursParChampAsync(champ) is { } existant)
            throw new RegleMetierException(
                $"Le formulaire {existant.CodeReference} existe déjà pour {champ} : choisissez-le, ou renommez son code.");
        if (code.Length > 30) throw new RegleMetierException("Le code d'un formulaire ne dépasse pas 30 caractères.");
        await _formulaireRepository.AddAsync(new RefFormulaire
        {
            Id = Guid.NewGuid(),
            CodeReference = code,
            Designation = $"Formulaire {code}",
            Version = 0,
            Statut = StatutsPlan.Brouillon,
            CreeLe = DateTime.Now,
            Role = champ.Role,
            FamilleProduitFiniCode = champ.Famille,
            PosteCode = champ.Poste,
            MachineCode = champ.Machine,
            NatureArticleCode = champ.Nature,
            TypeRobinetCode = champ.TypeRobinet
        });
        await _unitOfWork.CommitAsync();
        return code;
    }

    public async Task<ChangementCodeFormulaire> ChangerCodeAsync(Guid formulaireId, string nouveauCode, string matricule)
    {
        var code = VerifierCode(nouveauCode);
        var formulaire = await _formulaireRepository.GetByIdAsync(formulaireId)
            ?? throw new RessourceIntrouvableException("Formulaire introuvable.");
        if (formulaire.Statut == StatutsPlan.Archive)
            throw new RegleMetierException("Un formulaire archivé garde son code : changez le code du formulaire en cours.");
        if (formulaire.CodeReference == code) return new ChangementCodeFormulaire(formulaire.Id, code, formulaire.Version, 0);
        // Un code reste réservé à son formulaire, même s'il n'est plus porté que par des lignes archivées.
        if (await _formulaireRepository.CodeUtiliseParUnAutreFormulaireAsync(code, formulaire.VersionGroupId))
            throw new RegleMetierException($"Le code {code} est déjà celui d'un autre formulaire.");
        if (await _formulaireRepository.CodeVersionExisteAsync(code, formulaire.Version))
            throw new RegleMetierException($"Le formulaire {code} version {formulaire.Version} existe déjà (archivé) : choisissez un autre code.");

        if (!await _formulaireRepository.EstUtiliseAsync(formulaire.Id))
        {
            await _formulaireRepository.ChangerCodeAsync(formulaire.Id, formulaire.CodeReference, code);
            return new ChangementCodeFormulaire(formulaire.Id, code, formulaire.Version, 0);
        }
        var (nouveauId, recrees) = await _formulaireRepository.RemplacerParNouveauCodeAsync(formulaire.Id, code, matricule);
        return new ChangementCodeFormulaire(nouveauId, code, formulaire.Version, recrees);
    }

    public Task<ListesChampApplication> GetListesChampApplicationAsync() => _formulaireRepository.GetListesChampApplicationAsync();

    public Task<List<RoleFormulaire>> GetRolesAsync() => _formulaireRepository.GetRolesAsync();

    /// <summary>
    /// Champ d'application d'un nouveau formulaire, vérifié selon son rôle (mêmes règles que la base) : avec une famille,
    /// le type de robinet est celui de la famille ; nature et type de robinet ne désignent un formulaire que sans famille.
    /// </summary>
    private async Task<ChampFormulaire> ChampVerifieAsync(ChampFormulaire champ)
    {
        var role = (await _formulaireRepository.GetRolesAsync()).FirstOrDefault(r => r.Code == champ.Role)
            ?? throw new RegleMetierException($"Rôle de formulaire inconnu : {champ.Role}.");
        if (champ.Famille != null)
        {
            if (champ.Nature != null)
                throw new RegleMetierException("La nature d'article ne désigne un formulaire que sans famille (ex. PISTON).");
            champ = champ with
            {
                TypeRobinet = await _formulaireRepository.GetTypeRobinetDeFamilleAsync(champ.Famille)
                    ?? throw new RegleMetierException($"Famille inconnue : {champ.Famille}.")
            };
        }

        var refuses = new[]
        {
            champ.Famille != null && !role.ParFamille ? "famille" : null,
            champ.Poste != null && !role.ParPoste ? "poste" : null,
            champ.Machine != null && !role.ParMachine ? "machine" : null,
            champ.Nature != null && !role.ParNature ? "nature d'article" : null,
            champ.Famille == null && champ.TypeRobinet != null && !role.ParTypeRobinet ? "type de robinet" : null
        }.Where(c => c != null).ToList();
        if (refuses.Count > 0)
            throw new RegleMetierException($"Un formulaire « {role.Libelle} » ne se désigne pas par : {string.Join(", ", refuses)}.");
        return champ;
    }

    public async Task<List<DocumentActifFormulaire>> GetDocumentsActifsAsync(Guid formulaireId)
    {
        _ = await _formulaireRepository.GetByIdAsync(formulaireId)
            ?? throw new RessourceIntrouvableException("Formulaire introuvable.");
        return await _formulaireRepository.GetDocumentsActifsAsync(formulaireId);
    }

    public const int VersionDepartMaximale = 999;

    public async Task<Guid> CreerFormulaireAsync(ChampFormulaire champ, string code, string? designation, int versionDepart = 0)
    {
        code = VerifierCode(code);
        if (versionDepart < 0 || versionDepart > VersionDepartMaximale)
            throw new RegleMetierException($"La version de départ d'un formulaire est comprise entre 0 et {VersionDepartMaximale}.");
        if (await _formulaireRepository.CodeExisteAsync(code))
            throw new RegleMetierException($"Le code {code} est déjà celui d'un autre formulaire.");
        champ = await ChampVerifieAsync(champ);
        if (await _formulaireRepository.GetFormulaireEnCoursParChampAsync(champ) is { } existant)
            throw new RegleMetierException($"Le formulaire {existant.CodeReference} existe déjà pour {champ}.");
        var formulaire = new RefFormulaire
        {
            Id = Guid.NewGuid(),
            CodeReference = code,
            Designation = string.IsNullOrWhiteSpace(designation) ? $"Formulaire {code}" : designation.Trim(),
            Version = versionDepart,
            Statut = StatutsPlan.Brouillon,
            CreeLe = DateTime.Now,
            Role = champ.Role,
            FamilleProduitFiniCode = champ.Famille,
            PosteCode = champ.Poste,
            MachineCode = champ.Machine,
            NatureArticleCode = champ.Nature,
            TypeRobinetCode = champ.TypeRobinet
        };
        await _formulaireRepository.AddAsync(formulaire);
        await _unitOfWork.CommitAsync();
        await _formulaireRepository.DemanderModeleAsync(formulaire.Id, DemandesModele.NouveauFormulaire, null);
        return formulaire.Id;
    }

    private static string VerifierCode(string? code)
    {
        code = code?.Trim().ToUpperInvariant() ?? "";
        if (code.Length == 0 || code.Length > 30 || !System.Text.RegularExpressions.Regex.IsMatch(code, "^[A-Z0-9][A-Z0-9_.\\-]*$"))
            throw new RegleMetierException("Le code d'un formulaire : 1 à 30 lettres, chiffres, points, tirets ou soulignés (ex. FE0594).");
        return code;
    }

    public async Task<bool> ActiverFormulaireAsync(Guid id)
    {
        await _formulaireRepository.UpdateStatutAsync(id, StatutsPlan.Actif);
        return true;
    }
}

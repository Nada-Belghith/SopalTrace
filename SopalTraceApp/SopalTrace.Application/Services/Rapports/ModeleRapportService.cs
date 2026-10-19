using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SopalTrace.Application.DTOs.Rapports;
using SopalTrace.Application.Interfaces;
using SopalTrace.Application.Interfaces.Rapports;
using SopalTrace.Application.Rapports;
using SopalTrace.Application.Rapports.Execution;
using SopalTrace.Application.Settings;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;
using SopalTrace.Domain.Exceptions;

namespace SopalTrace.Application.Services.Rapports;

public interface IModeleRapportService
{
    Task<List<ModeleRapportDto>> GetModelesAsync();
    /// <summary>Versions de formulaire non archivées du type du modèle, leurs colonnes et le modèle de leurs prochains documents.</summary>
    Task<List<FormulaireModeleDto>> GetFormulairesAsync(string codeModele);
    /// <summary>Tous les formulaires en cours, par rôle, avec le modèle de leurs prochains documents et les modèles du rôle.</summary>
    Task<List<FormulairesDuRoleDto>> GetFormulairesParRoleAsync();
    /// <summary>Champs utilisables dans ce modèle : contrat du type et colonnes propres (de la version de formulaire visée, ou de toutes).</summary>
    Task<List<ChampDisponibleDto>> GetChampsAsync(string codeModele, Guid? formulaireId);
    /// <summary>
    /// Vérifie le fichier (contre les colonnes de la version de formulaire visée) et, s'il est accepté, l'enregistre comme
    /// nouvelle version (brouillon) du modèle dédié à ce formulaire. Un type sans formulaire (modèle par défaut) n'en vise
    /// aucun.
    /// </summary>
    Task<VerificationModeleDto> EnvoyerVersionAsync(string codeModele, string nomFichier, byte[] contenu, string? commentaire, Guid? formulaireId);
    /// <summary>
    /// PDF d'essai de la version : le modèle vide avec l'identité du formulaire visé (code, version), jamais les données
    /// d'un document existant.
    /// </summary>
    Task<FichierDto> ApercuAsync(Guid versionId, Guid? formulaireId);
    /// <summary>La version devient le modèle par défaut de son type, pour les documents créés à partir de maintenant.</summary>
    Task<PublicationModeleDto> PublierParDefautAsync(Guid versionId, string? commentaire);
    /// <summary>
    /// Modèle conçu pour une version de formulaire (nouvelle structure) : il met en forme les documents de cette version
    /// de formulaire, y compris ceux déjà créés qui n'avaient pas de modèle propre.
    /// </summary>
    Task<PublicationModeleDto> AffecterAuFormulaireAsync(Guid versionId, Guid formulaireId, string? commentaire);
    /// <summary>
    /// Révision (logo…) de la version remplacée : chaque formulaire non archivé dont les prochains documents utilisent la
    /// version remplacée reçoit la révision, et elle devient le modèle par défaut si la version remplacée l'était.
    /// Les documents déjà créés gardent leur modèle.
    /// </summary>
    Task<PublicationModeleDto> ReviserAsync(Guid versionId, Guid versionRemplaceeId, string? commentaire);
    /// <summary>
    /// Nouveau logo (ou nouvelle mise en page) pour les formulaires choisis, tout ou rien : chaque formulaire déjà utilisé
    /// passe à sa version suivante avec ce modèle (l'ancienne version et ses documents génériques sont archivés tels quels,
    /// les documents recréés en version suivante) ; un formulaire jamais utilisé reçoit simplement ce modèle.
    /// </summary>
    Task<PublicationModeleDto> ChangerLogoAsync(Guid versionId, IReadOnlyCollection<Guid> formulaireIds, string? commentaire);
    Task<FichierDto> TelechargerAsync(Guid versionId);
    /// <summary>Demandes de modèle (toutes, ou d'un état : PENDING, DONE, CANCELLED), les plus récentes d'abord.</summary>
    Task<List<DemandeModeleDto>> GetDemandesAsync(string? statut);
    /// <summary>
    /// Superviseur qualité : demande un nouveau modèle pour une ligne de formulaire (logo, mise en page…), avec son choix :
    /// même version (le nouveau modèle vaut pour les documents créés après) ou version suivante.
    /// </summary>
    Task<long> DemanderModeleAsync(Guid formulaireId, string? motif, string? changementVersion);
    /// <summary>Superviseur qualité : annule une demande encore en attente.</summary>
    Task AnnulerDemandeAsync(long demandeId);
    /// <summary>
    /// Administrateur : rattache la version (dédiée au formulaire, vérifiée) qui satisfait la demande, en appliquant tel
    /// quel le choix du superviseur ; la demande devient traitée, avec le rattachement qui l'a satisfaite. Tout ou rien.
    /// </summary>
    Task<PublicationModeleDto> TraiterDemandeAsync(long demandeId, Guid versionId, string? commentaire);
    /// <summary>
    /// Tests : importe le fichier de chaque type sans formulaire qui n'a encore aucune version (modèle par défaut), et le
    /// modèle dédié de chaque formulaire de départ qui n'en a pas encore.
    /// </summary>
    Task ImporterModelesLivresAsync();
}

/// <summary>
/// Modèles de rapport : un fichier Excel envoyé n'est enregistré que s'il passe la vérification (structure et champs
/// du contrat) ; il reste brouillon (aperçu possible) jusqu'à sa publication par une affectation. Le contenu d'une
/// version et l'historique des affectations ne changent jamais (garanti par la base).
/// </summary>
public class ModeleRapportService : IModeleRapportService
{
    private const string TypeExcel = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private const int LongueurMaximaleCommentaire = 500;
    private const int LongueurMaximaleNomFichier = 200;

    private readonly IModeleRapportRepository _modeles;
    private readonly IDocumentGenereRepository _documents;
    private readonly IMoteurModeleExcel _moteur;
    private readonly IConvertisseurPdf _convertisseur;
    private readonly IModelesLivres _modelesLivres;
    private readonly IEnumerable<IDefinitionRapport> _definitions;
    private readonly IEnumerable<IDefinitionRapportExecution> _definitionsExecution;
    private readonly ICurrentUserService _utilisateur;
    private readonly RapportsSettings _parametres;
    private readonly ILogger<ModeleRapportService> _logger;

    public ModeleRapportService(IModeleRapportRepository modeles, IDocumentGenereRepository documents, IMoteurModeleExcel moteur,
        IConvertisseurPdf convertisseur, IModelesLivres modelesLivres,
        IEnumerable<IDefinitionRapport> definitions, IEnumerable<IDefinitionRapportExecution> definitionsExecution,
        ICurrentUserService utilisateur, RapportsSettings parametres,
        ILogger<ModeleRapportService> logger)
    {
        _modeles = modeles;
        _documents = documents;
        _moteur = moteur;
        _convertisseur = convertisseur;
        _modelesLivres = modelesLivres;
        _definitions = definitions;
        _definitionsExecution = definitionsExecution;
        _utilisateur = utilisateur;
        _parametres = parametres;
        _logger = logger;
    }

    public async Task<List<ModeleRapportDto>> GetModelesAsync()
    {
        var modeles = await _modeles.GetModelesAsync();
        var codes = modeles.Select(m => m.Code).ToList();
        var versions = await _modeles.GetResumesVersionsAsync(codes);
        var affectations = await _modeles.GetAffectationsAsync(codes);
        var resultat = new List<ModeleRapportDto>();
        foreach (var modele in modeles.OrderBy(m => m.Name))
        {
            var etats = await EtatFormulairesAsync(modele, modeles);
            resultat.Add(new ModeleRapportDto
            {
                Code = modele.Code,
                Nom = modele.Name,
                TypeDocumentCode = modele.DocumentTypeCode ?? "",
                TypeExecutionCode = modele.ExecutionDocumentTypeCode ?? "",
                FormRole = modele.FormRole,
                VersionParDefaut = modele.FormRole == null ? ParDefautActuelle(affectations, modele.Code)?.NumeroVersion : null,
                Versions = versions.Where(v => v.CodeModele == modele.Code).ToList(),
                Affectations = await _modeles.GetAffectationsDetailleesAsync(modele.Code),
                Formulaires = etats.Where(e => e.Resolution.Modele?.Affectation.CodeModele == modele.Code)
                    .Select(e => $"{e.Formulaire.CodeFormulaire} version {e.Formulaire.Version}").ToList(),
                FormulairesSansModele = etats.Where(e => e.Resolution.Modele == null)
                    .Select(e => $"{e.Formulaire.CodeFormulaire} version {e.Formulaire.Version}").ToList()
            });
        }
        return resultat;
    }

    public async Task<List<FormulaireModeleDto>> GetFormulairesAsync(string codeModele)
    {
        var modele = await ModeleAsync(codeModele);
        return VersDtos(await EtatFormulairesAsync(modele, await _modeles.GetModelesAsync()));
    }

    public async Task<List<FormulairesDuRoleDto>> GetFormulairesParRoleAsync()
    {
        var modeles = await _modeles.GetModelesAsync();
        var roles = await _modeles.GetRolesFormulaireAsync();
        var libelles = roles.ToDictionary(r => r.Code, r => r.Libelle);
        var natures = roles.ToDictionary(r => r.Code, r => r.Nature);
        var enAttente = (await _modeles.GetDemandesAsync(DemandesModele.EnAttente)).ToDictionary(d => d.FormulaireId);
        var noms = (await _modeles.GetResumesVersionsAsync(modeles.Select(m => m.Code).ToList())).ToDictionary(v => v.Id, v => v.Nom);
        var resultat = new List<FormulairesDuRoleDto>();
        foreach (var role in roles.Select(r => r.Code))
        {
            // Les modèles d'un rôle mettent en forme le même type de document : l'état se lit par n'importe lequel d'entre eux.
            var duRole = modeles.Where(m => m.FormRole == role).OrderBy(m => m.Name).ToList();
            var etats = duRole.Count > 0
                ? await EtatFormulairesAsync(duRole[0], modeles)
                : (await _modeles.GetColonnesFormulairesAsync(role)).Select(f => (f, ResolutionModele.Absent(ModeleManquant.AucunModele))).ToList();
            var formulaires = VersDtos(etats);
            foreach (var f in formulaires)
            {
                f.DemandeEnAttente = enAttente.GetValueOrDefault(f.Id);
                if (f.ModeleActuel != null) f.ModeleActuel.NomModele = noms.GetValueOrDefault(f.ModeleActuel.VersionId);
            }
            resultat.Add(new FormulairesDuRoleDto
            {
                Role = role,
                Libelle = libelles[role],
                Nature = natures[role],
                Modeles = duRole.Select(m => new ModeleDuRoleDto(m.Code, m.Name)).ToList(),
                Formulaires = formulaires
            });
        }
        return resultat;
    }

    private static List<FormulaireModeleDto> VersDtos(IEnumerable<(ColonnesFormulaire Formulaire, ResolutionModele Resolution)> etats) =>
        etats
            .OrderBy(e => e.Formulaire.CodeFormulaire).ThenByDescending(e => e.Formulaire.Version)
            .Select(e => new FormulaireModeleDto
            {
                Id = e.Formulaire.Id,
                GroupeId = e.Formulaire.GroupeId,
                CodeReference = e.Formulaire.CodeFormulaire,
                Version = e.Formulaire.Version,
                Statut = e.Formulaire.Statut,
                Role = e.Formulaire.Role,
                ChampApplication = e.Formulaire.ChampApplication,
                Colonnes = e.Formulaire.LibellesParCle.OrderBy(c => c.Key, StringComparer.Ordinal)
                    .Select(c => new ColonneFormulaireDto { Cle = c.Key, Libelle = c.Value }).ToList(),
                ModeleActuel = ModelesDocuments.VersDto(e.Resolution.Modele),
                ModeleManquant = e.Resolution.Manque is { } manque ? ModelesDocuments.Code(manque) : null,
                MessageModeleManquant = e.Resolution.Manque is { } absence
                    ? ModelesDocuments.Message(absence, $"{e.Formulaire.CodeFormulaire} version {e.Formulaire.Version}") : null
            })
            .ToList();

    public async Task<List<ChampDisponibleDto>> GetChampsAsync(string codeModele, Guid? formulaireId)
    {
        var modele = await ModeleAsync(codeModele);
        var champs = Contrat(modele).Champs
            .Select(c => new ChampDisponibleDto { Nom = c.Nom, Portee = Portee(c.Portee), Description = c.Description, Exemple = c.Exemple })
            .ToList();

        var formulaires = await FormulairesVerifiesAsync(modele, formulaireId);
        foreach (var colonne in formulaires
                     .SelectMany(f => f.LibellesParCle.Select(c => (Cle: c.Key, Libelle: c.Value, Formulaire: $"{f.CodeFormulaire} v{f.Version}")))
                     .GroupBy(c => c.Cle, StringComparer.Ordinal)
                     .OrderBy(g => g.Key, StringComparer.Ordinal))
        {
            champs.Add(new ChampDisponibleDto
            {
                Nom = NomsChamps.Colonne(colonne.Key),
                Portee = Portee(PorteeChamp.ColonneLigne),
                Description = $"Colonne « {colonne.First().Libelle} » ({string.Join(", ", colonne.Select(c => c.Formulaire).Distinct())})",
                Exemple = ""
            });
        }
        return champs;
    }

    public async Task<VerificationModeleDto> EnvoyerVersionAsync(string codeModele, string nomFichier, byte[] contenu, string? commentaire, Guid? formulaireId)
    {
        var modele = await ModeleAsync(codeModele);
        var refus = VerifierFichier(nomFichier, contenu) ?? VerifierCommentaire(commentaire)
            ?? (modele.FormRole != null && formulaireId == null ? "Choisissez le formulaire auquel ce modèle est dédié." : null);
        if (refus != null) return new VerificationModeleDto { Erreurs = { refus } };

        var verification = await VerifierAsync(modele, contenu, formulaireId);
        if (!verification.Accepte) return verification;

        // Modèle dédié au formulaire visé (aucun pour un modèle par défaut).
        var formulaire = formulaireId is { } id ? await FormulaireDuModeleAsync(modele, id) : null;
        var groupe = formulaire?.GroupeId;

        // Fichier déjà envoyé pour ce formulaire : sa version est reprise telle quelle (aucun doublon).
        if (await _modeles.GetVersionIdentiqueAsync(modele.Code, groupe, Empreinte(contenu)) is { } identique)
        {
            verification.Version = await _modeles.GetVersionResumeAsync(identique);
            verification.Avertissements.Insert(0, $"Ce fichier est déjà la version {verification.Version!.Numero} de ce modèle : elle est reprise, aucune nouvelle version n'est créée.");
            return verification;
        }

        var numero = await _modeles.GetDernierNumeroAsync(modele.Code, groupe) + 1;
        var version = new ReportTemplateVersion
        {
            Id = Guid.NewGuid(),
            ReportTemplateCode = modele.Code,
            FormGroupId = groupe,
            Name = await NomModeleAsync(modele, formulaire, numero),
            VersionNumber = numero,
            FileName = Tronquer(Path.GetFileName(nomFichier), LongueurMaximaleNomFichier),
            Content = contenu,
            Status = StatutsVersionModele.Brouillon,
            ValidationWarnings = Avertissements(verification.Avertissements),
            Comment = Nettoyer(commentaire),
            UploadedBy = _utilisateur.MatriculeRequis,
            UploadedAt = DateTime.Now
        };
        _modeles.Ajouter(version);
        await _modeles.SaveChangesAsync();

        verification.Version = await _modeles.GetVersionResumeAsync(version.Id);
        return verification;
    }

    public async Task<FichierDto> ApercuAsync(Guid versionId, Guid? formulaireId)
    {
        var version = await _modeles.GetVersionLectureAsync(versionId) ?? throw new RessourceIntrouvableException("Version de modèle introuvable.");
        var modele = await ModeleAsync(version.ReportTemplateCode);
        var formulaire = formulaireId is { } id ? await FormulaireDuModeleAsync(modele, id) : null;
        var designation = formulaire == null ? null : await _modeles.GetDesignationFormulaireAsync(formulaire.Id);
        var donnees = DonneesApercu.Construire(Contrat(modele), formulaire, designation);

        var pdf = await _convertisseur.ConvertirAsync(_moteur.Remplir(version.Content, donnees));
        return new FichierDto(pdf, "application/pdf", $"Apercu_{modele.Code}_V{version.VersionNumber}.pdf");
    }

    public async Task<PublicationModeleDto> PublierParDefautAsync(Guid versionId, string? commentaire)
    {
        var (version, modele) = await VersionAPublierAsync(versionId, commentaire);
        if (modele.FormRole != null)
            throw new RegleMetierException("Ce modèle met en forme les documents de formulaires : rattachez-le à chaque formulaire concerné.");
        var affectations = await _modeles.GetAffectationsAsync(new[] { modele.Code });
        if (ParDefautActuelle(affectations, modele.Code)?.VersionId == version.Id)
            throw new RegleMetierException($"La version {version.VersionNumber} est déjà le modèle par défaut.");

        // Les formulaires ont pu changer depuis l'envoi (colonne retirée) : le fichier est vérifié à nouveau.
        var verification = await VerifierAsync(modele, version.Content, null);
        ExigerAccepte(verification);
        await PublierAsync(version, verification, new[] { NouvelleAffectation(version, TypesAffectationModele.ParDefaut, null, commentaire) });
        return new PublicationModeleDto { Version = (await _modeles.GetVersionResumeAsync(version.Id))!, ParDefaut = true };
    }

    public async Task<PublicationModeleDto> AffecterAuFormulaireAsync(Guid versionId, Guid formulaireId, string? commentaire)
    {
        var (version, modele) = await VersionAPublierAsync(versionId, commentaire);
        var formulaire = await FormulaireDuModeleAsync(modele, formulaireId);
        ExigerDedie(version, formulaire);
        var demande = await _modeles.GetDemandeEnAttenteAsync(formulaireId);
        if (demande?.Origin == DemandesModele.Superviseur)
            throw new RegleMetierException($"Le superviseur qualité a demandé un nouveau modèle pour {formulaire.CodeFormulaire} version {formulaire.Version} : "
                + "traitez sa demande (son choix de version s'applique).");
        await ExigerPasDejaEnPlaceAsync(modele, version, formulaire);

        var verification = VerificateurModele.Verifier(_moteur.Analyser(version.Content), Contrat(modele), new[] { formulaire });
        ExigerAccepte(verification);
        await PublierAsync(version, verification,
            () => Task.FromResult<IReadOnlyCollection<ReportTemplateAssignment>>(new[] { NouvelleAffectation(version, TypesAffectationModele.StructureFormulaire, formulaireId, commentaire) }),
            affectations => MarquerTraiteeAsync(demande, affectations));
        return new PublicationModeleDto
        {
            Version = (await _modeles.GetVersionResumeAsync(version.Id))!,
            Formulaires = { $"{formulaire.CodeFormulaire} version {formulaire.Version}" }
        };
    }

    public async Task<PublicationModeleDto> ChangerLogoAsync(Guid versionId, IReadOnlyCollection<Guid> formulaireIds, string? commentaire)
    {
        var (version, modele) = await VersionAPublierAsync(versionId, commentaire);
        var ids = formulaireIds.Distinct().ToList();
        if (ids.Count == 0) throw new RegleMetierException("Choisissez au moins un formulaire qui reçoit le nouveau logo.");
        var formulaires = new List<ColonnesFormulaire>();
        foreach (var id in ids) formulaires.Add(await FormulaireDuModeleAsync(modele, id));
        foreach (var formulaire in formulaires) ExigerDedie(version, formulaire);
        var dejaEnPlace = (await EtatFormulairesAsync(modele, await _modeles.GetModelesAsync()))
            .Where(e => ids.Contains(e.Formulaire.Id) && e.Resolution.Modele?.Affectation.VersionId == version.Id)
            .Select(e => $"{e.Formulaire.CodeFormulaire}.{e.Formulaire.Version}")
            .ToList();
        if (dejaEnPlace.Count > 0)
            throw new RegleMetierException($"La version {version.VersionNumber} est déjà le modèle de {string.Join(", ", dejaEnPlace)} : "
                + "envoyez le fichier qui contient le nouveau logo.");

        var verification = VerificateurModele.Verifier(_moteur.Analyser(version.Content), Contrat(modele), formulaires);
        ExigerAccepte(verification);

        var motif = Nettoyer(commentaire) is { } precision ? $"Mise à jour du logo : {precision}" : "Mise à jour du logo";
        var matricule = _utilisateur.MatriculeRequis;
        var resultats = new List<string>();
        await PublierAsync(version, verification, async () =>
        {
            var affectations = new List<ReportTemplateAssignment>();
            foreach (var formulaire in formulaires)
            {
                if (!await _modeles.FormulaireUtiliseAsync(formulaire.Id))
                {
                    affectations.Add(NouvelleAffectation(version, TypesAffectationModele.StructureFormulaire, formulaire.Id, motif));
                    resultats.Add($"{formulaire.CodeFormulaire} version {formulaire.Version}");
                    continue;
                }
                var (nouveau, numero, recrees) = await _modeles.NouvelleVersionFormulaireAsync(formulaire.Id, matricule, motif);
                affectations.Add(NouvelleAffectation(version, TypesAffectationModele.StructureFormulaire, nouveau, motif));
                resultats.Add($"{formulaire.CodeFormulaire} version {numero} ({recrees} document(s) recréé(s))");
            }
            return affectations;
        });
        return new PublicationModeleDto { Version = (await _modeles.GetVersionResumeAsync(version.Id))!, Formulaires = resultats };
    }

    public async Task<PublicationModeleDto> ReviserAsync(Guid versionId, Guid versionRemplaceeId, string? commentaire)
    {
        var (version, modele) = await VersionAPublierAsync(versionId, commentaire);
        var remplacee = await _modeles.GetVersionLectureAsync(versionRemplaceeId) ?? throw new RessourceIntrouvableException("Version remplacée introuvable.");
        if (remplacee.ReportTemplateCode != modele.Code || remplacee.FormGroupId != version.FormGroupId
            || remplacee.Status != StatutsVersionModele.Publiee || remplacee.Id == version.Id)
            throw new RegleMetierException("La version remplacée doit être une autre version publiée du même modèle.");

        var affectations = await _modeles.GetAffectationsAsync(new[] { modele.Code });
        var parDefaut = modele.FormRole == null && ParDefautActuelle(affectations, modele.Code)?.VersionId == remplacee.Id;
        // Formulaires dont les prochains documents utilisent la version remplacée.
        var formulaires = await FormulairesAsync(modele);
        var concernes = (await EtatFormulairesAsync(modele, await _modeles.GetModelesAsync()))
            .Where(e => e.Resolution.Modele?.Affectation.VersionId == remplacee.Id && e.Formulaire.GroupeId == version.FormGroupId)
            .Select(e => e.Formulaire)
            .ToList();
        if (!parDefaut && concernes.Count == 0)
            throw new RegleMetierException($"Aucun formulaire ni le modèle par défaut n'utilise la version {remplacee.VersionNumber} : rien à réviser.");

        // Comme la version remplacée, une révision peut utiliser une colonne absente de certains formulaires (laissée vide).
        var verification = VerificateurModele.Verifier(_moteur.Analyser(version.Content), Contrat(modele), parDefaut ? formulaires : concernes);
        ExigerAccepte(verification);

        var nouvelles = concernes.Select(f => NouvelleAffectation(version, TypesAffectationModele.RevisionFormulaire, f.Id, commentaire)).ToList();
        if (parDefaut) nouvelles.Add(NouvelleAffectation(version, TypesAffectationModele.ParDefaut, null, commentaire));
        await PublierAsync(version, verification, nouvelles);
        return new PublicationModeleDto
        {
            Version = (await _modeles.GetVersionResumeAsync(version.Id))!,
            Formulaires = concernes.Select(f => $"{f.CodeFormulaire} version {f.Version}").ToList(),
            ParDefaut = parDefaut
        };
    }

    public async Task<FichierDto> TelechargerAsync(Guid versionId)
    {
        var version = await _modeles.GetVersionLectureAsync(versionId) ?? throw new RessourceIntrouvableException("Version de modèle introuvable.");
        // Nom normalisé : code du formulaire et numéro du modèle dans ce formulaire (FE-ASS-MANU_M2.xlsx).
        var nom = version.FormGroupId is { } groupe && await _modeles.GetCodeFormulaireDuGroupeAsync(groupe) is { } code
            ? $"{code}_M{version.VersionNumber}"
            : $"{version.ReportTemplateCode}_M{version.VersionNumber}";
        return new FichierDto(version.Content, TypeExcel, $"{nom}.xlsx");
    }

    public Task<List<DemandeModeleDto>> GetDemandesAsync(string? statut) => _modeles.GetDemandesAsync(statut);

    public async Task<long> DemanderModeleAsync(Guid formulaireId, string? motif, string? changementVersion)
    {
        if (changementVersion is not (DemandesModele.MemeVersion or DemandesModele.VersionSuivante))
            throw new RegleMetierException("Choisissez « même version » ou « version suivante ».");
        motif = Nettoyer(motif) ?? throw new RegleMetierException("Précisez le motif de la demande (ex. nouveau logo, mise en page).");
        if (VerifierCommentaire(motif) is { } refus) throw new RegleMetierException(refus);
        var formulaire = await _modeles.GetColonnesFormulaireAsync(formulaireId) ?? throw new RessourceIntrouvableException("Formulaire introuvable.");
        if (formulaire.Statut == StatutsPlan.Archive)
            throw new RegleMetierException($"Le formulaire {formulaire.CodeFormulaire} version {formulaire.Version} est archivé : ses documents gardent leur modèle.");
        if (!(await _modeles.GetModelesAsync()).Any(m => m.FormRole != null && m.FormRole == formulaire.Role))
            throw new RegleMetierException("Ce type de document n'a pas encore de modèle Excel.");
        if (await _modeles.GetDemandeEnAttenteAsync(formulaireId) != null)
            throw new RegleMetierException($"Une demande de modèle attend déjà pour {formulaire.CodeFormulaire} version {formulaire.Version} : "
                + "attendez son traitement ou annulez-la.");
        var demande = new ReportTemplateRequest
        {
            FormId = formulaireId,
            Origin = DemandesModele.Superviseur,
            Reason = motif,
            VersionChange = changementVersion,
            Status = DemandesModele.EnAttente,
            RequestedBy = _utilisateur.MatriculeRequis,
            RequestedAt = DateTime.Now
        };
        _modeles.Ajouter(demande);
        await _modeles.SaveChangesAsync();
        return demande.Id;
    }

    public async Task AnnulerDemandeAsync(long demandeId)
    {
        var demande = await _modeles.GetDemandeAsync(demandeId) ?? throw new RessourceIntrouvableException("Demande introuvable.");
        if (demande.Status != DemandesModele.EnAttente) throw new RegleMetierException("Cette demande n'est plus en attente.");
        demande.Status = DemandesModele.Annulee;
        demande.HandledBy = _utilisateur.MatriculeRequis;
        demande.HandledAt = DateTime.Now;
        await _modeles.SaveChangesAsync();
    }

    public async Task<PublicationModeleDto> TraiterDemandeAsync(long demandeId, Guid versionId, string? commentaire)
    {
        var demande = await _modeles.GetDemandeAsync(demandeId) ?? throw new RessourceIntrouvableException("Demande introuvable.");
        if (demande.Status != DemandesModele.EnAttente) throw new RegleMetierException("Cette demande n'est plus en attente.");
        var (version, modele) = await VersionAPublierAsync(versionId, commentaire);
        var formulaire = await FormulaireDuModeleAsync(modele, demande.FormId);
        ExigerDedie(version, formulaire);
        var aUnModele = await ExigerPasDejaEnPlaceAsync(modele, version, formulaire);
        var verification = VerificateurModele.Verifier(_moteur.Analyser(version.Content), Contrat(modele), new[] { formulaire });
        ExigerAccepte(verification);

        var motif = string.Join(" — ", new[] { demande.Reason, Nettoyer(commentaire) }.Where(t => !string.IsNullOrWhiteSpace(t)));
        if (motif.Length == 0) motif = demande.Origin == DemandesModele.NouvelleStructure ? "Nouvelle structure" : "Premier modèle";
        if (motif.Length > LongueurMaximaleCommentaire) motif = motif[..LongueurMaximaleCommentaire];
        var matricule = _utilisateur.MatriculeRequis;
        var resultats = new List<string>();
        await PublierAsync(version, verification, async () =>
        {
            if (demande.Origin == DemandesModele.Superviseur && demande.VersionChange == DemandesModele.VersionSuivante)
            {
                // Version suivante : la ligne et ses documents génériques sont archivés tels quels puis recréés en version suivante.
                var (nouveau, numero, recrees) = await _modeles.NouvelleVersionFormulaireAsync(formulaire.Id, matricule, motif);
                resultats.Add($"{formulaire.CodeFormulaire} version {numero} ({recrees} document(s) recréé(s))");
                return new[] { NouvelleAffectation(version, TypesAffectationModele.StructureFormulaire, nouveau, motif) };
            }
            resultats.Add($"{formulaire.CodeFormulaire} version {formulaire.Version}");
            // Même version : les documents déjà créés gardent leur modèle. Nouveau formulaire, nouvelle structure (ou formulaire
            // encore sans modèle) : modèle de la version, valable aussi pour ses documents déjà créés.
            var nature = demande.Origin == DemandesModele.Superviseur && aUnModele
                ? TypesAffectationModele.RevisionFormulaire : TypesAffectationModele.StructureFormulaire;
            return new[] { NouvelleAffectation(version, nature, formulaire.Id, motif) };
        }, affectations => MarquerTraiteeAsync(demande, affectations));
        return new PublicationModeleDto { Version = (await _modeles.GetVersionResumeAsync(version.Id))!, Formulaires = resultats };
    }

    /// <summary>La demande satisfaite garde le rattachement qui l'a satisfaite (dans la même transaction).</summary>
    private async Task MarquerTraiteeAsync(ReportTemplateRequest? demande, IReadOnlyCollection<ReportTemplateAssignment> affectations)
    {
        if (demande == null || demande.Status != DemandesModele.EnAttente) return;
        demande.Status = DemandesModele.Traitee;
        demande.HandledBy = _utilisateur.MatriculeRequis;
        demande.HandledAt = DateTime.Now;
        demande.AssignmentId = affectations.First().Id;
        await _modeles.SaveChangesAsync();
    }

    /// <summary>La version est déjà le modèle actuel de ce formulaire : rien à rattacher. Vrai si le formulaire a déjà un modèle.</summary>
    private async Task<bool> ExigerPasDejaEnPlaceAsync(ReportTemplate modele, ReportTemplateVersion version, ColonnesFormulaire formulaire)
    {
        var actuel = (await EtatFormulairesAsync(modele, await _modeles.GetModelesAsync()))
            .FirstOrDefault(e => e.Formulaire.Id == formulaire.Id).Resolution?.Modele?.Affectation.VersionId;
        if (actuel == version.Id)
            throw new RegleMetierException($"Le modèle M{version.VersionNumber} est déjà le modèle de {formulaire.CodeFormulaire} version {formulaire.Version} : "
                + "envoyez le nouveau fichier.");
        return actuel != null;
    }

    public async Task ImporterModelesLivresAsync()
    {
        foreach (var code in await _modeles.GetModelesParDefautSansVersionAsync())
        {
            var contenu = _modelesLivres.Lire(code);
            if (contenu == null)
            {
                _logger.LogWarning("Modèle de rapport {Code} : aucun fichier livré, envoyez-en un depuis l'écran des modèles.", code);
                continue;
            }
            var modele = await ModeleAsync(code);
            var verification = await VerifierAsync(modele, contenu, null);
            if (!verification.Accepte)
            {
                _logger.LogError("Modèle de rapport livré {Code} refusé : {Erreurs}", code, string.Join(" ", verification.Erreurs));
                continue;
            }
            var maintenant = DateTime.Now;
            var version = new ReportTemplateVersion
            {
                Id = Guid.NewGuid(),
                ReportTemplateCode = code,
                Name = modele.Name,
                VersionNumber = 1,
                FileName = $"{code}.xlsx",
                Content = contenu,
                Status = StatutsVersionModele.Publiee,
                ValidationWarnings = Avertissements(verification.Avertissements),
                Comment = "Modèle livré avec l'application",
                UploadedAt = maintenant,
                PublishedAt = maintenant
            };
            await _modeles.ExecuterDansTransactionAsync(async () =>
            {
                _modeles.Ajouter(version);
                await _modeles.SaveChangesAsync();
                _modeles.Ajouter(new ReportTemplateAssignment
                {
                    ReportTemplateVersionId = version.Id,
                    Kind = TypesAffectationModele.ParDefaut,
                    Comment = "Modèle livré avec l'application",
                    AssignedAt = maintenant
                });
                await _modeles.SaveChangesAsync();
            });
            _logger.LogInformation("Modèle de rapport {Code} : version 1 livrée importée.", code);
        }
        await RattacherFormulairesInitiauxAsync();
    }

    /// <summary>
    /// Rattachements de départ (tests) : chaque formulaire listé qui n'a encore aucun modèle reçoit son modèle dédié (le
    /// fichier de son type), sur ses versions non archivées. Rejouable : un formulaire déjà pourvu n'est jamais modifié.
    /// </summary>
    private async Task RattacherFormulairesInitiauxAsync()
    {
        foreach (var initial in await _modeles.GetFormulairesInitiauxAsync())
        {
            if (await _modeles.FormulaireAUnModeleAsync(initial.CodeFormulaire)) continue;
            var lignes = await _modeles.GetVersionsNonArchiveesAsync(initial.CodeFormulaire);
            var contenu = _modelesLivres.Lire(initial.CodeModele);
            var modele = await _modeles.GetModeleAsync(initial.CodeModele);
            if (lignes.Count == 0 || contenu == null || modele?.FormRole == null)
            {
                _logger.LogWarning("Rattachement de départ {Formulaire} → {Modele} impossible : formulaire absent ou fichier introuvable.",
                    initial.CodeFormulaire, initial.CodeModele);
                continue;
            }
            var formulaire = await FormulaireDuModeleAsync(modele, lignes[0]);
            var verification = VerificateurModele.Verifier(_moteur.Analyser(contenu), Contrat(modele), new[] { formulaire });
            if (!verification.Accepte)
            {
                _logger.LogError("Modèle {Modele} refusé pour {Formulaire} : {Erreurs}", initial.CodeModele, initial.CodeFormulaire,
                    string.Join(" ", verification.Erreurs));
                continue;
            }
            var maintenant = DateTime.Now;
            var numero = await _modeles.GetDernierNumeroAsync(modele.Code, formulaire.GroupeId) + 1;
            var version = new ReportTemplateVersion
            {
                Id = Guid.NewGuid(),
                ReportTemplateCode = modele.Code,
                FormGroupId = formulaire.GroupeId,
                Name = await NomModeleAsync(modele, formulaire, numero),
                VersionNumber = numero,
                FileName = $"{modele.Code}.xlsx",
                Content = contenu,
                Status = StatutsVersionModele.Publiee,
                ValidationWarnings = Avertissements(verification.Avertissements),
                Comment = "Modèle livré avec l'application",
                UploadedAt = maintenant,
                PublishedAt = maintenant
            };
            await _modeles.ExecuterDansTransactionAsync(async () =>
            {
                _modeles.Ajouter(version);
                await _modeles.SaveChangesAsync();
                foreach (var formulaireId in lignes)
                    _modeles.Ajouter(new ReportTemplateAssignment
                    {
                        ReportTemplateVersionId = version.Id,
                        Kind = TypesAffectationModele.StructureFormulaire,
                        FormId = formulaireId,
                        Comment = "Rattachement de départ du modèle livré",
                        AssignedAt = maintenant
                    });
                await _modeles.SaveChangesAsync();
            });
            _logger.LogInformation("Formulaire {Formulaire} : modèle dédié {Modele} V{Numero} rattaché.", initial.CodeFormulaire, modele.Code, version.VersionNumber);
        }
    }

    // ── Publication ──────────────────────────────────────────────────────

    private async Task<(ReportTemplateVersion Version, ReportTemplate Modele)> VersionAPublierAsync(Guid versionId, string? commentaire)
    {
        if (VerifierCommentaire(commentaire) is { } refus) throw new RegleMetierException(refus);
        var version = await _modeles.GetVersionAsync(versionId) ?? throw new RessourceIntrouvableException("Version de modèle introuvable.");
        return (version, await ModeleAsync(version.ReportTemplateCode));
    }

    private ReportTemplateAssignment NouvelleAffectation(ReportTemplateVersion version, string nature, Guid? formulaireId, string? commentaire) => new()
    {
        ReportTemplateVersionId = version.Id,
        Kind = nature,
        FormId = formulaireId,
        Comment = Nettoyer(commentaire),
        AssignedBy = _utilisateur.MatriculeRequis,
        AssignedAt = DateTime.Now
    };

    /// <summary>Publie la version (si elle est encore brouillon) et enregistre ses affectations, tout ou rien.</summary>
    private Task PublierAsync(ReportTemplateVersion version, VerificationModeleDto verification, IReadOnlyCollection<ReportTemplateAssignment> affectations) =>
        PublierAsync(version, verification, () => Task.FromResult(affectations));

    /// <summary>
    /// Publie la version puis enregistre les affectations préparées dans la même transaction (préparation qui peut
    /// créer des versions de formulaire et recréer des documents) : tout ou rien.
    /// </summary>
    private async Task PublierAsync(ReportTemplateVersion version, VerificationModeleDto verification,
        Func<Task<IReadOnlyCollection<ReportTemplateAssignment>>> preparerAffectations,
        Func<IReadOnlyCollection<ReportTemplateAssignment>, Task>? apres = null)
    {
        var matricule = _utilisateur.MatriculeRequis;
        await _modeles.ExecuterDansTransactionAsync(async () =>
        {
            // Deux enregistrements : la base n'accepte d'affecter qu'une version déjà publiée.
            if (version.Status == StatutsVersionModele.Brouillon)
            {
                version.Status = StatutsVersionModele.Publiee;
                version.PublishedBy = matricule;
                version.PublishedAt = DateTime.Now;
                version.ValidationWarnings = Avertissements(verification.Avertissements);
                await _modeles.SaveChangesAsync();
            }
            var affectations = await preparerAffectations();
            foreach (var affectation in affectations) _modeles.Ajouter(affectation);
            await _modeles.SaveChangesAsync();
            if (apres != null) await apres(affectations);
        });
    }

    private static AffectationModele? ParDefautActuelle(IEnumerable<AffectationModele> affectations, string codeModele) => affectations
        .Where(a => a.CodeModele == codeModele && a.Nature == TypesAffectationModele.ParDefaut)
        .OrderByDescending(a => a.Ordre)
        .FirstOrDefault();

    /// <summary>Une version ne se rattache qu'au formulaire auquel elle est dédiée (ses versions et ses changements de code).</summary>
    private static void ExigerDedie(ReportTemplateVersion version, ColonnesFormulaire formulaire)
    {
        if (version.FormGroupId != formulaire.GroupeId)
            throw new RegleMetierException($"La version {version.VersionNumber} est le modèle d'un autre formulaire : envoyez le fichier "
                + $"pour {formulaire.CodeFormulaire} version {formulaire.Version}.");
    }

    /// <summary>
    /// Nom normalisé du modèle dédié : désignation du formulaire, puis code.version du formulaire.numéro du modèle
    /// (« Plan de contrôle soupape PAS71 — FE-ASS-MANU.1.2 ») ; nom du type pour un modèle par défaut.
    /// </summary>
    private async Task<string> NomModeleAsync(ReportTemplate modele, ColonnesFormulaire? formulaire, int numero)
    {
        if (formulaire == null) return Tronquer200(modele.Name);
        var designation = await _modeles.GetDesignationFormulaireAsync(formulaire.Id);
        return Tronquer200($"{(string.IsNullOrWhiteSpace(designation) ? modele.Name : designation.Trim())} — {formulaire.CodeFormulaire}.{formulaire.Version}.{numero}");
    }

    private static string Tronquer200(string nom) => nom.Length <= 200 ? nom : nom[..200];

    private static void ExigerAccepte(VerificationModeleDto verification)
    {
        if (!verification.Accepte)
            throw new RegleMetierException("Ce modèle ne convient pas : " + string.Join(" ", verification.Erreurs));
    }

    // ── Vérification ─────────────────────────────────────────────────────

    private string? VerifierFichier(string nomFichier, byte[] contenu)
    {
        if (!string.Equals(Path.GetExtension(nomFichier), ".xlsx", StringComparison.OrdinalIgnoreCase))
            return "Le modèle doit être un fichier Excel .xlsx.";
        if (contenu.Length == 0)
            return "Le fichier est vide.";
        if (contenu.Length > _parametres.TailleMaximaleModeleKo * 1024L)
            return $"Le fichier dépasse {_parametres.TailleMaximaleModeleKo / 1024} Mo.";
        // Un .xlsx est une archive zip.
        if (contenu.Length < 4 || contenu[0] != 'P' || contenu[1] != 'K')
            return "Le fichier n'est pas un classeur Excel .xlsx.";
        return null;
    }

    private static string? VerifierCommentaire(string? commentaire) =>
        commentaire?.Trim().Length > LongueurMaximaleCommentaire ? $"Le motif ne doit pas dépasser {LongueurMaximaleCommentaire} caractères." : null;

    private async Task<VerificationModeleDto> VerifierAsync(ReportTemplate modele, byte[] contenu, Guid? formulaireId) =>
        VerificateurModele.Verifier(_moteur.Analyser(contenu), Contrat(modele), await FormulairesVerifiesAsync(modele, formulaireId));

    /// <summary>Colonnes de la version de formulaire visée, sinon de toutes celles du type (aucune pour un document d'exécution).</summary>
    private async Task<List<ColonnesFormulaire>> FormulairesVerifiesAsync(ReportTemplate modele, Guid? formulaireId) =>
        formulaireId is { } id ? new List<ColonnesFormulaire> { await FormulaireDuModeleAsync(modele, id) } : await FormulairesAsync(modele);

    /// <summary>Versions de formulaire non archivées dont ce modèle met en forme les documents (aucune pour un modèle sans formulaire).</summary>
    private async Task<List<ColonnesFormulaire>> FormulairesAsync(ReportTemplate modele) =>
        modele.FormRole is not { } role ? new List<ColonnesFormulaire>() : await _modeles.GetColonnesFormulairesAsync(role);

    /// <summary>Chaque formulaire du rôle du modèle et le modèle de ses prochains documents (ou la raison de son absence).</summary>
    private async Task<List<(ColonnesFormulaire Formulaire, ResolutionModele Resolution)>> EtatFormulairesAsync(ReportTemplate modele, List<ReportTemplate> modeles)
    {
        var formulaires = await FormulairesAsync(modele);
        if (formulaires.Count == 0) return new();
        var versions = await _modeles.GetVersionsFormulairesAsync(formulaires.Select(f => f.GroupeId).Distinct().ToList());
        var affectations = await AffectationsDuTypeAsync(modele, modeles);
        return formulaires
            .Select(f => (f, versions.FirstOrDefault(v => v.Id == f.Id) is { } version
                ? ResolutionModeleRapport.PourFormulaire(ResolutionModeleRapport.Maintenant, version, versions, affectations)
                : ResolutionModele.Absent(ModeleManquant.AucunModele)))
            .ToList();
    }

    /// <summary>Rattachements de tous les modèles qui mettent en forme le même type de document que ce modèle.</summary>
    private async Task<List<AffectationModele>> AffectationsDuTypeAsync(ReportTemplate modele, List<ReportTemplate> modeles) =>
        await _modeles.GetAffectationsAsync(modeles
            .Where(m => m.DocumentTypeCode == modele.DocumentTypeCode && m.ExecutionDocumentTypeCode == modele.ExecutionDocumentTypeCode)
            .Select(m => m.Code)
            .ToList());

    /// <summary>Version de formulaire non archivée dont ce modèle met en forme les documents (même rôle).</summary>
    private async Task<ColonnesFormulaire> FormulaireDuModeleAsync(ReportTemplate modele, Guid formulaireId)
    {
        if (modele.FormRole == null)
            throw new RegleMetierException("Ce modèle met en forme des documents sans formulaire : publiez-le comme modèle par défaut.");
        var formulaire = await _modeles.GetColonnesFormulaireAsync(formulaireId) ?? throw new RessourceIntrouvableException("Formulaire introuvable.");
        if (formulaire.Statut == StatutsPlan.Archive)
            throw new RegleMetierException($"Le formulaire {formulaire.CodeFormulaire} version {formulaire.Version} est archivé : ses documents gardent leur modèle.");
        if (formulaire.Role != modele.FormRole)
            throw new RegleMetierException($"Le formulaire {formulaire.CodeFormulaire} n'est pas un formulaire des documents mis en forme par ce modèle.");
        return formulaire;
    }

    // ── Utilitaires ──────────────────────────────────────────────────────

    private async Task<ReportTemplate> ModeleAsync(string code) =>
        await _modeles.GetModeleAsync(code) ?? throw new RessourceIntrouvableException($"Modèle de rapport {code} introuvable.");

    /// <summary>Contrat de champs du type mis en forme par le modèle (document qualité ou document d'exécution).</summary>
    private IContratRapport Contrat(ReportTemplate modele) =>
        modele.ExecutionDocumentTypeCode is { } typeExecution ? DefinitionExecution(typeExecution) : Definition(modele.DocumentTypeCode!);

    private IDefinitionRapportExecution DefinitionExecution(string typeExecution) =>
        _definitionsExecution.FirstOrDefault(d => d.TypeExecution == typeExecution)
        ?? throw new RegleMetierException($"Les documents d'exécution {typeExecution} n'ont pas encore de contrat de champs pour les modèles.");

    private IDefinitionRapport Definition(string typeDocumentCode) =>
        _definitions.FirstOrDefault(d => d.TypeDocumentCode == typeDocumentCode)
        ?? throw new RegleMetierException($"Les documents {typeDocumentCode} n'ont pas encore de contrat de champs pour les modèles.");

    private static string Empreinte(byte[] contenu) => Convert.ToHexString(SHA256.HashData(contenu));

    private static string? Nettoyer(string? commentaire) => string.IsNullOrWhiteSpace(commentaire) ? null : commentaire.Trim();

    /// <summary>Nom trop long raccourci en gardant son extension.</summary>
    private static string Tronquer(string nom, int longueur) =>
        nom.Length <= longueur ? nom : Path.GetFileNameWithoutExtension(nom)[..(longueur - Path.GetExtension(nom).Length)] + Path.GetExtension(nom);

    private static string? Avertissements(List<string> avertissements) =>
        avertissements.Count == 0 ? null : JsonSerializer.Serialize(avertissements);

    private static string Portee(PorteeChamp portee) => portee switch
    {
        PorteeChamp.Section => "SECTION",
        PorteeChamp.Ligne => "LIGNE",
        PorteeChamp.ColonneLigne => "COLONNE",
        _ => "DOCUMENT"
    };
}

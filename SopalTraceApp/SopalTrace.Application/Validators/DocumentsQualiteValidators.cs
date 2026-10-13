using System;
using FluentValidation;
using SopalTrace.Application.DTOs.QualityPlans.Documents;
using SopalTrace.Application.DTOs.QualityPlans.Fabrication;
using SopalTrace.Application.DTOs.QualityPlans.Modeles;

namespace SopalTrace.Application.Validators.QualityPlans;

/// <summary>
/// Validation des documents qualité (documents génériques, modèles et plans de fabrication).
/// Les limites reprennent celles des colonnes de la base : une saisie trop longue reçoit un message clair (400)
/// au lieu d'une erreur technique. Les règles métier restent dans les services et les contraintes de la base.
/// </summary>
internal static class LimitesDocument
{
    /// <summary>Document_Entete.Nom (150) moins le suffixe de version ajouté par le service (« - V12 »).</summary>
    public const int NomVersionne = 140;
    public const int Nom = 150;
    public const int Designation = 200;
    /// <summary>Désignation d'un modèle, suffixée « V1.0 » par le service.</summary>
    public const int DesignationModele = 190;
    public const int CodeArticle = 30;
    public const int CodeReferentiel = 20;
    public const int CodeLong = 30;
    public const int LibelleSection = 300;
    public const int LibelleLigne = 250;
    public const int MoyenTexte = 100;
    public const int LimiteSpec = 100;
    public const int Instrument = 40;
    public const int CleColonne = 60;
    public const int ValeurColonne = 500;

    public static void Texte<T>(AbstractValidator<T> v, Func<T, string?> champ, int max, string nom, bool obligatoire = false)
    {
        var regle = v.RuleFor(x => champ(x)).Cascade(CascadeMode.Stop);
        if (obligatoire) regle.NotEmpty().WithName(nom).WithMessage($"{nom} : obligatoire.");
        regle.MaximumLength(max).WithName(nom).WithMessage($"{nom} : {max} caractères maximum.");
    }

    /// <summary>Règles communes aux lignes des documents, plans et modèles.</summary>
    public static void Ligne<T>(AbstractValidator<T> v, Func<T, string?> libelle, Func<T, string?> moyenTexte,
        Func<T, string?> instrument, Func<T, string?> limiteSpec, Func<T, string?> machine)
    {
        Texte(v, libelle, LibelleLigne, "Libellé de la ligne");
        Texte(v, moyenTexte, MoyenTexte, "Moyen de contrôle");
        Texte(v, instrument, Instrument, "Code instrument");
        Texte(v, limiteSpec, LimiteSpec, "Limite de spécification");
        Texte(v, machine, CodeLong, "Code machine");
    }

    public static void Colonne<T>(AbstractValidator<T> v, Func<T, string?> cle, Func<T, string?> valeur)
    {
        Texte(v, cle, CleColonne, "Colonne", obligatoire: true);
        Texte(v, valeur, ValeurColonne, "Valeur de colonne");
    }
}

// ── Documents génériques ────────────────────────────────────────────────

public class CreateDocumentExtraColonneValidator : AbstractValidator<CreateDocumentExtraColonneDto>
{
    public CreateDocumentExtraColonneValidator() => LimitesDocument.Colonne(this, x => x.CleColonne, x => x.ValeurColonne);
}

public class CreateDocumentLigneValidator : AbstractValidator<CreateDocumentLigneDto>
{
    public CreateDocumentLigneValidator()
    {
        LimitesDocument.Ligne(this, x => x.LibelleAffiche, x => x.MoyenTexteLibre, x => x.InstrumentCode, x => x.LimiteSpecTexte, x => x.MachineCode);
        RuleForEach(x => x.ExtraColonnes).SetValidator(new CreateDocumentExtraColonneValidator());
    }
}

public class CreateDocumentSectionValidator : AbstractValidator<CreateDocumentSectionDto>
{
    public CreateDocumentSectionValidator()
    {
        LimitesDocument.Texte(this, x => x.LibelleSection, LimitesDocument.LibelleSection, "Libellé de la section");
        RuleForEach(x => x.Lignes).SetValidator(new CreateDocumentLigneValidator());
    }
}

public class CreateDocumentRequestValidator : AbstractValidator<CreateDocumentRequestDto>
{
    public CreateDocumentRequestValidator()
    {
        LimitesDocument.Texte(this, x => x.TypeDocumentCode, LimitesDocument.CodeLong, "Type de document", obligatoire: true);
        LimitesDocument.Texte(this, x => x.Nom, LimitesDocument.NomVersionne, "Nom", obligatoire: true);
        LimitesDocument.Texte(this, x => x.Designation, LimitesDocument.Designation, "Désignation");
        LimitesDocument.Texte(this, x => x.OperationCode, LimitesDocument.CodeReferentiel, "Opération");
        LimitesDocument.Texte(this, x => x.NatureArticleCode, LimitesDocument.CodeReferentiel, "Nature d'article");
        LimitesDocument.Texte(this, x => x.FamilleProduitFiniCode, LimitesDocument.CodeLong, "Famille de produit fini");
        LimitesDocument.Texte(this, x => x.PosteCode, LimitesDocument.CodeLong, "Poste");
        RuleForEach(x => x.Sections).SetValidator(new CreateDocumentSectionValidator());
    }
}

public class NouvelleVersionDocumentRequestValidator : AbstractValidator<NouvelleVersionDocumentRequestDto>
{
    public NouvelleVersionDocumentRequestValidator() => Include(new CreateDocumentRequestValidator());
}

public class UpdateDocumentRequestValidator : AbstractValidator<UpdateDocumentRequestDto>
{
    public UpdateDocumentRequestValidator()
    {
        LimitesDocument.Texte(this, x => x.Nom, LimitesDocument.NomVersionne, "Nom");
        LimitesDocument.Texte(this, x => x.OperationCode, LimitesDocument.CodeReferentiel, "Opération");
        RuleForEach(x => x.Sections).SetValidator(new CreateDocumentSectionValidator());
    }
}

// ── Plans de fabrication ────────────────────────────────────────────────

public class CreatePlanFabricationExtraColonneValidator : AbstractValidator<CreatePlanFabricationExtraColonneDto>
{
    public CreatePlanFabricationExtraColonneValidator() => LimitesDocument.Colonne(this, x => x.CleColonne, x => x.ValeurColonne);
}

public class CreatePlanFabricationLigneValidator : AbstractValidator<CreatePlanFabricationLigneDto>
{
    public CreatePlanFabricationLigneValidator()
    {
        LimitesDocument.Ligne(this, x => x.LibelleAffiche, x => x.MoyenTexteLibre, x => x.InstrumentCode, x => x.LimiteSpecTexte, x => x.MachineCode);
        RuleForEach(x => x.ExtraColonnes).SetValidator(new CreatePlanFabricationExtraColonneValidator());
    }
}

public class CreatePlanFabricationSectionValidator : AbstractValidator<CreatePlanFabricationSectionDto>
{
    public CreatePlanFabricationSectionValidator()
    {
        LimitesDocument.Texte(this, x => x.LibelleSection, LimitesDocument.LibelleSection, "Libellé de la section");
        RuleForEach(x => x.Lignes).SetValidator(new CreatePlanFabricationLigneValidator());
    }
}

public class CreatePlanFabricationRequestValidator : AbstractValidator<CreatePlanFabricationRequestDto>
{
    public CreatePlanFabricationRequestValidator()
    {
        // Le nom d'un plan est son code article Sage versionné.
        LimitesDocument.Texte(this, x => x.Nom, LimitesDocument.CodeArticle, "Code article");
        LimitesDocument.Texte(this, x => x.CodeArticleSageVersionne, LimitesDocument.CodeArticle, "Code article");
        LimitesDocument.Texte(this, x => x.Designation, LimitesDocument.Designation, "Désignation");
        LimitesDocument.Texte(this, x => x.OperationCode, LimitesDocument.CodeReferentiel, "Opération");
        RuleFor(x => x).Must(x => !string.IsNullOrWhiteSpace(x.CodeArticleSageVersionne) || !string.IsNullOrWhiteSpace(x.Nom))
            .OverridePropertyName("CodeArticleSageVersionne").WithMessage("Code article : obligatoire.");
        RuleForEach(x => x.Sections).SetValidator(new CreatePlanFabricationSectionValidator());
    }
}

public class NouvelleVersionPlanFabricationRequestValidator : AbstractValidator<NouvelleVersionPlanFabricationRequestDto>
{
    public NouvelleVersionPlanFabricationRequestValidator()
    {
        // Les champs vides reprennent ceux du plan existant : seules les limites s'appliquent.
        LimitesDocument.Texte(this, x => x.Nom, LimitesDocument.CodeArticle, "Code article");
        LimitesDocument.Texte(this, x => x.CodeArticleSageVersionne, LimitesDocument.CodeArticle, "Code article");
        LimitesDocument.Texte(this, x => x.Designation, LimitesDocument.Designation, "Désignation");
        RuleForEach(x => x.Sections).SetValidator(new CreatePlanFabricationSectionValidator());
    }
}

public class UpdatePlanFabricationRequestValidator : AbstractValidator<UpdatePlanFabricationRequestDto>
{
    public UpdatePlanFabricationRequestValidator()
    {
        LimitesDocument.Texte(this, x => x.Nom, LimitesDocument.CodeArticle, "Code article");
        LimitesDocument.Texte(this, x => x.CodeArticleSageVersionne, LimitesDocument.CodeArticle, "Code article");
        LimitesDocument.Texte(this, x => x.OperationCode, LimitesDocument.CodeReferentiel, "Opération");
        RuleForEach(x => x.Sections).SetValidator(new CreatePlanFabricationSectionValidator());
    }
}

// ── Modèles de fabrication ──────────────────────────────────────────────

public class CreateModeleExtraColonneValidator : AbstractValidator<CreateModeleExtraColonneDto>
{
    public CreateModeleExtraColonneValidator() => LimitesDocument.Colonne(this, x => x.CleColonne, x => x.ValeurColonne);
}

public class LigneModeleEditValidator : AbstractValidator<LigneModeleEditDto>
{
    public LigneModeleEditValidator()
    {
        LimitesDocument.Ligne(this, x => x.LibelleAffiche, x => x.MoyenTexteLibre, x => x.InstrumentCode, x => x.LimiteSpecTexte, _ => null);
        RuleForEach(x => x.ExtraColonnes).SetValidator(new CreateModeleExtraColonneValidator());
    }
}

public class SectionModeleEditValidator : AbstractValidator<SectionModeleEditDto>
{
    public SectionModeleEditValidator()
    {
        LimitesDocument.Texte(this, x => x.LibelleSection, LimitesDocument.LibelleSection, "Libellé de la section");
        RuleForEach(x => x.Lignes).SetValidator(new LigneModeleEditValidator());
    }
}

public class CreateModeleRequestValidator : AbstractValidator<CreateModeleRequestDto>
{
    public CreateModeleRequestValidator()
    {
        LimitesDocument.Texte(this, x => x.Code, LimitesDocument.Nom, "Code du modèle", obligatoire: true);
        LimitesDocument.Texte(this, x => x.Libelle, LimitesDocument.DesignationModele, "Libellé du modèle");
        LimitesDocument.Texte(this, x => x.NatureComposantCode, LimitesDocument.CodeReferentiel, "Nature d'article");
        LimitesDocument.Texte(this, x => x.OperationCode, LimitesDocument.CodeReferentiel, "Opération");
        LimitesDocument.Texte(this, x => x.FamilleProduitCode, LimitesDocument.CodeLong, "Famille de produit fini");
        RuleForEach(x => x.Sections).SetValidator(new SectionModeleEditValidator());
    }
}

public class NouvelleVersionModeleRequestValidator : AbstractValidator<NouvelleVersionModeleRequestDto>
{
    public NouvelleVersionModeleRequestValidator()
    {
        LimitesDocument.Texte(this, x => x.Code, LimitesDocument.Nom, "Code du modèle");
        LimitesDocument.Texte(this, x => x.Libelle, LimitesDocument.DesignationModele, "Libellé du modèle");
        RuleForEach(x => x.Sections).SetValidator(new SectionModeleEditValidator());
    }
}

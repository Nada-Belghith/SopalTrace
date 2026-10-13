using SopalTrace.Application.DTOs.QualityPlans.Echantillonnage;
using SopalTrace.Domain.Constants;
using SopalTrace.Application.Helpers;
using SopalTrace.Domain.Entities;

namespace SopalTrace.Application.Mappers;

/// <summary>
/// Plan d'échantillonnage = Document_Entete (type PLAN_ECHANTILLONNAGE) + paramètres ISO 2859-1 (SamplingPlan).
/// Les DTO gardent les noms attendus par le front.
/// </summary>
public static class DocumentEchantillonnageMapper
{
    public static DocumentEchantillonnageResponseDto? ToResponseDto(this DocumentEntete? entity)
    {
        if (entity?.SamplingPlan is not SamplingPlan plan) return null;

        return new DocumentEchantillonnageResponseDto
        {
            Id = entity.Id,
            RowVersion = VersionsDocument.EnTexte(entity.RowVersion),
            NiveauControle = plan.InspectionLevelCode,
            TypePlan = plan.SamplingType,
            ModeControle = plan.InspectionSeverity,
            NqaId = plan.AcceptableQualityLevelId,
            ValeurNqa = plan.AcceptableQualityLevel?.ValeurNqa ?? 0,
            Version = entity.Version,
            Statut = entity.Statut,
            CreePar = entity.CreePar,
            CreeLe = entity.CreeLe,
            ModifiePar = entity.ModifiePar!,
            ModifieLe = entity.ModifieLe,
            CommentaireVersion = entity.CommentaireVersion!,
            Remarques = entity.Remarques!,
            LegendeMoyens = entity.LegendeMoyens!,
            CritereAcceptationAc = plan.AcceptanceNumber,
            CritereRejetRe = plan.RejectionNumber
        };
    }

    /// <summary>Nouveau plan (sans version ni statut : fixés par le service). Le niveau de qualité acceptable est résolu par le service.</summary>
    public static DocumentEntete NouveauPlan(UpdateDocumentEchantillonnageRequestDto donnees, int niveauQualiteAcceptableId, string? commentaireVersion)
    {
        var entete = new DocumentEntete
        {
            TypeDocumentCode = TypesDocument.PlanEchantillonnage,
            Portee = PorteesDocument.Generique,
            Remarques = donnees.Remarques,
            LegendeMoyens = donnees.LegendeMoyens,
            CommentaireVersion = commentaireVersion,
            SamplingPlan = new SamplingPlan()
        };
        AppliquerParametres(entete.SamplingPlan, donnees.NiveauControle, donnees.TypePlan, donnees.ModeControle,
            niveauQualiteAcceptableId, donnees.CritereAcceptationAc, donnees.CritereRejetRe);
        return entete;
    }

    public static DocumentEntete NouveauPlan(CreateDocumentEchantillonnageRequestDto dto, int niveauQualiteAcceptableId)
    {
        var entete = new DocumentEntete
        {
            TypeDocumentCode = TypesDocument.PlanEchantillonnage,
            Portee = PorteesDocument.Generique,
            Remarques = dto.Remarques,
            LegendeMoyens = dto.LegendeMoyens,
            CommentaireVersion = dto.CommentaireVersion,
            SamplingPlan = new SamplingPlan()
        };
        AppliquerParametres(entete.SamplingPlan, dto.NiveauControle, dto.TypePlan, dto.ModeControle,
            niveauQualiteAcceptableId, dto.CritereAcceptationAc, dto.CritereRejetRe);
        return entete;
    }

    public static void UpdateEntity(this DocumentEntete entity, UpdateDocumentEchantillonnageRequestDto dto, int niveauQualiteAcceptableId)
    {
        entity.Remarques = dto.Remarques;
        entity.LegendeMoyens = dto.LegendeMoyens;
        AppliquerParametres(entity.SamplingPlan!, dto.NiveauControle, dto.TypePlan, dto.ModeControle,
            niveauQualiteAcceptableId, dto.CritereAcceptationAc, dto.CritereRejetRe);
    }

    /// <summary>Nom lisible du document, versionné comme les autres documents qualité.</summary>
    public static string Nom(int version) => $"Plan d'échantillonnage - V{version}";

    private static void AppliquerParametres(SamplingPlan plan, string niveau, string type, string severite,
        int niveauQualiteAcceptableId, int critereAcceptation, int critereRejet)
    {
        plan.InspectionLevelCode = niveau;
        plan.SamplingType = type;
        plan.InspectionSeverity = severite;
        plan.AcceptableQualityLevelId = niveauQualiteAcceptableId;
        plan.AcceptanceNumber = critereAcceptation;
        plan.RejectionNumber = critereRejet;
    }
}

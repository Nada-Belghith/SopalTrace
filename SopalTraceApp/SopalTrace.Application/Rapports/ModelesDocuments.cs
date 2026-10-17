using SopalTrace.Application.DTOs.Rapports;

namespace SopalTrace.Application.Rapports;

/// <summary>Présentation du modèle qui met en forme un document (code de la règle et explication pour l'écran).</summary>
public static class ModelesDocuments
{
    public static ModeleDocumentDto? VersDto(ModeleResolu? modele) => modele == null ? null : new ModeleDocumentDto
    {
        VersionId = modele.Affectation.VersionId,
        CodeModele = modele.Affectation.CodeModele,
        NumeroVersion = modele.Affectation.NumeroVersion,
        Regle = modele.Regle switch
        {
            RegleModele.ConcuPourLaVersionDuFormulaire => "CONCU_POUR_LA_VERSION_DU_FORMULAIRE",
            RegleModele.HeriteDeLaVersionPrecedente => "HERITE_DE_LA_VERSION_PRECEDENTE",
            RegleModele.PremierModele => "PREMIER_MODELE",
            _ => "EN_VIGUEUR_A_LA_CREATION"
        },
        Explication = modele.Regle switch
        {
            RegleModele.ConcuPourLaVersionDuFormulaire => "Modèle rattaché à cette version du formulaire après la création du document.",
            RegleModele.HeriteDeLaVersionPrecedente => "Modèle de la version précédente du formulaire (mêmes colonnes), en vigueur à la création du document.",
            RegleModele.PremierModele => "Aucun modèle n'existait à la création du document : premier modèle rattaché à ce formulaire.",
            _ => "Modèle en vigueur à la création du document."
        }
    };

    /// <summary>Message affiché quand un document n'a pas de modèle (son PDF est refusé).</summary>
    public static string Message(ModeleManquant manque, string? formulaire) => manque switch
    {
        ModeleManquant.StructureModifiee =>
            $"Le formulaire {formulaire} a changé de colonnes : l'administrateur doit lui rattacher un modèle mis à jour (page Modèles Excel).",
        ModeleManquant.AucunModele => $"Aucun modèle n'est rattaché au formulaire {formulaire} : l'administrateur doit lui en rattacher un (page Modèles Excel).",
        _ => "Ce document n'a pas de formulaire et son type n'a pas de modèle par défaut."
    };

    public static string Code(ModeleManquant manque) => manque switch
    {
        ModeleManquant.StructureModifiee => "STRUCTURE_MODIFIEE",
        ModeleManquant.AucunModele => "AUCUN_MODELE",
        _ => "SANS_FORMULAIRE"
    };
}

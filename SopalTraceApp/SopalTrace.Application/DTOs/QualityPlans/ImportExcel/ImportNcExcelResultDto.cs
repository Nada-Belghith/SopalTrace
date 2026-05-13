using System.Collections.Generic;

namespace SopalTrace.Application.DTOs.QualityPlans.ImportExcel;

/// <summary>
/// Résultat du parsing Excel pour un Plan de Résultat de Contrôle de Poste (PlanNC).
/// Format réel du fichier Excel :
///   Ligne titre  : "Test de Non-conformité" (cellule fusionnée, ignorée)
///   Ligne en-tête: | N° | Machine / Banc d'essai | Désignation du défaut |
///   Données      : | 1  | MAS26                  | ABSENCE/MAUVAIS MONTAGE JOINT ANTI-FUITE |
/// </summary>
public class ImportNcExcelResultDto
{
    /// <summary>Code du poste détecté depuis l'entête du fichier.</summary>
    public string PosteCode { get; set; } = string.Empty;

    /// <summary>Nom/titre du document (ex: "Fiche de Contrôle - Poste 71").</summary>
    public string NomPlan { get; set; } = string.Empty;

    /// <summary>Liste des lignes défaut/machine importées.</summary>
    public List<ImportNcLigneDto> Lignes { get; set; } = new();

    public string Remarques { get; set; } = string.Empty;
}

public class ImportNcLigneDto
{
    /// <summary>Code machine tel que renseigné dans la colonne A du fichier.</summary>
    public string MachineCode { get; set; } = string.Empty;

    /// <summary>Désignation du défaut tel que renseigné dans la colonne B du fichier.</summary>
    public string LibelleDefaut { get; set; } = string.Empty;
}

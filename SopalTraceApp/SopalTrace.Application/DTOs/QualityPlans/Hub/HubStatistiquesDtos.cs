namespace SopalTrace.Application.DTOs.QualityPlans.Hub;

/// <summary>Nombre de documents par statut (brouillons, actifs, archivés).</summary>
public record CompteStatutsDto(int Brouillons, int Actifs, int Archives)
{
    public int Total => Brouillons + Actifs + Archives;
}

/// <summary>Répartition pour un type de document (plan d'assemblage, plan produit fini…).</summary>
public record StatistiquesTypeDocumentDto(string TypeDocumentCode, string Libelle, string Portee, CompteStatutsDto Comptes);

/// <summary>Tableau de bord du superviseur qualité : documents génériques et spécifiques, puis détail par type.</summary>
public record HubStatistiquesDto(CompteStatutsDto Generiques, CompteStatutsDto Specifiques, List<StatistiquesTypeDocumentDto> ParType);

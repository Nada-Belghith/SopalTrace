using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SopalTrace.Domain.Entities;

namespace SopalTrace.Application.Interfaces.Repositories;

/// <summary>Plans de fabrication : Document_Entete de type PLAN_FAB, portée SPECIFIQUE (par code article Sage versionné).</summary>
public interface IPlanFabricationEnteteRepository
{
    Task<DocumentEntete?> GetByIdAsync(Guid id, bool includeRelations = false);
    Task<IEnumerable<DocumentEntete>> GetAllAsync(bool includeRelations = false);
    Task AddAsync(DocumentEntete document);
    Task UpdateAsync(DocumentEntete document);
    Task DeleteAsync(DocumentEntete document);

    void RemoveSection(DocumentSection section);
    void RemoveLigne(DocumentLigne ligne);
    void RemoveExtraColonne(DocumentLigneExtraColonne extraColonne);
    /// <summary>Insert une nouvelle section directement dans le DbSet (EntityState.Added garanti).</summary>
    void AddSection(DocumentSection section);

    Task<IEnumerable<DocumentEntete>> GetByFormulaireIdAsync(Guid formulaireId);

    Task<DocumentEntete?> GetActifByReferenceAsync(string typeDocumentCode, string codeArticleSageVersionne, string? operationCode = null);
    
    Task<IEnumerable<DocumentEntete>> GetByFiltersAsync(
        string? operationCode = null, 
        string? statut = null);

    Task<int> GetLatestVersionAsync(
        string codeArticleSageVersionne, 
        string? operationCode = null);
}

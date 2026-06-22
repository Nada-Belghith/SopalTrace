using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SopalTrace.Domain.Entities;

namespace SopalTrace.Application.Interfaces.Repositories;

public interface IDocumentEnteteRepository
{
    // CRUD de base
    Task<DocumentEntete?> GetByIdAsync(Guid id, bool includeRelations = false);
    Task<IEnumerable<DocumentEntete>> GetAllAsync(bool includeRelations = false);
    Task AddAsync(DocumentEntete document);
    Task UpdateAsync(DocumentEntete document);
    Task DeleteAsync(DocumentEntete document);

    void RemoveSection(DocumentSection section);
    void RemoveLigne(DocumentLigne ligne);
    void RemoveExtraColonne(DocumentLigneExtraColonne ec);



    // Recherches spécifiques métier (ex-PlanFabrication, PlanAssemblage, etc.)
    Task<DocumentEntete?> GetActifByReferenceAsync(string typeDocumentCode, string nom, string? operationCode = null, string? posteCode = null);
    
    Task<IEnumerable<DocumentEntete>> GetByFiltersAsync(
        string typeDocumentCode, 
        string? natureComposantCode = null, 
        string? operationCode = null, 
        string? posteCode = null, 
        string? familleProduitCode = null,
        string? statut = null);

    Task<bool> ExistsByNomAndDesignationAsync(string nom, string designation, string typeDocumentCode);
    
    // Pour la gestion des versions
    Task<int> GetLatestVersionAsync(
        string typeDocumentCode, 
        string nom, 
        string? operationCode = null,
        string? posteCode = null,
        string? natureComposantCode = null,
        string? familleProduitCode = null);
}

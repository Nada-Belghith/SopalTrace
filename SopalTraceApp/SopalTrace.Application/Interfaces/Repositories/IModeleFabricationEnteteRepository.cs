using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SopalTrace.Domain.Entities;

namespace SopalTrace.Application.Interfaces.Repositories;

/// <summary>Modèles de fabrication : Document_Entete de type MODELE_FAB, portée GENERIQUE (Code = Nom).</summary>
public interface IModeleFabricationEnteteRepository
{
    Task<DocumentEntete?> GetByIdAsync(Guid id, bool includeRelations = false);
    Task<IEnumerable<DocumentEntete>> GetAllAsync(bool includeRelations = false);
    Task AddAsync(DocumentEntete document);
    Task UpdateAsync(DocumentEntete document);
    Task DeleteAsync(DocumentEntete document);

    void RemoveSection(DocumentSection section);
    void RemoveLigne(DocumentLigne ligne);
    void RemoveExtraColonne(DocumentLigneExtraColonne extraColonne);

    Task<IEnumerable<DocumentEntete>> GetByFormulaireIdAsync(Guid formulaireId);

    Task<DocumentEntete?> GetActifByReferenceAsync(string typeDocumentCode, string code, string? operationCode = null);
    
    Task<IEnumerable<DocumentEntete>> GetByFiltersAsync(
        string? natureComposantCode = null, 
        string? operationCode = null, 
        string? familleProduitCode = null,
        string? statut = null);

    Task<int> GetLatestVersionAsync(
        string code, 
        string? operationCode = null,
        string? natureComposantCode = null,
        string? familleProduitCode = null);
}

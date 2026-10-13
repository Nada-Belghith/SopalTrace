using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SopalTrace.Domain.Entities;

namespace SopalTrace.Application.Interfaces.Execution;

public interface IOccurrenceRepository
{
    Task<ExecControleOf?> GetExecControleOfWithIntermediairesAsync(Guid execControleOfId);
    /// <summary>Session de l'équipe qui tient le poste (null s'il est libre).</summary>
    Task<Guid?> GetSessionIdDuPosteAsync(Guid execControleOfId, string posteCode);
    Task<List<DocumentSection>> GetSectionsActivesAsync(Guid planEnteteId);
    /// <summary>Sections ECHANTILLONNAGE du plan d'assemblage (DocumentSection) pour un OF de type ASS.</summary>
    Task<List<DocumentSection>> GetDocumentSectionsActivesAsync(Guid execControleOfId);
    Task<List<DocumentSection>> GetDocumentSectionsActivesParPosteAsync(Guid execControleOfId, string posteCode);
    /// <summary>Retourne l'effectif d'échantillonnage par heure (p/h) depuis ExecEchantillonnage pour un OF ASS.</summary>
    /// <summary>Lignes d'une DocumentSection (plan d'assemblage).</summary>
    Task<List<DocumentLigne>> GetDocumentLignesForSectionAsync(Guid sectionId);
    Task<List<ExecPrelevementIntermediaire>> GetIntermediairesActifsAsync(Guid execControleOfId);
    Task<List<ExecPrelevementIntermediaire>> GetIntermediairesParOfAsync(Guid execControleOfId);
    Task<ExecPrelevementIntermediaire?> GetIntermediaireAsync(Guid id);
    Task<List<ExecPrelevementIntermediaire>> GetIntermediairesParTrancheAsync(Guid execControleOfId, string trancheHoraire);
    Task<ExecControleTranche?> GetTrancheExistanteAsync(Guid execControleOfId, string trancheHoraire);
    Task<ExecControleTranche?> GetDerniereTrancheAsync(Guid execControleOfId);
    void AddIntermediaires(IEnumerable<ExecPrelevementIntermediaire> intermediaires);
    void AddTranche(ExecControleTranche tranche);
    void AddPieceType(ExecPieceType pieceType);

    void RemoveIntermediaires(IEnumerable<ExecPrelevementIntermediaire> intermediaires);
    Task<List<DocumentLigne>> GetLignesForSectionAsync(Guid sectionId);
    
    void AddLigneReponses(IEnumerable<ExecControleLigneReponse> reponses);
    Task<string> GetContexteOccurrenceAsync(Guid sectionId);

    Task SaveChangesAsync();
}

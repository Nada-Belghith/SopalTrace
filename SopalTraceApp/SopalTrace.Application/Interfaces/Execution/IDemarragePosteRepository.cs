using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SopalTrace.Application.Services.Execution;
using SopalTrace.Domain.Entities;

namespace SopalTrace.Application.Interfaces.Execution;

/// <summary>
/// Postes tenus par les sessions d'équipe (Exec_SessionPost) et sessions ouvertes de l'OF. Plusieurs équipes
/// peuvent travailler en même temps sur l'OF, une seule à la fois par poste : l'équipe d'un poste est celle qui
/// le tient (seule source de cette règle).
/// </summary>
public interface IDemarragePosteRepository
{
    /// <summary>Tous les postes tenus de l'OF, toutes sessions, avec l'équipe de chaque session.</summary>
    Task<List<DemarragePoste>> GetDemarragesAsync(Guid execControleOfId);

    /// <summary>La session tient le poste et ne l'a pas terminé.</summary>
    Task<bool> EstEnCoursAsync(Guid sessionId, string posteCode);

    /// <summary>Poste de la session (suivi), terminé ou non.</summary>
    Task<ExecSessionPost?> GetPosteSessionAsync(Guid sessionId, string posteCode);

    /// <summary>Postes que la session tient encore (suivis).</summary>
    Task<List<ExecSessionPost>> GetPostesEnCoursAsync(Guid sessionId);

    /// <summary>Sessions ouvertes (en production ou en pause) de l'OF (suivies).</summary>
    Task<List<ExecControleOfSession>> GetSessionsOuvertesAsync(Guid execControleOfId);

    /// <summary>
    /// Session d'équipe d'un poste (suivie) : celle de l'équipe donnée si elle est ouverte ; sinon celle qui tient
    /// le poste ; sinon celle qui clôture l'OF (elle fait le « fin d'OF » des postes qu'elle n'a pas démarrés).
    /// </summary>
    Task<ExecControleOfSession?> GetSessionAsync(Guid execControleOfId, string? posteCode, string? equipe = null);

    void Ajouter(ExecSessionPost poste);
    Task SaveChangesAsync();
}

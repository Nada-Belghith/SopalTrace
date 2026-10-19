using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using SopalTrace.Application.DTOs.DocumentsExternes;
using SopalTrace.Application.Interfaces;
using SopalTrace.Application.Settings;
using SopalTrace.Domain.Exceptions;

namespace SopalTrace.Application.Services;

public interface IRelanceDocumentsExternesService
{
    /// <summary>Prévient par e-mail le service qui dépose ce type de document qu'un document manque pour un OF.</summary>
    Task<RelanceDocumentExterneDto> RelancerAsync(RelanceDocumentExterneRequest demande);
}

/// <summary>
/// Relance d'un document externe manquant, demandée par le responsable qualité depuis la traçabilité d'un OF : e-mail
/// aux comptes actifs du service qui dépose ce type, avec le nom attendu et un lien vers la page de dépôt où le type et
/// les numéros sont déjà remplis. Un document n'est relancé que si ses numéros sont connus (il peut être désigné).
/// </summary>
public class RelanceDocumentsExternesService : IRelanceDocumentsExternesService
{
    private readonly IDocumentExterneRepository _depot;
    private readonly IUserRepository _utilisateurs;
    private readonly IEmailService _email;
    private readonly AuthSettings _auth;

    public RelanceDocumentsExternesService(IDocumentExterneRepository depot, IUserRepository utilisateurs, IEmailService email, AuthSettings auth)
    {
        _depot = depot;
        _utilisateurs = utilisateurs;
        _email = email;
        _auth = auth;
    }

    public async Task<RelanceDocumentExterneDto> RelancerAsync(RelanceDocumentExterneRequest demande)
    {
        var type = (await _depot.GetTypesAsync()).FirstOrDefault(t => t.Code == demande.TypeCode)
            ?? throw new RegleMetierException("Type de document externe inconnu.");
        var champs = DocumentExterneService.Champs(type);
        var numeros = (demande.Numeros ?? new()).ToDictionary(n => n.Key, n => n.Value?.Trim() ?? "");
        // Seuls les numéros de ce type. Il suffit d'en connaître un qui désigne le document : le service complète les
        // autres en déposant (ex. rapport de contrôle : n° lu sur l'étiquette, la commande fournisseur est dans le rapport).
        numeros = numeros.Where(n => n.Value.Length > 0).ToDictionary(n => n.Key, n => n.Value);
        if (numeros.Keys.Any(k => champs.All(c => c.Code != k)))
            throw new RegleMetierException("Numéro inattendu pour ce type de document.");
        if (numeros.Count == 0)
            throw new RegleMetierException("Ce document ne peut pas encore être désigné : aucun de ses numéros n'est connu.");
        // Nom formé comme par la base (référence client : espaces et « / » remplacés par « _ ») quand tous ses numéros sont connus.
        var nomAttendu = champs.Where(c => c.DansLeNom)
            .Select(c => numeros.GetValueOrDefault(c.Code) is { } v && c.Format == FormatsChamp.Reference ? v.Replace(' ', '_').Replace('/', '_') : numeros.GetValueOrDefault(c.Code))
            .ToList();

        var destinataires = await _utilisateurs.GetEmailsByRoleAsync(type.UploadRole);
        var service = type.UploadRole;
        if (destinataires.Count == 0)
            throw new RegleMetierException($"Aucun compte actif ne dépose les documents « {type.Label} » ({service}) : personne à prévenir.");

        var lien = Lien(type.Code, numeros);
        var lignes = champs.Where(c => numeros.ContainsKey(c.Code))
            .Select(c => $"<li>{WebUtility.HtmlEncode(c.Libelle)} : <b>{WebUtility.HtmlEncode(numeros[c.Code])}</b></li>");
        // Un BL finit par la référence client reprise de sa commande Sage : son nom complet est donné au dépôt.
        var nom = type.RequiresSalesOrderNumber || nomAttendu.Any(string.IsNullOrEmpty) ? null : string.Join("-", nomAttendu) + ".pdf";
        var sujet = $"SopalTrace — document manquant : {type.Label}" + (demande.NumeroOf is { Length: > 0 } of ? $" (OF {of})" : "");
        var corps = $"""
            <p>Bonjour,</p>
            <p>Le responsable qualité signale qu'un document <b>{WebUtility.HtmlEncode(type.Label)}</b> manque{(demande.NumeroOf is { Length: > 0 } o ? $" au dossier de l'OF <b>{WebUtility.HtmlEncode(o)}</b>" : "")}.</p>
            <ul>{string.Concat(lignes)}</ul>
            {(nom == null ? "" : $"<p>Nom attendu : <b>{WebUtility.HtmlEncode(nom)}</b></p>")}
            <p><a href="{WebUtility.HtmlEncode(lien)}">Déposer le document dans SopalTrace</a> (type et numéros déjà remplis).</p>
            """;
        // Une adresse refusée n'empêche pas les autres ; aucun envoi abouti : la relance n'a pas eu lieu.
        var envoyes = 0;
        foreach (var destinataire in destinataires)
        {
            try
            {
                await _email.EnvoyerAsync(destinataire, sujet, corps, isHtml: true);
                envoyes++;
            }
            catch (Exception)
            {
                // Erreur déjà journalisée par le service d'envoi.
            }
        }
        if (envoyes == 0)
            throw new RegleMetierException("La relance n'a pas pu être envoyée : serveur d'envoi des e-mails indisponible ou non configuré.");
        return new RelanceDocumentExterneDto(service, envoyes);
    }

    /// <summary>Page de dépôt avec le type et les numéros dans l'adresse (repris par la page).</summary>
    private string Lien(string typeCode, Dictionary<string, string> numeros) =>
        $"{_auth.FrontendBaseUrl.TrimEnd('/')}/documents-externes/depot?type={Uri.EscapeDataString(typeCode)}"
        + string.Concat(numeros.Select(n => $"&{Uri.EscapeDataString(n.Key)}={Uri.EscapeDataString(n.Value)}"));
}

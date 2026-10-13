using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using SopalTrace.Domain.Exceptions;

namespace SopalTrace.Infrastructure.Data;

/// <summary>
/// Traduit les erreurs SQL Server connues en exceptions métier (réponses HTTP 400/409),
/// sans jamais exposer le détail technique au client.
/// </summary>
public static class DbUpdateExceptionExtensions
{
    /// <summary>Unicité métier portée par un trigger (voir PARTIE 18 du script 01).</summary>
    public const int ErreurUniciteMetier = 50001;

    /// <summary>Suppression ou modification interdite par un trigger (ISO 9001).</summary>
    public const int ErreurOperationInterdite = 50002;

    /// <summary>Donnée refusée par une règle métier portée par un trigger (ex. valeur de colonne invalide).</summary>
    public const int ErreurRegleMetier = 50003;

    /// <summary>Règles d'exécution portées par des triggers (50100 à 50199) : messages rédigés pour l'utilisateur.</summary>
    private const int PremiereErreurExecution = 50100;
    private const int DerniereErreurExecution = 50199;

    private const int ErreurIndexUnique = 2601;
    private const int ErreurContrainteUnique = 2627;
    private const int ErreurContrainteReference = 547;

    /// <summary>Index uniques dont la violation a un sens métier précis (deux tablettes au même instant).</summary>
    private static readonly IReadOnlyDictionary<string, string> MessagesIndexUniques = new Dictionary<string, string>
    {
        ["UX_ExecSessionPost_OneTeamPerPoste"] = "Ce poste vient d'être pris par une autre équipe : rafraîchissez l'écran.",
        ["UX_ExecControleOfSession_OneOpenSessionPerTeam"] = "Votre équipe a déjà une session ouverte sur cet OF : rafraîchissez l'écran."
    };

    public static bool IsUniqueConstraintViolation(this DbUpdateException ex)
        => ex.InnerException is SqlException sqlEx && sqlEx.Number is ErreurIndexUnique or ErreurContrainteUnique or ErreurUniciteMetier;

    /// <summary>
    /// Les erreurs levées par les triggers gardent leur message (rédigé pour l'utilisateur) ;
    /// les autres violations d'unicité prennent le message fourni.
    /// </summary>
    public static Exception ToDomainExceptionOrSelf(this DbUpdateException ex, string details)
    {
        if (ex.InnerException is SqlException { Number: ErreurUniciteMetier or ErreurOperationInterdite or ErreurRegleMetier
                or (>= PremiereErreurExecution and <= DerniereErreurExecution) })
            return TraduireErreurBase(ex);

        return ex.IsUniqueConstraintViolation()
            ? new ConflitConcurrenceException(details)
            : ex;
    }

    /// <summary>Retourne l'exception métier correspondant à une erreur de base, ou l'exception d'origine.</summary>
    public static Exception TraduireErreurBase(Exception exception)
    {
        if (exception is DbUpdateConcurrencyException)
            return new ConflitConcurrenceException("Ces données viennent d'être modifiées par un autre utilisateur.");

        // SaveChanges enveloppe l'erreur SQL ; ExecuteUpdate/ExecuteDelete la lèvent directement.
        var sql = exception switch
        {
            DbUpdateException { InnerException: SqlException interne } => interne,
            SqlException directe => directe,
            _ => null
        };
        if (sql == null)
            return exception;

        return sql.Number switch
        {
            // Messages des triggers rédigés pour l'utilisateur : ils peuvent être renvoyés tels quels.
            ErreurUniciteMetier => new EtatExecutionInvalideException(sql.Message),
            ErreurOperationInterdite => new EtatExecutionInvalideException(sql.Message),
            ErreurRegleMetier => new RegleMetierException(sql.Message),
            >= PremiereErreurExecution and <= DerniereErreurExecution => new EtatExecutionInvalideException(sql.Message),
            ErreurIndexUnique or ErreurContrainteUnique => new ConflitConcurrenceException(
                MessagesIndexUniques.FirstOrDefault(m => sql.Message.Contains(m.Key, StringComparison.Ordinal)).Value
                ?? "Cette donnée existe déjà ou vient d'être enregistrée par un autre utilisateur."),
            ErreurContrainteReference => new RegleMetierException("Opération refusée : une donnée référencée est inconnue ou encore utilisée."),
            _ => exception
        };
    }
}

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

    private const int ErreurIndexUnique = 2601;
    private const int ErreurContrainteUnique = 2627;
    private const int ErreurContrainteReference = 547;

    public static bool IsUniqueConstraintViolation(this DbUpdateException ex)
        => ex.InnerException is SqlException sqlEx && sqlEx.Number is ErreurIndexUnique or ErreurContrainteUnique or ErreurUniciteMetier;

    /// <summary>
    /// Les erreurs levées par les triggers gardent leur message (rédigé pour l'utilisateur) ;
    /// les autres violations d'unicité prennent le message fourni.
    /// </summary>
    public static Exception ToDomainExceptionOrSelf(this DbUpdateException ex, string details)
    {
        if (ex.InnerException is SqlException { Number: ErreurUniciteMetier or ErreurOperationInterdite or ErreurRegleMetier })
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
            ErreurIndexUnique or ErreurContrainteUnique => new ConflitConcurrenceException("Cette donnée existe déjà ou vient d'être enregistrée par un autre utilisateur."),
            ErreurContrainteReference => new RegleMetierException("Opération refusée : une donnée référencée est inconnue ou encore utilisée."),
            _ => exception
        };
    }
}

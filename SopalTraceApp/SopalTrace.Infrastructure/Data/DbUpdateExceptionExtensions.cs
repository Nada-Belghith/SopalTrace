using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using SopalTrace.Domain.Exceptions;

namespace SopalTrace.Infrastructure.Data;

internal static class DbUpdateExceptionExtensions
{
    public static bool IsUniqueConstraintViolation(this DbUpdateException ex)
        => ex.InnerException is SqlException sqlEx && (sqlEx.Number == 2601 || sqlEx.Number == 2627);

    public static Exception ToDomainExceptionOrSelf(this DbUpdateException ex, string details)
        => ex.IsUniqueConstraintViolation()
            ? new ConflitConcurrenceException(details)
            : ex;
}
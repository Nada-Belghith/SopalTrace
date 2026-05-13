using FluentValidation;
using System;
using System.Threading.Tasks;

namespace SopalTrace.Application.Utilities;

/// <summary>
/// Helper pour valider les DTOs avec FluentValidation de manière cohérente.
/// </summary>
public static class ValidationHelper
{
    /// <summary>
    /// Valide un DTO et lève une ValidationException si invalide.
    /// </summary>
    /// <typeparam name="T">Type du DTO à valider</typeparam>
    /// <param name="validator">Validateur FluentValidation</param>
    /// <param name="dto">DTO à valider</param>
    /// <param name="operationName">Nom de l'opération (pour les logs)</param>
    /// <exception cref="ValidationException">Levée si la validation échoue</exception>
    public static async Task ValidateAndThrowAsync<T>(
        IValidator<T> validator,
        T dto,
        string operationName = "Operation")
    {
        var validationResult = await validator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(
                $"Validation failed for {operationName}: {string.Join("; ", validationResult.Errors)}",
                validationResult.Errors);
        }
    }
}

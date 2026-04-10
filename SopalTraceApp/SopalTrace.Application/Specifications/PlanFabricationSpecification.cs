using System;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;
using SopalTrace.Domain.Exceptions;

namespace SopalTrace.Application.Specifications;

/// <summary>
/// Centralise les règles métier pour les plans de fabrication
/// </summary>
public static class PlanFabricationSpecification
{
    public static void ValidateModeleDoesNotExist(bool modeleExists, string operationCode, string typeRobinetCode)
    {
        if (modeleExists)
            throw new DoublonModeleException();
    }

    public static void ValidatePlanDoesNotExist(bool planExists, string operationCode, string typeRobinetCode, string codeArticle)
    {
        if (planExists)
            throw new DoublonPlanException(codeArticle);
    }

    public static void ValidatePlanIsNotArchived(string statut, Guid planId)
    {
        if (statut == StatutsPlan.Archive)
            throw new PlanArchiveException();
    }
}
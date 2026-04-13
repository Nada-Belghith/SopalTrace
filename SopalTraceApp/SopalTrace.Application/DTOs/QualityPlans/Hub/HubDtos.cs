using System;

namespace SopalTrace.Application.DTOs.QualityPlans.Hub;

public record HubModeleDto(
    Guid Id,
    string Category,
    string Libelle,
    string Nature,
    string Type,
    string Poste,
    int Version,
    string Description
);
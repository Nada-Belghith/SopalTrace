using System;

namespace SopalTrace.Application.Interfaces;

public interface ICurrentUserService
{
    string? Matricule { get; }
    string? NomComplet { get; }
    string UserInfo { get; } // Retourne "Matricule - NomComplet"
}

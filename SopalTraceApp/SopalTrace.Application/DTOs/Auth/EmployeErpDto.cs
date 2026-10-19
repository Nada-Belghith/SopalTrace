namespace SopalTrace.Application.DTOs.Auth;

/// <summary>Employé lu dans l'annuaire ERP (AUTILIS). Email = adresse officielle, seule acceptée à l'inscription.</summary>
public record EmployeErpDto(string Matricule, string NomComplet, string CodeMetier, string IntituleMetier, bool EstActif, string? Email = null);

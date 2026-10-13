using SopalTrace.Application.DTOs.Auth;

namespace SopalTrace.Application.Interfaces;

public interface IErpService
{
    Task<EmployeErpDto?> GetEmployeByMatriculeAsync(string matricule);
    Task<List<string>> GetEmailsByRoleAsync(string roleCode);

    /// <summary>Nombre d'employés actifs de l'ERP portant cette adresse e-mail.</summary>
    Task<int> CountActiveEmployeesWithEmailAsync(string email);
}

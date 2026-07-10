using SopalTrace.Application.DTOs.Auth;

namespace SopalTrace.Application.Interfaces;

public interface IErpService
{
    Task<EmployeErpDto?> GetEmployeByMatriculeAsync(string matricule);
    Task<List<string>> GetEmailsByRoleAsync(string roleCode);
}

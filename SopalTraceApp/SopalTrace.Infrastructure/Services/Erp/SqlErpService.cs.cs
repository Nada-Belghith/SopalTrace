using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using SopalTrace.Application.DTOs.Auth;
using SopalTrace.Application.Interfaces;

namespace SopalTrace.Infrastructure.Services.Erp;

public class SqlErpService : IErpService
{
    private readonly string _connectionString;

    public SqlErpService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("SageErpConnection")
              ?? throw new InvalidOperationException("La chaîne de connexion 'SageErpConnection' est manquante dans la configuration.");
    }

    public async Task<EmployeErpDto?> GetEmployeByMatriculeAsync(string matricule)
    {
        using SqlConnection conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        string query = @"
            SELECT 
                u.USR_0 AS Matricule,
                u.INTUSR_0 AS NomComplet,
                u.CODMET_0 AS CodeMetier,
                CAST(u.ENAFLG_0 AS BIT) AS EstActif,
                t.TEXTE_0 AS IntituleMetier
            FROM AUTILIS u
            LEFT JOIN ATEXTRA t ON u.CODMET_0 = t.IDENT1_0 AND t.CODFIC_0 = 'APROFMET' AND t.LANGUE_0 = 'FRA'
            WHERE u.USR_0 = @Matricule";

        using SqlCommand cmd = new SqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@Matricule", matricule);

        using SqlDataReader reader = await cmd.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return new EmployeErpDto(
                Matricule: reader["Matricule"].ToString() ?? string.Empty,
                NomComplet: reader["NomComplet"].ToString() ?? string.Empty,
                CodeMetier: reader["CodeMetier"].ToString() ?? string.Empty,
                IntituleMetier: reader["IntituleMetier"] != DBNull.Value ? reader["IntituleMetier"].ToString() ?? string.Empty : string.Empty,
                EstActif: Convert.ToBoolean(reader["EstActif"])
            );
        }

        return null;
    }
}

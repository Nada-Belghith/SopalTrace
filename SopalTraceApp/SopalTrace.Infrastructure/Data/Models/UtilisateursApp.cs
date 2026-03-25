using System;
using System.Collections.Generic;

namespace SopalTrace.Infrastructure.Data.Models;

public partial class UtilisateursApp
{
    public Guid Id { get; set; }

    public string Matricule { get; set; } = null!;

    public string NomComplet { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string MotDePasseHash { get; set; } = null!;

    public string RoleApp { get; set; } = null!;

    public string? IntituleMetier { get; set; }

    public string? CodeRecuperation { get; set; }

    public DateTime? DateExpirationCode { get; set; }

    public DateTime? DateCreation { get; set; }

    public DateTime? DateDerniereConnexion { get; set; }

    public bool? EstActif { get; set; }

    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}

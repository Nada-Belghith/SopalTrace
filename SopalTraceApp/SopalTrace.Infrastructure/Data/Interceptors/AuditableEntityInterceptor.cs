using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.ValueGeneration;
using SopalTrace.Application.Interfaces;
using SopalTrace.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SopalTrace.Infrastructure.Data.Interceptors;

/// <summary>
/// Avant chaque enregistrement :
/// - attribue un identifiant GUID séquentiel aux nouvelles entités (index non fragmentés) ;
/// - renseigne l'auteur (matricule) et la date de création / modification ;
/// - écrit le journal d'audit (AuditLog) dans la même transaction que les données.
/// </summary>
public class AuditableEntityInterceptor : SaveChangesInterceptor
{
    private static readonly SequentialGuidValueGenerator GenerateurGuid = new();

    /// <summary>Tables tracées : documents qualité, exécution, magasin, formulaires et comptes.</summary>
    private static readonly string[] PrefixesAudites = { "Exec", "Document", "Mag", "RefFormulaire" };

    /// <summary>Jamais écrites dans le journal (secrets, contenu volumineux, colonnes techniques).</summary>
    private static readonly HashSet<string> ProprietesExclues = new()
    {
        "MotDePasseHash", "ActivationTokenHash", "CodeRecuperationHash", "TokenHash", "SecurityStamp", "Content", "RowVersion"
    };

    /// <summary>Recalculées en continu par le moteur d'occurrences : leur seule modification n'est pas un acte à tracer.</summary>
    /// Pour les comptes : compteur d'échecs, verrouillage et dernière connexion (déjà dans JournalConnexions).
    private static readonly HashSet<string> ProprietesTechniques = new()
    {
        "EstEnRetard", "TempsPauseTotalMinutes", "FailedLoginCount", "LockoutEndsAt", "DateDerniereConnexion"
    };

    private readonly ICurrentUserService _currentUserService;

    public AuditableEntityInterceptor(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        PreparerEnregistrement(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        PreparerEnregistrement(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void PreparerEnregistrement(DbContext? context)
    {
        if (context == null) return;

        context.ChangeTracker.DetectChanges();
        var entrees = context.ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        var matricule = _currentUserService.Matricule;
        var maintenant = DateTime.Now;
        var journal = new List<AuditLog>();

        foreach (var entree in entrees)
        {
            if (entree.State == EntityState.Added)
            {
                AttribuerIdentifiant(entree);
                Renseigner(entree, "CreePar", matricule);
                Renseigner(entree, "CreeLe", maintenant);
            }
            else if (entree.State == EntityState.Modified)
            {
                Renseigner(entree, "ModifiePar", matricule);
                Renseigner(entree, "ModifieLe", maintenant);
            }

            var ligne = CreerLigneAudit(entree, matricule, maintenant);
            if (ligne != null) journal.Add(ligne);
        }

        if (journal.Count > 0) context.AddRange(journal);
    }

    private static void AttribuerIdentifiant(EntityEntry entree)
    {
        var cle = entree.Metadata.FindPrimaryKey();
        if (cle?.Properties.Count != 1 || cle.Properties[0].ClrType != typeof(Guid)) return;

        var propriete = entree.Property(cle.Properties[0].Name);
        if (propriete.CurrentValue is Guid id && id == Guid.Empty)
            propriete.CurrentValue = GenerateurGuid.Next(entree);
    }

    /// <summary>Renseigne une colonne d'auteur ou de date si elle existe ; un auteur absent laisse la valeur fournie par le code.</summary>
    private static void Renseigner(EntityEntry entree, string nom, object? valeur)
    {
        if (valeur == null) return;
        var propriete = entree.Properties.FirstOrDefault(p => p.Metadata.Name == nom);
        if (propriete != null) propriete.CurrentValue = valeur;
    }

    private static AuditLog? CreerLigneAudit(EntityEntry entree, string? matricule, DateTime maintenant)
    {
        var nomEntite = entree.Metadata.ClrType.Name;
        bool audite = entree.Entity is UtilisateursApp || PrefixesAudites.Any(p => nomEntite.StartsWith(p, StringComparison.Ordinal));
        if (!audite) return null;

        // Les occurrences sont planifiées automatiquement : seules leurs réponses et suppressions sont tracées.
        if (entree.Entity is ExecPrelevementIntermediaire && entree.State == EntityState.Added) return null;

        var proprietes = entree.Properties.Where(p => !ProprietesExclues.Contains(p.Metadata.Name)).ToList();
        object changements;
        string action;

        switch (entree.State)
        {
            case EntityState.Added:
                action = "INSERT";
                changements = proprietes.ToDictionary(p => p.Metadata.Name, p => p.CurrentValue);
                break;
            case EntityState.Deleted:
                action = "DELETE";
                changements = proprietes.ToDictionary(p => p.Metadata.Name, p => p.OriginalValue);
                break;
            default:
                action = "UPDATE";
                var modifiees = proprietes
                    .Where(p => p.IsModified && !Equals(p.OriginalValue, p.CurrentValue))
                    .ToList();
                if (modifiees.All(p => ProprietesTechniques.Contains(p.Metadata.Name))) return null;
                changements = modifiees.ToDictionary(p => p.Metadata.Name, p => new { avant = p.OriginalValue, apres = p.CurrentValue });
                break;
        }

        var cle = entree.Metadata.FindPrimaryKey();
        var idEntite = cle == null ? "" : string.Join("|", cle.Properties.Select(p => entree.Property(p.Name).CurrentValue));

        return new AuditLog
        {
            TableName = entree.Metadata.GetTableName() ?? nomEntite,
            EntityId = idEntite,
            Action = action,
            Changes = JsonSerializer.Serialize(changements),
            UserMatricule = matricule,
            OccurredAt = maintenant
        };
    }
}

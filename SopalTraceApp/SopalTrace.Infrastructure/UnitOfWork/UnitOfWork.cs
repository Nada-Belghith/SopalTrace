using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SopalTrace.Application.Interfaces;
using SopalTrace.Infrastructure.Data;
using SopalTrace.Infrastructure.Repositories;
using SopalTrace.Application.Interfaces.Repositories;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SopalTrace.Infrastructure.UnitOfWork;

public class UnitOfWork : IUnitOfWork
{
    private readonly SopalTraceDbContext _context;
    private IDbContextTransaction? _transaction;

    private IDocumentEnteteRepository? _documentEnteteRepository;
    private IUserRepository? _userRepository;
    public UnitOfWork(SopalTraceDbContext context)
    {
        _context = context;
    }

    public IDocumentEnteteRepository DocumentEnteteRepository
        => _documentEnteteRepository ??= new DocumentEnteteRepository(_context);

    public IUserRepository UserRepository
        => _userRepository ??= new UserRepository(_context);

    private IDictionnaireQualiteRepository? _dictionnaireQualiteRepository;
    public IDictionnaireQualiteRepository DictionnaireQualiteRepository
        => _dictionnaireQualiteRepository ??= new DictionnaireQualiteRepository(_context);

    private IRefFormulaireRepository? _refFormulaireRepository;
    public IRefFormulaireRepository RefFormulaireRepository
        => _refFormulaireRepository ??= new RefFormulaireRepository(_context);

    private IDocumentVerifMachineEnteteRepository? _documentVerifMachineEnteteRepository;
    public IDocumentVerifMachineEnteteRepository DocumentVerifMachineEnteteRepository
        => _documentVerifMachineEnteteRepository ??= new DocumentVerifMachineEnteteRepository(_context);

    private IDocumentEchantillonnageEnteteRepository? _DocumentEchantillonnageEnteteRepository;
    public IDocumentEchantillonnageEnteteRepository DocumentEchantillonnageEnteteRepository
        => _DocumentEchantillonnageEnteteRepository ??= new DocumentEchantillonnageEnteteRepository(_context);

    private IModeleFabricationEnteteRepository? _modeleFabricationEnteteRepository;
    public IModeleFabricationEnteteRepository ModeleFabricationEnteteRepository
        => _modeleFabricationEnteteRepository ??= new ModeleFabricationEnteteRepository(_context);

    private IPlanFabricationEnteteRepository? _planFabricationEnteteRepository;
    public IPlanFabricationEnteteRepository PlanFabricationEnteteRepository
        => _planFabricationEnteteRepository ??= new PlanFabricationEnteteRepository(_context);

    private IAlerteRepository? _alerteRepository;
    public IAlerteRepository AlerteRepository
        => _alerteRepository ??= new AlerteRepository(_context);

    private IExecControleOfRepository? _execControleOfRepository;
    public IExecControleOfRepository ExecControleOfRepository
        => _execControleOfRepository ??= new ExecControleOfRepository(_context);

    private IExecEchantillonnageRepository? _execEchantillonnageRepository;
    public IExecEchantillonnageRepository ExecEchantillonnageRepository
        => _execEchantillonnageRepository ??= new ExecEchantillonnageRepository(_context);
    public async Task BeginTransactionAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task<int> CommitAsync()
    {
        try
        {
            var result = await _context.SaveChangesAsync();
            if (_transaction != null)
            {
                await _transaction.CommitAsync();
            }

            return result;
        }
        catch (DbUpdateException ex)
        {
            // Log details to a file for diagnosis
            try
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"[CommitAsync] DbUpdateException at {DateTime.UtcNow:O}");
                sb.AppendLine($"  Type: {ex.GetType().FullName}");
                sb.AppendLine($"  Message: {ex.Message}");
                if (ex.InnerException != null)
                    sb.AppendLine($"  InnerException: {ex.InnerException.Message}");
                var entriesProperty = ex.GetType().GetProperty("Entries");
                if (entriesProperty != null)
                {
                    var entries = entriesProperty.GetValue(ex) as System.Collections.IEnumerable;
                    if (entries != null)
                    {
                        foreach (var entry in entries)
                        {
                            var entityProp = entry.GetType().GetProperty("Entity");
                            var stateProp = entry.GetType().GetProperty("State");
                            var entity = entityProp?.GetValue(entry);
                            var state = stateProp?.GetValue(entry);
                            // Try to get Id
                            var idProp = entity?.GetType().GetProperty("Id");
                            var id = idProp?.GetValue(entity);
                            sb.AppendLine($"  Entry: {entity?.GetType().Name}, State={state}, Id={id}");
                        }
                    }
                }
                var logPath = @"c:\Users\LAPTOP\OneDrive - Ministere de l'Enseignement Superieur et de la Recherche Scientifique\Bureau\pfe\SopalTraceApp\SopalTraceApp\concurrency_debug.log";
                System.IO.File.WriteAllText(logPath, sb.ToString());
            }
            catch { /* ignore logging errors */ }

            await RollbackAsync();
            throw ex.ToDomainExceptionOrSelf("Un enregistrement concurrent a déjà été validé.");
        }
        catch
        {
            await RollbackAsync();
            throw;
        }
        finally
        {
            if (_transaction != null)
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }
    }

    /// <summary>Sauvegarde immédiate les changements tracked (ex: suppressions) sans fermer la transaction.</summary>
    public async Task<int> FlushDeletesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public void DetachAllEntities()
    {
        foreach (var entry in _context.ChangeTracker.Entries().ToList())
        {
            entry.State = EntityState.Detached;
        }
    }

    public async Task RollbackAsync()
    {
        try
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
            }
        }
        finally
        {
            if (_transaction != null)
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }
    }

    public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation)
    {
        var strategy = _context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var result = await operation();
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return result;
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();
                throw ex.ToDomainExceptionOrSelf("Conflit détecté pendant une opération transactionnelle.");
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        });
    }

    public async ValueTask DisposeAsync()
    {
        if (_transaction != null)
        {
            await _transaction.DisposeAsync();
        }

        await _context.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}

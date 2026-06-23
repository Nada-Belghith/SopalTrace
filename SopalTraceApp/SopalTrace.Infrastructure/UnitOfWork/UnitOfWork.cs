using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SopalTrace.Application.Interfaces;
using SopalTrace.Infrastructure.Data;
using SopalTrace.Infrastructure.Repositories;
using SopalTrace.Application.Interfaces.Repositories;
using System;
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

    private IPlanVerifMachineEnteteRepository? _planVerifMachineEnteteRepository;
    public IPlanVerifMachineEnteteRepository PlanVerifMachineEnteteRepository
        => _planVerifMachineEnteteRepository ??= new PlanVerifMachineEnteteRepository(_context);

    private IPlanEchantillonnageEnteteRepository? _planEchantillonnageEnteteRepository;
    public IPlanEchantillonnageEnteteRepository PlanEchantillonnageEnteteRepository
        => _planEchantillonnageEnteteRepository ??= new PlanEchantillonnageEnteteRepository(_context);

    private IModeleFabricationEnteteRepository? _modeleFabricationEnteteRepository;
    public IModeleFabricationEnteteRepository ModeleFabricationEnteteRepository
        => _modeleFabricationEnteteRepository ??= new ModeleFabricationEnteteRepository(_context);

    private IPlanFabricationEnteteRepository? _planFabricationEnteteRepository;
    public IPlanFabricationEnteteRepository PlanFabricationEnteteRepository
        => _planFabricationEnteteRepository ??= new PlanFabricationEnteteRepository(_context);

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

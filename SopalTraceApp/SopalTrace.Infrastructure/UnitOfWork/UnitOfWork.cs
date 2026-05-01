using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SopalTrace.Application.Interfaces;
using SopalTrace.Infrastructure.Data;
using SopalTrace.Infrastructure.Repositories;
using System;
using System.Threading.Tasks;

namespace SopalTrace.Infrastructure.UnitOfWork;

public class UnitOfWork : IUnitOfWork
{
    private readonly SopalTraceDbContext _context;
    private IDbContextTransaction? _transaction;

    private IPlanAssRepository? _planAssRepository;
    private IUserRepository? _userRepository;
    private IPlanNcRepository? _planNcRepository;
    private IPlanVerifMachineRepository? _planVerifMachineRepository;
    private IPlanEchanRepository? _planEchanRepository;


    public UnitOfWork(SopalTraceDbContext context)
    {
        _context = context;
    }

    public IPlanAssRepository PlanAssRepository
        => _planAssRepository ??= new PlanAssRepository(_context);

    public IUserRepository UserRepository
        => _userRepository ??= new UserRepository(_context);

    public IPlanNcRepository PlanNcRepository
        => _planNcRepository ??= new PlanNcRepository(_context);

    public IPlanVerifMachineRepository PlanVerifMachineRepository
        => _planVerifMachineRepository ??= new PlanVerifMachineRepository(_context);

    public IPlanEchanRepository PlanEchanRepository
        => _planEchanRepository ??= new PlanEchanRepository(_context);

    private IDictionnaireQualiteRepository? _dictionnaireQualiteRepository;
    public IDictionnaireQualiteRepository DictionnaireQualiteRepository
        => _dictionnaireQualiteRepository ??= new DictionnaireQualiteRepository(_context);

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
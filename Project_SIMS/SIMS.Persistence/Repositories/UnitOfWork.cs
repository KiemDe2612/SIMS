using System.Collections;
using Microsoft.Extensions.DependencyInjection;
using SIMS.Domain.Interfaces;
using SIMS.Persistence.Data;

namespace SIMS.Persistence.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private readonly IServiceProvider? _serviceProvider;
    private Hashtable? _repositories;

    public UnitOfWork(ApplicationDbContext context, IServiceProvider? serviceProvider = null)
    {
        _context = context;
        _serviceProvider = serviceProvider;
    }

    public IGenericRepository<T> Repository<T>() where T : class
    {
        _repositories ??= new Hashtable();

        var type = typeof(T).Name;

        if (!_repositories.ContainsKey(type))
        {
            var customRepo = _serviceProvider?.GetService<IGenericRepository<T>>();
            if (customRepo != null)
            {
                _repositories.Add(type, customRepo);
                return customRepo;
            }

            var repositoryType = typeof(GenericRepository<>);
            var repositoryInstance = Activator.CreateInstance(repositoryType.MakeGenericType(typeof(T)), _context);

            _repositories.Add(type, repositoryInstance);
        }

        return (IGenericRepository<T>)_repositories[type]!;
    }

    public async Task<int> CompleteAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}

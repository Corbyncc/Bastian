using System;
using System.Threading.Tasks;
using Bastian.API.Database;
using Microsoft.Extensions.DependencyInjection;

namespace Bastian.Database;
public partial class BastianDbRepository
{
    private readonly IServiceProvider _serviceProvider;

    public BastianDbRepository(
        IServiceProvider serviceProvider
    )
    {
        _serviceProvider = serviceProvider;
    }

    public async Task UpdateEntity<T>(T entity) where T : class
    {
        using var scope = _serviceProvider.CreateScope();
        var bastianDbProvider = scope.ServiceProvider.GetRequiredService<IBastianDbProvider>();
        await using var context = bastianDbProvider.GetDbContext();
        context.Set<T>().Update(entity);
        await context.SaveChangesAsync();
    }
}
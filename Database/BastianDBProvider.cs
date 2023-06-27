using Bastian.API.Database;
using Bastian.Framework.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace Bastian.Database;
[ServiceImplementation(Lifetime = ServiceLifetime.Scoped)]
public class BastianDbProvider : IBastianDbProvider
{
    private readonly ILogger<BastianDbProvider> _logger;
    private readonly IServiceProvider _serviceProvider;

    public BastianDbProvider(
        IServiceProvider serviceProvider
    )
    {
        _logger = serviceProvider.GetRequiredService<ILogger<BastianDbProvider>>();
        _serviceProvider = serviceProvider;
    }

    public BastianDbContext GetDbContext() =>
        _serviceProvider.GetRequiredService<BastianDbContext>();
}
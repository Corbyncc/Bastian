using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bastian.Database;
public class BastianDbContextFactory : IDesignTimeDbContextFactory<BastianDbContext>
{
    public BastianDbContext CreateDbContext(string[] args)
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(Environment.CurrentDirectory)
            .AddJsonFile("appsettings.json")
            .Build();

        var serviceProvider = new ServiceCollection()
            .AddDbContext<BastianDbContext>(options =>
                options.UseMySQL(configuration.GetConnectionString("Default")!))
            .BuildServiceProvider();

        return serviceProvider.GetRequiredService<BastianDbContext>();
    }
}
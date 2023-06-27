using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Bastian.Database;
using Bastian.Framework.Extensions;
using Discord.Interactions;
using Serilog.Events;
using Microsoft.Extensions.Logging;
using Bastian.API.Database;

namespace Bastian;
public class Program
{
    private static Serilog.ILogger clientLogger = null!;

    public static async Task Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();
        ConfigureLogger(host);

        using var scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;

        var bastianDbProvider = services.GetRequiredService<IBastianDbProvider>();

        await using var context = bastianDbProvider.GetDbContext();
        await context.Database.MigrateAsync();

        var client = services.GetRequiredService<DiscordSocketClient>();
        ConfigureClientLogger(client);

        var interactionHandler = services.GetRequiredService<InteractionHandler>();
        await interactionHandler.InitializeAsync();

        var configuration = services.GetRequiredService<IConfiguration>();
        var token = configuration["Token"];

        await client.LoginAsync(TokenType.Bot, token);
        await client.StartAsync();

        await host.WaitForShutdownAsync();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices(ConfigureServices);

    private static void ConfigureServices(HostBuilderContext hostContext, IServiceCollection services)
    {
        services.AddLogging(loggingBuilder =>
            loggingBuilder.ClearProviders()
                .AddSerilog(dispose: true));

        services.AddSingleton<DiscordSocketClient>();
        services.AddSingleton<InteractionHandler>();
        services.AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()));

        services.AddDbContext<BastianDbContext>(options =>
            options.UseMySQL(hostContext.Configuration.GetConnectionString("Default")!));


        services.RegisterServiceImplementations(Assembly.GetExecutingAssembly());
    }

    private static void ConfigureLogger(IHost host)
    {
        Log.Logger = new LoggerConfiguration()
            .ReadFrom
            .Configuration(host.Services.GetRequiredService<IConfiguration>())
            .CreateLogger();
    }

    private static void ConfigureClientLogger(DiscordSocketClient client)
    {
        clientLogger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();

        client.Log += async message =>
        {
            var severity = message.Severity switch
            {
                LogSeverity.Critical => LogEventLevel.Fatal,
                LogSeverity.Error => LogEventLevel.Error,
                LogSeverity.Warning => LogEventLevel.Warning,
                LogSeverity.Info => LogEventLevel.Information,
                LogSeverity.Verbose => LogEventLevel.Verbose,
                LogSeverity.Debug => LogEventLevel.Debug,
                _ => LogEventLevel.Information
            };
            clientLogger.Write(severity, message.Exception, "[{Source}] {Message}", message.Source, message.Message);
            await Task.CompletedTask;
        };
    }
}
using System.Threading.Tasks;
using Bastian.API.Database;
using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Logging;

namespace Bastian.Modules.Verification.Commands;

public class TestUserCommand : InteractionModuleBase<SocketInteractionContext>
{
    private readonly ILogger<TestUserCommand> _logger;
    private readonly IBastianDbProvider _bastianDbProvider;

    public TestUserCommand(ILogger<TestUserCommand> logger, IBastianDbProvider bastianDbProvider)
    {
        _logger = logger;
        _bastianDbProvider = bastianDbProvider;
    }

    [EnabledInDm(false)]
    [UserCommand("Test User Command")]
    public async Task Execute(IUser user)
    {
        _logger.LogDebug("Hello From TEST USER COMMAND");
    }
}

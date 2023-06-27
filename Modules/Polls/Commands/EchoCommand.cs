using System.Threading.Tasks;
using Discord.Interactions;
using Microsoft.Extensions.Logging;

namespace Bastian.Modules.Polls.Commands;
public class EchoCommand : InteractionModuleBase<SocketInteractionContext>
{
    private readonly ILogger<EchoCommand> _logger;

    public EchoCommand(
        ILogger<EchoCommand> logger
    )
    {
        _logger = logger;
    }

    [SlashCommand("echo", "Echo a message")]
    public async Task Echo(string echoMessage)
    {
        _logger.LogInformation($"Hello {echoMessage}");
        await ReplyAsync(echoMessage);
    }
}
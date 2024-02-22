using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Logging;

namespace Bastian.Modules.Verification.Commands;

public class SetupVerificationCommand : InteractionModuleBase<SocketInteractionContext>
{
    private readonly ILogger<SetupVerificationCommand> _logger;

    public SetupVerificationCommand(ILogger<SetupVerificationCommand> logger)
    {
        _logger = logger;
    }

    [SlashCommand("setupverification", "Create the embed to register verification xd rawr")]
    public async Task Execute()
    {
        // await DeferAsync(ephemeral: true);

        var embed = new EmbedBuilder()
        {
            Title = "Apply For A Role",
            Description =
                "To apply for a role, select the roles you qualify for below and submit any relevant links to your portfolio"
        };

        /* var embedMessage = await Context.Channel.SendMessageAsync( */
        /*     embed: embed.Build() */
        /*              components: component.Build() */
        /* ); */

        var components = new ComponentBuilder() { };

        await ReplyAsync(embed: embed.Build());

        await RespondAsync("Created verification embed", ephemeral: true);
    }
}

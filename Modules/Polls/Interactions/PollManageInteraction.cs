using System.Threading.Tasks;
using Bastian.API.Modules.Polls.Services;
using Bastian.Modules.Polls.Enums;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace Bastian.Modules.Polls.Interactions;
public class PollManageInteraction : InteractionModuleBase<SocketInteractionContext>
{
    ILogger<PollManageInteraction> _logger;
    IPollDbRepository _pollDbRepository;

    public PollManageInteraction(
        ILogger<PollManageInteraction> logger,
        IPollDbRepository pollDbRepository
    )
    {
        _logger = logger;
        _pollDbRepository = pollDbRepository;
    }

    [ComponentInteraction("managePollButton:*", ignoreGroupNames: true)]
    public async Task ManagePollButtonPressed(int pollId)
    {
        await DeferAsync(ephemeral: true);

        var poll = await _pollDbRepository.GetPollAsync(pollId);
        if (poll == null)
        {
            await FollowupAsync($"Error managing poll: Poll {pollId} not found.");
            return;
        }

        if (Context.User is not SocketGuildUser guildUser)
        {
            await FollowupAsync("User is not a guild user - this command should only be used within a guild.", ephemeral: true);
            return;
        }

        if (!guildUser.GuildPermissions.ManageEvents)
        {
            await FollowupAsync("You do not have permission to manage polls!", ephemeral: true);
            return;
        }

        var component = new ComponentBuilder();
        if (poll.Status == PollStatus.Opened)
        {
            component.WithButton("Add Option", $"pollAddOptionButton:{pollId}", ButtonStyle.Primary);
            component.WithButton("Remove Option", $"pollRemoveOptionButton:{pollId}", ButtonStyle.Primary);
            component.WithButton("Manage Allowed Roles", $"pollManageRolesButton:{pollId}", ButtonStyle.Primary);
            component.WithButton("View Results", $"pollViewResultsButton:{pollId}", ButtonStyle.Primary);
            component.WithButton("Close Poll", $"pollCloseButton:{pollId}", ButtonStyle.Danger);
        }
        else
        {
            component.WithButton("View Results", $"pollViewResultsButton:{pollId}", ButtonStyle.Primary);
        }

        var embed = new EmbedBuilder
        {
            Title = "Poll Manager",
            Description = "Manage this poll with the buttons below",
        };

        await FollowupAsync(ephemeral: true, components: component.Build(), embed: embed.Build());
    }
}
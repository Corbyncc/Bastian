using System.Threading.Tasks;
using Bastian.API.Database;
using Bastian.API.Modules.Polls.Services;
using Bastian.Modules.Polls.Entities;
using Bastian.Modules.Polls.Enums;
using Bastian.Utils;
using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Bastian.Modules.Polls.Commands;

public class PollCommand : InteractionModuleBase<SocketInteractionContext>
{
    private readonly ILogger<PollCommand> _logger;
    private readonly IPollManager _pollManager;
    private readonly IBastianDbProvider _bastianDbProvider;

    public PollCommand(
        ILogger<PollCommand> logger,
        IPollManager pollManager,
        IBastianDbProvider bastianDbProvider
    )
    {
        _logger = logger;
        _pollManager = pollManager;
        _bastianDbProvider = bastianDbProvider;
    }

    [EnabledInDm(false)]
    [DefaultMemberPermissions(GuildPermission.ManageEvents)]
    [SlashCommand("poll", "Create a new poll")]
    public async Task Poll(
        string title,
        string question,
        // VoteType voteType,
        PollPrivacy privacy,
        string duration = "",
        string embedColor = "#7289da",
        int maxVotes = 1,
        bool uniqueVotes = true,
        bool viewResultsBeforeClose = false
    )
    {
        await DeferAsync(ephemeral: true);

        var guildId = (ulong)Context.Interaction.GuildId!;

        var pollTime = TimeConverter.GetEpochTimestamp(duration);

        var embed = new EmbedBuilder
        {
            Title = title,
            Description = question,
            Color = (Color)System.Drawing.ColorTranslator.FromHtml(embedColor)
        };

        embed.AddField("Status", PollStatus.Opened);
        if (!string.IsNullOrEmpty(duration))
        {
            embed.AddField("Closes In", $"<t:{pollTime}:R>");
        }

        try
        {
            var newPoll = new Poll
            {
                GuildId = guildId,
                ChannelId = Context.Channel.Id,
                Type = VoteType.Buttons,
                Privacy = privacy,
                CloseAt = string.IsNullOrEmpty(duration) ? 0 : pollTime,
                Status = PollStatus.Opened,
                MaxVotes = maxVotes,
                UniqueVotes = uniqueVotes,
                ViewResultsBeforeClose = viewResultsBeforeClose
            };

            await using var context = _bastianDbProvider.GetDbContext();

            await context.Polls.AddAsync(newPoll);

            await context.SaveChangesAsync();

            var component = new ComponentBuilder();
            if (privacy == PollPrivacy.Public && viewResultsBeforeClose)
            {
                component.WithButton(
                    "View Results",
                    $"pollViewResultsButton:{newPoll.Id}",
                    ButtonStyle.Primary
                );
            }
            component.WithButton(
                "Manage Poll",
                $"managePollButton:{newPoll.Id}",
                ButtonStyle.Secondary
            );

            var embedMessage = await Context.Channel.SendMessageAsync(
                embed: embed.Build(),
                components: component.Build()
            );

            newPoll.MessageId = embedMessage.Id;

            await context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(duration))
                await _pollManager.StartPollTimer(newPoll);
        }
        catch (DbUpdateException ex)
        {
            await FollowupAsync(
                $"Error creating poll, please contact the developer. {ex.Message}",
                ephemeral: true
            );
            return;
        }

        await FollowupAsync(
            "Created a poll. To manage the poll press the Manage Poll button.",
            ephemeral: true
        );
    }
}


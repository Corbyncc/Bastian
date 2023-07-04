using System;
using System.Linq;
using System.Threading.Tasks;
using Bastian.API.Database;
using Bastian.API.Modules.Polls.Services;
using Bastian.Modules.Polls.Enums;
using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;

namespace Bastian.Modules.Polls.Interactions;
public class PollRemoveOptionInteraction : InteractionModuleBase<SocketInteractionContext>
{
    private readonly IPollManager _pollManager;
    private readonly IBastianDbProvider _bastianDbProvider;

    public PollRemoveOptionInteraction(
        IPollManager pollManager,
        IBastianDbProvider bastianDbProvider
    )
    {
        _pollManager = pollManager;
        _bastianDbProvider = bastianDbProvider;
    }

    [ComponentInteraction("pollRemoveOptionButton:*", ignoreGroupNames: true)]
    public async Task PollRemoveOptionPressed(int pollId)
    {
        await DeferAsync(ephemeral: true);

        var guildId = (ulong)Context.Interaction.GuildId!;

        await using var context = _bastianDbProvider.GetDbContext();

        var pollOptions = await context.PollOptions
            .Where(p => p.Poll.GuildId == guildId && p.Poll.Id == pollId)
            .ToListAsync();

        if (pollOptions.Count < 1)
        {
            await FollowupAsync("Poll must have at least one option to remove.", ephemeral: true);
            return;
        }

        var removeOptionsMenu = new SelectMenuBuilder()
            .WithCustomId($"pollRemoveOptionMenu:{pollId}")
            .WithMinValues(0)
            .WithMaxValues(pollOptions.Count);

        foreach (var option in pollOptions)
        {
            Console.WriteLine($"ADding Option {option.Name}");
            removeOptionsMenu.AddOption(option.Name, option.Id.ToString());
        }

        var component = new ComponentBuilder()
            .WithSelectMenu(removeOptionsMenu);

        await FollowupAsync("Select options to remove", ephemeral: true, components: component.Build());
    }

    [ComponentInteraction(customId: "pollRemoveOptionMenu:*", ignoreGroupNames: true)]
    public async Task PollRemoveOptionMenu(int pollId, string[] selectedOptions)
    {
        await DeferAsync(ephemeral: true);

        var guildId = (ulong)Context.Interaction.GuildId!;

        await using var context = _bastianDbProvider.GetDbContext();

        var poll = await context.Polls
            .Include(p => p.Options)
            .FirstOrDefaultAsync(p => p.GuildId == guildId && p.Id == pollId);
        if (poll == null)
        {
            await FollowupAsync($"Failed to remove option from poll {pollId} poll not found", ephemeral: true);
            return;
        }

        if (poll.Status == PollStatus.Closed)
        {
            await FollowupAsync("This poll is closed.");
            return;
        }

        var optionsRemoved = 0;

        foreach (var option in selectedOptions)
        {
            var optionId = int.Parse(option);

            var optionToRemove = poll.Options.Find(o => o.Id == optionId);
            if (optionToRemove == null) continue;

            context.PollOptions.Remove(optionToRemove);

            optionsRemoved++;
        }

        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            await FollowupAsync($"Failed to remove options - please contact developer {ex.Message}", ephemeral: true);
            return;
        }

        var optionsUpdated = await _pollManager.TryRebuildPollOptionsAsync(poll);
        if (!optionsUpdated)
        {
            await FollowupAsync($"Error adding option to poll {pollId}");
            return;
        }

        await FollowupAsync($"Removed {optionsRemoved} options.", ephemeral: true);
    }
}
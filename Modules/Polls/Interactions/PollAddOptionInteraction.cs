using System.Threading.Tasks;
using Bastian.API.Database;
using Bastian.API.Modules.Polls.Services;
using Bastian.Modules.Polls.Entities;
using Bastian.Modules.Polls.Enums;
using Bastian.Modules.Polls.Modals;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Bastian.Modules.Polls.Interactions;
public class PollAddOptionInteraction : InteractionModuleBase<SocketInteractionContext>
{
    private readonly ILogger<PollAddOptionInteraction> _logger;
    private readonly IPollManager _pollManager;
    private readonly IBastianDbProvider _bastianDbProvider;
    private readonly IPollDbRepository _dbRepository;

    public PollAddOptionInteraction(
        ILogger<PollAddOptionInteraction> logger,
        IPollManager pollManager,
        IPollDbRepository dbRepository,
        IBastianDbProvider bastianDbProvider
    )
    {
        _logger = logger;
        _pollManager = pollManager;
        _dbRepository = dbRepository;
        _bastianDbProvider = bastianDbProvider;
    }

    [ComponentInteraction("pollAddOptionButton:*", ignoreGroupNames: true)]
    public async Task PollAddOptionPressed(int pollId)
        => await RespondWithModalAsync<PollAddOptionModal>($"pollAddOptionModal:{pollId}");

    [ModalInteraction("pollAddOptionModal:*", ignoreGroupNames: true)]
    public async Task PollAddOptionsModalSubmitted(int pollId, PollAddOptionModal modal)
    {
        await DeferAsync(ephemeral: true);

        var guildId = (ulong)Context.Interaction.GuildId!;

        var poll = await _dbRepository.GetPollAsync(
            pollId,
            poll =>
                poll.Include(p => p.Options));
        if (poll == null)
        {
            await FollowupAsync($"Failed to add option, poll with name {pollId} not found.", ephemeral: true);
            return;
        }

        if (poll.Status == PollStatus.Closed)
        {
            await FollowupAsync("This poll is closed.");
            return;
        }

        // We have to keep our "manage poll" button, 
        // so we can allow up to 24 options for a total of 25 buttons
        if (poll.Options.Count == 24)
        {
            await FollowupAsync("Maximum options count reached for this poll. Discord has a limit of 25 buttons.", ephemeral: true);
            return;
        }

        var newOption = new PollOption
        {
            Name = modal.Option,
            PollId = poll.Id
        };

        poll.Options.Add(newOption);

        try
        {
            await _dbRepository.UpdateEntity(poll);
        }
        catch (DbUpdateException ex)
        {
            await FollowupAsync($"Error adding option to poll, please contact the developer. {ex.Message}", ephemeral: true);
            return;
        }

        var optionsUpdated = await _pollManager.TryRebuildPollOptionsAsync(poll);
        if (!optionsUpdated)
        {
            await FollowupAsync($"Error adding option to poll {pollId}");
            return;
        }

        await FollowupAsync($"Added option {modal.Option} to poll {pollId}", ephemeral: true);
    }
}
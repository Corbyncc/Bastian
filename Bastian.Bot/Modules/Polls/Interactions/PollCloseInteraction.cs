using System.Threading.Tasks;
using Bastian.API.Database;
using Bastian.API.Modules.Polls.Services;
using Bastian.Modules.Polls.Enums;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace Bastian.Modules.Polls.Interactions;
public class PollCloseInteraction : InteractionModuleBase<SocketInteractionContext>
{
    ILogger<PollCloseInteraction> _logger;
    private readonly IPollManager _pollManager;
    private readonly IPollDbRepository _pollDbRepository;

    public PollCloseInteraction(
        ILogger<PollCloseInteraction> logger,
        IPollManager pollManager,
        IPollDbRepository pollDbRepository
    )
    {
        _logger = logger;
        _pollManager = pollManager;
        _pollDbRepository = pollDbRepository;
    }

    [ComponentInteraction("pollCloseButton:*", ignoreGroupNames: true)]
    public async Task PollClosePressed(int pollId)
    {
        await DeferAsync(ephemeral: true);

        var poll = await _pollDbRepository.GetPollAsync(pollId);
        if (poll == null)
        {
            _logger.LogError("Error closing poll: Poll {PollId} not found.", pollId);
            return;
        }

        if (poll.Status == PollStatus.Closed)
        {
            await FollowupAsync("This poll is closed.");
            return;
        }

        await _pollManager.ClosePoll(poll);

        await FollowupAsync("This poll has been closed.", ephemeral: true);
    }
}
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Bastian.API.Database;
using Bastian.API.Modules.Polls.Services;
using Bastian.Modules.Polls.Entities;
using Bastian.Modules.Polls.Enums;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;

namespace Bastian.Modules.Polls.Interactions;
public class PollViewResultsInteraction : InteractionModuleBase<SocketInteractionContext>
{
    private readonly IPollManager _pollManager;
    private readonly IPollDbRepository _pollDbRepository;

    public PollViewResultsInteraction(
        IPollManager pollManager,
        IPollDbRepository pollDbRepository
    )
    {
        _pollManager = pollManager;
        _pollDbRepository = pollDbRepository;
    }

    [ComponentInteraction("pollViewResultsButton:*", ignoreGroupNames: true)]
    public async Task PollViewResultsPressed(int pollId)
    {
        await DeferAsync(ephemeral: true);

        var poll = await _pollDbRepository.GetPollAsync(
            pollId,
            poll =>
                poll
                .Include(p => p.Options)
                    .ThenInclude(o => o.Votes)
                .Include(p => p.Votes));
        if (poll == null)
        {
            await FollowupAsync("Failed to generate poll results, poll not found");
            return;
        }

        var (success, filePath) = await _pollManager.GenerateChartAsync(poll);
        if (!success)
        {
            await FollowupAsync("Failed to generate results - likely no votes.", ephemeral: true);
            return;
        }

        using var fileStream = new FileStream(filePath, FileMode.Open);

        await FollowupWithFileAsync(fileStream, $"PollResults-{poll.Id}.png", ephemeral: true);
    }
}
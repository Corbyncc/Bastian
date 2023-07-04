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
public class PollVoteInteraction : InteractionModuleBase<SocketInteractionContext>
{
    private readonly IPollManager _pollManager;
    private readonly IBastianDbProvider _bastianDbProvider;

    public PollVoteInteraction(
        IPollManager pollManager,
        IBastianDbProvider bastianDbProvider
    )
    {
        _pollManager = pollManager;
        _bastianDbProvider = bastianDbProvider;
    }

    [ComponentInteraction("*:Option:*", ignoreGroupNames: true)]
    public async Task PollOptionPressed(int pollId, int optionId)
    {
        await DeferAsync(ephemeral: true);

        var guildId = (ulong)Context.Interaction.GuildId!;

        await using var context = _bastianDbProvider.GetDbContext();

        var poll = await context.Polls
            .Include(p => p.Options)
            .Include(p => p.Votes)
            .Include(p => p.AllowedRoles)
            .FirstOrDefaultAsync(p => p.GuildId == guildId && p.Id == pollId);
        if (poll == null)
        {
            await FollowupAsync("Failed to find poll", ephemeral: true);
            return;
        }

        if (poll.Status == PollStatus.Closed)
        {
            await FollowupAsync("This poll is closed.");
            return;
        }

        if (Context.User is not SocketGuildUser guildUser)
        {
            await FollowupAsync("Failed to submit vote - guild user not found", ephemeral: true);
            return;
        }

        var userRoleIds = guildUser.Roles.Select(x => x.Id);
        // var allowedRoleIds = poll.AllowedRoles.Select(r => r.RoleId);

        // var hasAllowedRole = userRoleIds
        //     .Intersect(allowedRoleIds)
        //     .Any();

        // if (!hasAllowedRole)
        // {
        //     await FollowupAsync("You do not have the required role to vote on this poll.", ephemeral: true);
        //     return;
        // }

        var userVotes = poll.Votes
            .Where(v => v.UserId == guildUser.Id)
            .ToList();

        if (poll.MaxVotes > 0 && userVotes.Count >= poll.MaxVotes)
        {
            await FollowupAsync("You have reached the maximum number of allowed votes on this poll.", ephemeral: true);
            return;
        }

        var hasVoted = userVotes.Any(v => v.OptionId == optionId);
        var votesRemaining = poll.MaxVotes - userVotes.Count;

        if (poll.UniqueVotes && hasVoted)
        {
            await FollowupAsync($"You cannot vote for this option more than once. You have {votesRemaining} votes remaining.", ephemeral: true);
            return;
        }

        poll.Votes.Add(new PollVote
        {
            UserId = Context.Interaction.User.Id,
            OptionId = optionId,
            PollId = poll.Id
        });

        var votedOption = poll.Options.Find(o => o.Id == optionId);
        if (votedOption == null)
        {
            await FollowupAsync("Failed to get vote option", ephemeral: true);
            return;
        }

        await context.SaveChangesAsync();

        await FollowupAsync($"You have voted for {votedOption.Name}", ephemeral: true);
    }
}
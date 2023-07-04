using System.Collections.Generic;
using Bastian.Modules.Polls.Enums;

#nullable disable

namespace Bastian.Modules.Polls.Entities;
public class Poll
{
    public int Id { get; set; }
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public ulong MessageId { get; set; }
    public VoteType Type { get; set; }
    public PollPrivacy Privacy { get; set; }
    public long CloseAt { get; set; } // Epoch timestamp representing the end of the poll
    public PollStatus Status { get; set; }
    public int MaxVotes { get; set; }
    public bool UniqueVotes { get; set; }
    public bool ViewResultsBeforeClose { get; set; }
    public List<PollOption> Options { get; set; }
    public List<AllowedRole> AllowedRoles { get; set; }
    public List<PollVote> Votes { get; set; }
}
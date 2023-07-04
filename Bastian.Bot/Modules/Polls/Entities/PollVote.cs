#nullable disable

namespace Bastian.Modules.Polls.Entities;
public class PollVote
{
    public int Id { get; set; }
    public ulong UserId { get; set; }
    public int OptionId { get; set; }
    public PollOption PollOption { get; set; }
    public int PollId { get; set; }
    public Poll Poll { get; set; }
}
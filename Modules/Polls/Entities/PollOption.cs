#nullable disable

using System.Collections.Generic;

namespace Bastian.Modules.Polls.Entities;
public class PollOption
{
    public int Id { get; set; }
    public string Name { get; set; }
    public List<PollVote> Votes { get; set; }
    public int PollId { get; set; }
    public Poll Poll { get; set; }
}
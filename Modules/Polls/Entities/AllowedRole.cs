namespace Bastian.Modules.Polls.Entities;

#nullable disable

public class AllowedRole
{
    public int Id { get; set; }
    public ulong RoleId { get; set; }
    public int PollId { get; set; }
    public Poll Poll { get; set; }
}
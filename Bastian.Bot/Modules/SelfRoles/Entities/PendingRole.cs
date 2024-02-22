namespace Bastian.Modules.SelfRoles.Entities;

public class PendingRole
{
    public int Id { get; set; }
    public ulong GuildId { get; set; }
    public ulong RoleId { get; set; }
    public ulong UserId { get; set; }
}

#nullable disable

namespace Bastian.Modules.SelfRoles.Entities;

public class SelfRole
{
    public int Id { get; set; }
    public ulong GuildId { get; set; }
    public ulong RoleId { get; set; }
    public bool RequiresVerification { get; set; }
}


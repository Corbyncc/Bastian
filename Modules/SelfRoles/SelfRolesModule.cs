using Bastian.Database;
using Bastian.Modules.SelfRoles.Entities;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace Bastian.Modules.SelfRoles;
[EnabledInDm(false)]
[DefaultMemberPermissions(GuildPermission.ManageRoles)]
[Group("selfroles", "Manage the self roles module")]
public class SelfRolesModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly BastianDbContext _dbContext;

    public SelfRolesModule(BastianDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [SlashCommand("menu", "Create a self role selection menu.")]
    public async Task MenuCommand()
    {
        var component = new ComponentBuilder();
        component.WithButton("Manage Roles", "selfRolesButton", ButtonStyle.Primary);

        var embed = new EmbedBuilder
        {
            Title = "Self Roles",
            Description = "To manage your self roles, click the button below.",
            Color = Color.Teal
        };

        await Context.Channel.SendMessageAsync(embed: embed.Build(), components: component.Build());

        await RespondAsync("Created self roles menu.", ephemeral: true);
    }

    [SlashCommand(name: "add", description: "Add a role to the self roles list.")]
    public async Task AddCommand(IRole role, bool giveToEveryone)
    {
        await DeferAsync(true);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            var guildId = Context.Interaction.GuildId.GetValueOrDefault();

            var selfRoleExists = await _dbContext.SelfRoles.AnyAsync(r => r.GuildId == guildId && r.RoleId == role.Id);
            if (selfRoleExists)
            {
                await FollowupAsync($"Role {role.Name} is already a self role.");
                return;
            }

            await _dbContext.SelfRoles.AddAsync(new SelfRole
            {
                GuildId = guildId,
                RoleId = role.Id
            });

            await _dbContext.SaveChangesAsync();

            await transaction.CommitAsync();

            if (giveToEveryone)
            {
                var usersWithRole = Context.Guild.Users
                    .Where(u => u.Roles.Any(r => r.Id == role.Id))
                    .ToList();

                await Task.WhenAll(usersWithRole.Select(u => u.AddRoleAsync(role.Id)));
            }

            await FollowupAsync($"Added role {role.Name} to self roles.", ephemeral: true);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    [SlashCommand(name: "remove", description: "Remove a role from the self roles list.")]
    public async Task RemoveCommand(IRole role, bool removeFromEveryone)
    {
        await DeferAsync(true);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            var guildId = (ulong)Context.Interaction.GuildId!;

            var roleToRemove = await _dbContext.SelfRoles.FirstOrDefaultAsync(r => r.RoleId == role.Id);
            if (roleToRemove == null)
            {
                await FollowupAsync($"Role {role.Name} is not a self role.");
                return;
            }

            _dbContext.SelfRoles.Remove(roleToRemove);

            await _dbContext.SaveChangesAsync();

            await transaction.CommitAsync();

            if (removeFromEveryone)
            {
                var usersWithRole = Context.Guild.Users
                    .Where(u => u.Roles.Any(r => r.Id == roleToRemove.RoleId))
                    .ToList();

                await Task.WhenAll(usersWithRole.Select(u => u.RemoveRoleAsync(roleToRemove.RoleId)));
            }

            await FollowupAsync($"Removed role {role.Name} from self roles.", ephemeral: true);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    [ComponentInteraction("selfRolesButton", ignoreGroupNames: true)]
    public async Task SelfRolesButtonPressed()
    {
        await DeferAsync(true);

        var selfRoles = await _dbContext.SelfRoles
            .Where(r => r.GuildId == Context.Interaction.GuildId.GetValueOrDefault())
            .ToListAsync();

        if (selfRoles.Count == 0)
        {
            await FollowupAsync("You have not added any self roles, use command /selfroles add <role>", ephemeral: true);
            return;
        }

        var menuBuilder = new SelectMenuBuilder()
            .WithCustomId("selfRolesMenu")
            .WithPlaceholder("Select your roles")
            .WithMinValues(0)
            .WithMaxValues(selfRoles.Count);

        foreach (var selfRole in selfRoles)
        {
            var role = Context.Guild.GetRole(selfRole.RoleId);
            menuBuilder.AddOption(role?.Name, role?.Id.ToString());
        }

        var builder = new ComponentBuilder()
            .WithSelectMenu(menuBuilder);

        await FollowupAsync(ephemeral: true, components: builder.Build());
    }

    [ComponentInteraction("selfRolesMenu", ignoreGroupNames: true)]
    public async Task SelfRolesMenu(string[] selectedRoles)
    {
        await DeferAsync(ephemeral: true);

        if (Context.Interaction.User is not SocketGuildUser user) return;

        var selfRoles = await _dbContext.SelfRoles
            .Where(r => r.GuildId == Context.Interaction.GuildId.GetValueOrDefault())
            .ToListAsync();

        var rolesToAdd = selfRoles
            .Where(selfRole =>
                !user.Roles.Any(r => r.Id == selfRole.RoleId) &&
                selectedRoles.Any(r => r == selfRole.RoleId.ToString()))
            .Select(selfRole => selfRole.RoleId)
            .ToList();

        var rolesToRemove = selfRoles
            .Where(selfRole =>
                user.Roles.Any(r => r.Id == selfRole.RoleId) &&
                !selectedRoles.Any(r => r == selfRole.RoleId.ToString()))
            .Select(selfRole => selfRole.RoleId)
            .ToList();

        await user.AddRolesAsync(rolesToAdd);
        await user.RemoveRolesAsync(rolesToRemove);
    }
}

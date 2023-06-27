using System.Linq;
using System.Threading.Tasks;
using Bastian.API.Database;
using Bastian.API.Modules.Polls.Services;
using Bastian.Modules.Polls.Entities;
using Bastian.Modules.Polls.Enums;
using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;

namespace Bastian.Modules.Polls.Interactions;
public class PollManageRolesInteraction : InteractionModuleBase<SocketInteractionContext>
{
    private readonly IPollManager _pollManager;
    private readonly IPollDbRepository _pollDbRepository;

    public PollManageRolesInteraction(
        IPollManager pollManager,
        IPollDbRepository pollDbRepository
    )
    {
        _pollManager = pollManager;
        _pollDbRepository = pollDbRepository;
    }

    [ComponentInteraction("pollManageRolesButton:*", ignoreGroupNames: true)]
    public async Task PollManageRolesPressed(int pollId)
    {
        await DeferAsync(ephemeral: true);

        var guildId = (ulong)Context.Interaction.GuildId!;

        var poll = await _pollDbRepository.GetPollAsync(
            pollId,
                poll =>
                    poll.Include(p => p.AllowedRoles));
        if (poll == null)
        {
            await FollowupAsync($"Error managing rolls: Poll {pollId} not found.");
            return;
        }

        if (poll.Status == PollStatus.Closed)
        {
            await FollowupAsync("This poll is closed.");
            return;
        }

        var roles = Context.Guild.Roles;

        var manageRolesMenu = new SelectMenuBuilder()
            .WithCustomId($"pollManageRolesMenu:{pollId}")
            .WithPlaceholder("Select Roles")
            .WithMinValues(0)
            .WithMaxValues(Context.Guild.Roles.Count);

        foreach (var role in Context.Guild.Roles)
        {
            manageRolesMenu.AddOption(
                label: role.Name,
                value: role.Id.ToString(),
                isDefault: poll.AllowedRoles.Any(r => r.RoleId == role.Id));
        }

        var component = new ComponentBuilder()
            .WithSelectMenu(manageRolesMenu);

        await FollowupAsync("Select which roles can vote on this poll.", ephemeral: true, components: component.Build());
    }

    [ComponentInteraction(customId: "pollManageRolesMenu:*", ignoreGroupNames: true)]
    public async Task PollManageRolesMenu(int pollId, string[]
    selectedRoles)
    {
        await DeferAsync(ephemeral: true);

        var guildId = (ulong)Context.Interaction.GuildId!;

        var poll = await _pollDbRepository.GetPollAsync(
            pollId,
                poll =>
                    poll.Include(p => p.AllowedRoles));
        if (poll == null)
        {
            await FollowupAsync($"Error managing rolls: Poll {pollId} not found.");
            return;
        }

        var rolesToRemove = poll.AllowedRoles.ExceptBy(selectedRoles, allowedRole => allowedRole.RoleId.ToString()).ToList();
        var rolesToAdd = selectedRoles.Except(poll.AllowedRoles.Select(role => role.RoleId.ToString())).ToList();

        poll.AllowedRoles.RemoveAll(role => rolesToRemove.Contains(role));

        poll.AllowedRoles.AddRange(rolesToAdd.Select(roleToAdd => new AllowedRole
        {
            RoleId = ulong.Parse(roleToAdd),
            PollId = poll.Id
        }));

        await _pollDbRepository.UpdateEntity(poll);

        await FollowupAsync("Updated allowed roles.", ephemeral: true);
    }
}
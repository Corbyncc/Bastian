using Bastian.API.Modules.Polls.Services;
using Bastian.Framework.Attributes;
using Bastian.Modules.Polls.Entities;
using Bastian.Modules.Polls.Enums;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

namespace Bastian.Modules.Polls.Services;
[ServiceImplementation(Lifetime = ServiceLifetime.Singleton)]
public class PollManager : IPollManager
{
    private readonly ILogger<PollManager> _logger;
    private readonly DiscordSocketClient _client;
    private readonly IPollDbRepository _pollDbRepository;

    private readonly List<Poll> _activePolls; // list of PollIds

    public PollManager(
        ILogger<PollManager> logger,
        DiscordSocketClient client,
        IPollDbRepository pollDbRepository
    )
    {
        _logger = logger;
        _client = client;
        _pollDbRepository = pollDbRepository;

        _activePolls = new();

        // _client.Ready += OnClientReady;
    }

    // private async Task OnClientReady()
    // {
    //     LoadActivePolls();
    // }

    public async Task LoadActivePolls()
    {
        var polls = await _pollDbRepository.GetAllPollsAsync();
        foreach (var poll in polls)
        {
            if (poll.CloseAt == 0 || poll.Status == PollStatus.Closed) continue;

            var timeRemaining = poll.CloseAt - DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (timeRemaining < 0)
            {
                await ClosePoll(poll);
            }
            else
            {
                await StartPollTimer(poll);
            }
        }
    }

    public async Task StartPollTimer(Poll poll)
    {
        if (!_activePolls.Contains(poll))
        {
            _activePolls.Add(poll);
        }

        var timeRemaining = poll.CloseAt - DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var timer = new Timer(timeRemaining * 1000)
        {
            AutoReset = false
        };

        timer.Elapsed += async (sender, e) =>
            await ClosePoll(poll);

        timer.Start();
    }

    public async Task ClosePoll(Poll poll)
    {
        var guild = _client.GetGuild(poll.GuildId);
        if (guild == null)
        {
            _logger.LogError("Error closing poll: Guild {GuildId} not found.", poll.GuildId);
            return;
        }

        if (_client.GetChannel(poll.ChannelId) is not ISocketMessageChannel channel)
        {
            _logger.LogError("Error closing poll: Channel {ChannelId} not found.", poll.ChannelId);
            return;
        }

        if (await channel.GetMessageAsync(poll.MessageId) is not IUserMessage message)
        {
            _logger.LogError("Error closing poll: Message {MessageId} not found.", poll.MessageId);
            return;
        }

        var embed = message.Embeds.FirstOrDefault();
        if (embed == null)
        {
            _logger.LogError("Error closing poll: Embed on message {MessageId} not found.", poll.MessageId);
            return;
        }

        if (_activePolls.Contains(poll))
            _activePolls.Remove(poll);

        poll.Status = PollStatus.Closed;
        await _pollDbRepository.UpdateEntity(poll);

        var componentBuilder = new ComponentBuilder();

        if (poll.Privacy == PollPrivacy.Public)
            componentBuilder.WithButton("View Results", $"pollViewResultsButton:{poll.Id}", ButtonStyle.Primary);

        componentBuilder.WithButton("Manage Poll", $"managePollButton:{poll.Id}", ButtonStyle.Secondary);

        var embedBuilder = new EmbedBuilder()
        {
            Title = embed.Title,
            Description = embed.Description,
            Color = embed.Color,
        };
        embedBuilder.AddField("Status", PollStatus.Closed);
        embedBuilder.AddField("Closed On", $"<t:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}:R>");

        await message.ModifyAsync(msg =>
        {
            msg.Components = componentBuilder.Build();
            msg.Embed = embedBuilder.Build();
        });
    }

    public async Task<bool> TryRebuildPollOptionsAsync(Poll poll)
    {
        if (_client.GetChannel(poll.ChannelId) is not ISocketMessageChannel channel)
        {
            _logger.LogError("Error rebuilding poll options: Channel {ChannelId} not found.", poll.ChannelId);
            return false;
        }

        if (await channel.GetMessageAsync(poll.MessageId) is not IUserMessage message)
        {
            _logger.LogError("Error rebuilding poll options: Message {MessageId} not found.", poll.MessageId);
            return false;
        }

        var newComponents = new ComponentBuilder();

        foreach (var option in poll.Options)
        {
            newComponents.WithButton(label: option.Name, customId: $"{poll.Id}:Option:{option.Id}");
        }

        if (poll.Privacy == PollPrivacy.Public && poll.ViewResultsBeforeClose)
        {
            newComponents.WithButton("View Results", $"pollViewResultsButton:{poll.Id}", ButtonStyle.Primary);
        }

        newComponents.WithButton("Manage Poll", $"managePollButton:{poll.Id}", ButtonStyle.Secondary);

        await message.ModifyAsync(m => m.Components = newComponents.Build());

        return true;
    }

    public async Task<(bool, string)> GenerateChartAsync(Poll poll)
    {
        if (poll.Votes.Count == 0)
        {
            return (false, string.Empty);
        }

        var plt = new ScottPlot.Plot(600, 300);

        // plt.Style(
        //     figureBackground: System.Drawing.Color.FromArgb(0x40444B),
        //     dataBackground: System.Drawing.Color.FromArgb(0x40444B),
        //     grid: System.Drawing.Color.FromArgb(0x40444B),
        //     tick: System.Drawing.Color.FromArgb(0xffffff));

        if (poll.Options == null)
        {
            _logger.LogInformation("Failed to get poll options");
            return (false, string.Empty);
        }

        if (poll.Votes == null)
        {
            _logger.LogInformation("Failed to get poll votes");
            return (false, string.Empty);
        }

        foreach (var option in poll.Options)
        {
            if (option.Votes == null)
            {
                _logger.LogInformation("Failed to get votes for option");
                return (false, string.Empty);
            }
        }

        double[] values = poll.Options
            .Select(option => (double)option.Votes.Count)
            .ToArray();

        string[] labels = poll.Options.Select(o => o.Name).ToArray();

        double[] positions = labels
            .Select((_, index) => (double)index)
            .ToArray();

        // add a bar graph to the plot
        var bar = plt.AddBar(values);
        bar.ShowValuesAboveBars = true;
        // bar.Font.Color = System.Drawing.Color.FromArgb(0xffffff);

        plt.XTicks(positions, labels);

        // adjust axis limits so there is no padding below the bar graph
        plt.SetAxisLimits(yMin: 0);

        string workingDirectory = Environment.CurrentDirectory;

        string projectDirectory = Directory.GetParent(workingDirectory)!.Parent!.Parent!.FullName;

        string pollResultsDirectory = projectDirectory + "/PollResults";

        if (!Directory.Exists(pollResultsDirectory))
            Directory.CreateDirectory(pollResultsDirectory);

        string filePath = $"{pollResultsDirectory}/PollResults-{poll.Id}.png";

        await Task.Run(() => plt.SaveFig(filePath));

        return (true, filePath);
    }
}
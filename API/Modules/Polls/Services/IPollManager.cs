using System.Threading.Tasks;
using Bastian.Framework.Attributes;
using Bastian.Modules.Polls.Entities;

namespace Bastian.API.Modules.Polls.Services;
[Service]
public interface IPollManager
{
    Task StartPollTimer(Poll poll);
    Task ClosePoll(Poll poll);
    Task LoadActivePolls();
    Task<bool> TryRebuildPollOptionsAsync(Poll poll);
    Task<(bool, string)> GenerateChartAsync(Poll poll);
}
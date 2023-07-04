using Bastian.API.Database;
using Bastian.API.Modules.Polls.Services;
using Bastian.Database;
using Bastian.Framework.Attributes;
using Bastian.Modules.Polls.Entities;
using Bastian.Modules.Polls.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bastian.Modules.Polls.Services;
[ServiceImplementation(Lifetime = ServiceLifetime.Transient)]
public class PollDbRepository : BastianDbRepository, IPollDbRepository
{
    private readonly ILogger<PollDbRepository> _logger;
    private readonly IBastianDbProvider _bastianDbProvider;

    public PollDbRepository(
        ILogger<PollDbRepository> logger,
        IBastianDbProvider bastianDbProvider,
        IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _bastianDbProvider = bastianDbProvider;
        _logger = logger;
    }

    public async Task<List<Poll>> GetAllPollsAsync(Func<IQueryable<Poll>, IQueryable<Poll>> predicate = null!)
    {
        await using var context = _bastianDbProvider.GetDbContext();
        var query = context.Polls.AsQueryable();

        if (predicate != null)
            query = predicate(query);

        return await query.ToListAsync();
    }

    public async Task<Poll?> GetPollAsync(int pollId, Func<IQueryable<Poll>, IQueryable<Poll>> predicate = null!)
    {
        await using var context = _bastianDbProvider.GetDbContext();
        var query = context.Polls.AsQueryable();

        if (predicate != null)
            query = predicate(query);

        return await query.FirstOrDefaultAsync(poll => poll.Id == pollId);
    }

    public async Task ClosePollAsync(int pollId)
    {
        await using var context = _bastianDbProvider.GetDbContext();

        var poll = await context.Polls.FirstOrDefaultAsync(poll => poll.Id == pollId);
        if (poll == null)
        {
            _logger.LogError("Error closing poll {PollId} poll not found.", pollId);
            return;
        }

        poll.Status = PollStatus.Closed;

        await context.SaveChangesAsync();
    }
}
using Bastian.Framework.Attributes;
using Bastian.Modules.Polls.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bastian.API.Modules.Polls.Services;
[Service]
public interface IPollDbRepository
{
    Task<List<Poll>> GetAllPollsAsync(Func<IQueryable<Poll>, IQueryable<Poll>> predicate = null!);
    Task<Poll?> GetPollAsync(int pollId, Func<IQueryable<Poll>, IQueryable<Poll>> predicate = null!);
    Task ClosePollAsync(int pollId);
    Task UpdateEntity<T>(T entity) where T : class;
}
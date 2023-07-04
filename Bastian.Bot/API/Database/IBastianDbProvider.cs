using Bastian.Database;
using Bastian.Framework.Attributes;

namespace Bastian.API.Database;
[Service]
public interface IBastianDbProvider
{
    public BastianDbContext GetDbContext();
}
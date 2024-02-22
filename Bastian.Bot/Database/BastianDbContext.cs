using Bastian.Modules.Polls.Entities;
using Bastian.Modules.SelfRoles.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bastian.Database;

public class BastianDbContext : DbContext
{
    public DbSet<SelfRole> SelfRoles { get; set; } = null!;

    public DbSet<PendingRole> PendingRoles { get; set; } = null!;

    public DbSet<Poll> Polls { get; set; } = null!;

    public DbSet<AllowedRole> AllowedRoles { get; set; } = null!;

    public DbSet<PollOption> PollOptions { get; set; } = null!;

    public DbSet<PollVote> PollVotes { get; set; } = null!;

    public BastianDbContext(DbContextOptions<BastianDbContext> options)
        : base(options) { }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseMySQL(o =>
            o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)
        );
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Poll>(entity =>
        {
            entity
                .HasMany(p => p.AllowedRoles)
                .WithOne(ar => ar.Poll)
                .HasForeignKey(ar => ar.PollId);

            entity.HasMany(p => p.Options).WithOne(o => o.Poll).HasForeignKey(o => o.PollId);

            entity.Property(p => p.Type).HasConversion<string>();

            entity.Property(p => p.Status).HasConversion<string>();

            entity.Property(p => p.Privacy).HasConversion<string>();

            entity.HasMany(p => p.Votes).WithOne(v => v.Poll).HasForeignKey(v => v.PollId);
        });

        modelBuilder.Entity<PollOption>(entity =>
        {
            entity
                .HasMany(po => po.Votes)
                .WithOne(v => v.PollOption)
                .HasForeignKey(v => v.OptionId);
        });
    }
}


using Microsoft.EntityFrameworkCore;

namespace ResumeAI.WebAPI.Data;

public class ResumeContext : DbContext
{
    public ResumeContext(DbContextOptions options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasPostgresExtension("vector");
    }

    public DbSet<ResumeEntity> Resume { get; set; }
}
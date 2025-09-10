using Microsoft.EntityFrameworkCore;

public class TelemetryDbContext : DbContext
{
    public TelemetryDbContext(DbContextOptions<TelemetryDbContext> options)
        : base(options)
    {
    }
    public DbSet<TelemetryData> Telemetry { get; set; }

}
using ForestFireDetection.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace ForestFireDetection.Data;

public class ForestFireDetectionDbContext : IdentityDbContext<ApplicationUser>
{
    public ForestFireDetectionDbContext(DbContextOptions<ForestFireDetectionDbContext> options)
        : base(options)
    {
    }

    public DbSet<Sensor> Sensors { get; set; }
    public DbSet<Alert> Alerts { get; set; }
    public DbSet<SensorData> SensorData { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Sensor>()
        .HasMany(s => s.DataHistory)
        .WithOne(d => d.Sensor)
        .HasForeignKey(d => d.SensorId)
        .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Alert>()
                .HasOne(a => a.Sensor)
                .WithMany(s => s.Alerts)
                .HasForeignKey(a => a.SensorId)
                .OnDelete(DeleteBehavior.Cascade);
    }

}
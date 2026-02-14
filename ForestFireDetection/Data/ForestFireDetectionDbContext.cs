using ForestFireDetection.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ForestFireDetection.Data
{
    public class ForestFireDetectionDbContext : IdentityDbContext<ApplicationUser>
    {
        public ForestFireDetectionDbContext(DbContextOptions<ForestFireDetectionDbContext> options)
            : base(options)
        {
        }

        public DbSet<Sensor> Sensors { get; set; }
        public DbSet<Alert> Alerts { get; set; }
        public DbSet<SensorData> SensorData { get; set; }
        public DbSet<SensorDataArchive> SensorDataArchive { get; set; }

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

            // Performance indexes
            builder.Entity<SensorData>()
                .HasIndex(d => new { d.SensorId, d.Timestamp })
                .HasDatabaseName("IX_SensorData_SensorId_Timestamp");

            builder.Entity<Alert>()
                .HasIndex(a => a.Status)
                .HasDatabaseName("IX_Alerts_Status");

            builder.Entity<SensorDataArchive>()
                .HasIndex(d => new { d.SensorId, d.Timestamp })
                .HasDatabaseName("IX_SensorDataArchive_SensorId_Timestamp");
        }
    }
}
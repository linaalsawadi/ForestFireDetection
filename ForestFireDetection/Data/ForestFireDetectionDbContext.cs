using ForestFireDetection.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

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
        // Customize the ASP.NET Identity model and override the defaults if needed.
        // For example, you can rename the ASP.NET Identity table names and more.
        // Add your customizations after calling base.OnModelCreating(builder);
    }
}
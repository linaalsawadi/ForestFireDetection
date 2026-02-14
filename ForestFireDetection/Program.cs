using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ForestFireDetection.Data;
using ForestFireDetection.Models;
using ForestFireDetection.Hubs;
using ForestFireDetection.Helpers;
using ForestFireDetection.Services;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var connectionString = builder.Configuration.GetConnectionString("ForestFireDetectionDbContextConnection")
            ?? throw new InvalidOperationException("Connection string 'ForestFireDetectionDbContextConnection' not found.");

        // Database
        builder.Services.AddDbContext<ForestFireDetectionDbContext>(options =>
            options.UseSqlServer(connectionString));

        // MVC + Razor Pages
        builder.Services.AddControllersWithViews();
        builder.Services.AddRazorPages();

        // Identity
        builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 8;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
        })
        .AddEntityFrameworkStores<ForestFireDetectionDbContext>()
        .AddDefaultTokenProviders();

        // Cookie configuration
        builder.Services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/Account/Login";
            options.AccessDeniedPath = "/Account/Login";
        });

        // Services
        builder.Services.AddMemoryCache();
        builder.Services.AddSession();
        builder.Services.AddScoped<SensorDataProcessor>();
        builder.Services.AddSingleton<FuzzyEngine>();
        builder.Services.AddHostedService<MqttService>();
        builder.Services.AddHostedService<SensorMonitorService>();
        builder.Services.AddHostedService<SignalRKeepAliveService>();
        builder.Services.AddHostedService<MonthlyArchivingService>();
        builder.Services.AddSignalR();

        // ── TEMPORARY: Simulated sensor data for testing (DELETE when done) ──
        builder.Services.AddHostedService<SimulatedSensorService>();

        var app = builder.Build();

        // Configure AES from configuration
        var aesConfig = app.Configuration.GetSection("AesEncryption");
        var keyHex = aesConfig["Key"];
        var ivHex = aesConfig["IV"];
        if (!string.IsNullOrEmpty(keyHex) && !string.IsNullOrEmpty(ivHex))
        {
            AESHelper.Configure(
                Convert.FromHexString(keyHex),
                Convert.FromHexString(ivHex));
        }

        // Seed data
        using (var scope = app.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ForestFireDetectionDbContext>();
            await context.Database.MigrateAsync();
        }
        await Seeds.SeedUsersAndRolesAsync(app);

        // Seed test data (only adds if no sensors exist)
        if (app.Environment.IsDevelopment())
        {
            await Seeds.SeedTestDataAsync(app);
        }

        // Pipeline
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();

        // SignalR
        app.MapHub<AlertHub>("/alertHub");
        app.MapHub<MapHub>("/mapHub");
        app.MapHub<ChartHub>("/chartHub");

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");
        app.MapRazorPages();

        app.Run();
    }
}

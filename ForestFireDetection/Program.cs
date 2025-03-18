using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ForestFireDetection.Data;
using ForestFireDetection.Models;
using Microsoft.AspNetCore.Authentication.Cookies;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var connectionString = builder.Configuration.GetConnectionString("ForestFireDetectionDbContextConnection") ?? throw new InvalidOperationException("Connection string 'ForestFireDetectionDbContextConnection' not found.");

        builder.Services.AddDbContext<ForestFireDetectionDbContext>(options => options.UseSqlServer(connectionString));

        // Add services to the container.
        builder.Services.AddControllersWithViews();
        builder.Services.AddRazorPages();


        // Add ASP.NET Core Identity services
        builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            // Password settings
            options.Password.RequireDigit = false;
            options.Password.RequiredLength = 6;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireLowercase = false;
        }).AddEntityFrameworkStores<ForestFireDetectionDbContext>();
        builder.Services.AddMemoryCache();
        builder.Services.AddSession();
        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
               .AddCookie();

        var app = builder.Build();

        var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        await Seeds.SeedUsersAndRolesAsync(app);

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
        }
        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();


        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");
        app.MapRazorPages();
        
        app.Run();
    }
}
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ForestFireDetection.Data;
using ForestFireDetection.Areas.Identity.Data;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var connectionString = builder.Configuration.GetConnectionString("ForestFireDetectionDbContextConnection") ?? throw new InvalidOperationException("Connection string 'ForestFireDetectionDbContextConnection' not found.");

        builder.Services.AddDbContext<ForestFireDetectionDbContext>(options => options.UseSqlServer(connectionString));

        builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ForestFireDetectionDbContext>();

        // Add services to the container.
        builder.Services.AddControllersWithViews();
        builder.Services.AddRazorPages();


        builder.Services.Configure<IdentityOptions>(options =>
        {
            options.Password.RequireUppercase = false;
        });

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
        }
        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthorization();

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");
        app.MapRazorPages();

        using (var scope = app.Services.CreateScope())
        {
            var roleManager =
                scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var roles = new[] { "Admin", "Manager", "Member" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));

            }
        }
        using (var scope = app.Services.CreateScope())
        {
            var userManager =
                scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            string email = "admin@admin.com";
            string password = "test1234@";

            if(await userManager.FindByEmailAsync(email)==null)
            {
                var user = new ApplicationUser();
                user.UserName = email;
                user.Email = email;
                user.EmailConfirmed =true ;
                

               await userManager.CreateAsync(user,password);
               await userManager.AddToRoleAsync(user, "Admin");
            }
        }
        
        app.Run();
    }
}
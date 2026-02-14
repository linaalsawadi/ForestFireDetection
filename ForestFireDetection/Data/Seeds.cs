using ForestFireDetection.Models;
using ForestFireDetection.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ForestFireDetection.Data
{
    public static class Seeds
    {
        public static async Task SeedUsersAndRolesAsync(IApplicationBuilder applicationBuilder)
        {
            using var serviceScope = applicationBuilder.ApplicationServices.CreateScope();

            var roleManager = serviceScope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            if (!await roleManager.RoleExistsAsync(UserRoles.Admin))
                await roleManager.CreateAsync(new IdentityRole(UserRoles.Admin));
            if (!await roleManager.RoleExistsAsync(UserRoles.User))
                await roleManager.CreateAsync(new IdentityRole(UserRoles.User));

            var userManager = serviceScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            // ─── Admin ───
            await CreateUserIfNotExists(userManager, "admin@greenshield.com", "Admin", "GreenShield", "Admin@123", UserRoles.Admin);

            // ─── Fire Station Users ───
            await CreateUserIfNotExists(userManager, "station1@greenshield.com", "North", "Station", "Station@123", UserRoles.User);
            await CreateUserIfNotExists(userManager, "station2@greenshield.com", "South", "Station", "Station@123", UserRoles.User);
            await CreateUserIfNotExists(userManager, "station3@greenshield.com", "East", "Station", "Station@123", UserRoles.User);
        }

        public static async Task SeedTestDataAsync(IApplicationBuilder applicationBuilder)
        {
            using var scope = applicationBuilder.ApplicationServices.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ForestFireDetectionDbContext>();

            // Skip if data already exists
            if (await context.Sensors.AnyAsync())
                return;

            var now = DateTime.UtcNow;
            var random = new Random(42); // fixed seed for reproducibility

            // ─── Sensors (6 sensors across a forest area) ───
            var sensors = new List<Sensor>
            {
                new() { SensorId = "SNS-001", SensorState = "green",   SensorDangerSituation = false, SensorPositioningDate = now.AddMinutes(-1) },
                new() { SensorId = "SNS-002", SensorState = "green",   SensorDangerSituation = false, SensorPositioningDate = now.AddMinutes(-2) },
                new() { SensorId = "SNS-003", SensorState = "yellow",  SensorDangerSituation = true,  SensorPositioningDate = now.AddMinutes(-1) },
                new() { SensorId = "SNS-004", SensorState = "red",     SensorDangerSituation = true,  SensorPositioningDate = now.AddMinutes(-1) },
                new() { SensorId = "SNS-005", SensorState = "green",   SensorDangerSituation = false, SensorPositioningDate = now.AddMinutes(-2) },
                new() { SensorId = "SNS-006", SensorState = "offline", SensorDangerSituation = false, SensorPositioningDate = now.AddMinutes(-10) },
            };

            context.Sensors.AddRange(sensors);
            await context.SaveChangesAsync();

            // ─── Sensor coordinates (Bolu forest area, Turkey) ───
            var coords = new Dictionary<string, (double Lat, double Lng)>
            {
                ["SNS-001"] = (40.7350, 31.6100),
                ["SNS-002"] = (40.7380, 31.6150),
                ["SNS-003"] = (40.7410, 31.6200),
                ["SNS-004"] = (40.7440, 31.6250),
                ["SNS-005"] = (40.7470, 31.6300),
                ["SNS-006"] = (40.7500, 31.6350),
            };

            // ─── SensorData (last 24 hours, every 15 min per sensor) ───
            var sensorDataList = new List<SensorData>();

            foreach (var sensor in sensors)
            {
                var (lat, lng) = coords[sensor.SensorId];

                for (int i = 96; i >= 0; i--) // 96 intervals × 15 min = 24 hours
                {
                    var timestamp = now.AddMinutes(-i * 15);
                    float baseTemp, baseHum, baseSmoke;
                    double fireScore;

                    switch (sensor.SensorState)
                    {
                        case "red":
                            baseTemp = 55 + (float)(random.NextDouble() * 15);
                            baseHum = 10 + (float)(random.NextDouble() * 10);
                            baseSmoke = 40 + (float)(random.NextDouble() * 30);
                            fireScore = 78 + random.NextDouble() * 20;
                            break;
                        case "yellow":
                            baseTemp = 38 + (float)(random.NextDouble() * 12);
                            baseHum = 20 + (float)(random.NextDouble() * 15);
                            baseSmoke = 15 + (float)(random.NextDouble() * 20);
                            fireScore = 50 + random.NextDouble() * 25;
                            break;
                        case "offline":
                            baseTemp = 22 + (float)(random.NextDouble() * 5);
                            baseHum = 55 + (float)(random.NextDouble() * 15);
                            baseSmoke = 1 + (float)(random.NextDouble() * 3);
                            fireScore = 5 + random.NextDouble() * 10;
                            break;
                        default: // green
                            baseTemp = 20 + (float)(random.NextDouble() * 10);
                            baseHum = 50 + (float)(random.NextDouble() * 25);
                            baseSmoke = (float)(random.NextDouble() * 5);
                            fireScore = random.NextDouble() * 25;
                            break;
                    }

                    sensorDataList.Add(new SensorData
                    {
                        Id = Guid.NewGuid(),
                        SensorId = sensor.SensorId,
                        Latitude = lat + (random.NextDouble() - 0.5) * 0.001,
                        Longitude = lng + (random.NextDouble() - 0.5) * 0.001,
                        Temperature = MathF.Round(baseTemp, 1),
                        Humidity = MathF.Round(baseHum, 1),
                        Smoke = MathF.Round(baseSmoke, 1),
                        FireScore = Math.Round(fireScore, 2),
                        Timestamp = timestamp,
                    });
                }
            }

            context.SensorData.AddRange(sensorDataList);
            await context.SaveChangesAsync();

            // ─── Alerts (mix of statuses over last 24 hours) ───
            var alerts = new List<Alert>
            {
                // Active (NotReviewed)
                new()
                {
                    Id = Guid.NewGuid(),
                    SensorId = "SNS-004",
                    Temperature = 68.3f, Smoke = 52.7f, Humidity = 12.1f,
                    FireScore = 92.5,
                    Latitude = 40.7440, Longitude = 31.6250,
                    Timestamp = now.AddMinutes(-8),
                    Status = "NotReviewed"
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    SensorId = "SNS-004",
                    Temperature = 63.1f, Smoke = 45.2f, Humidity = 14.8f,
                    FireScore = 87.3,
                    Latitude = 40.7441, Longitude = 31.6251,
                    Timestamp = now.AddMinutes(-22),
                    Status = "NotReviewed"
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    SensorId = "SNS-003",
                    Temperature = 48.5f, Smoke = 28.9f, Humidity = 22.4f,
                    FireScore = 68.1,
                    Latitude = 40.7410, Longitude = 31.6200,
                    Timestamp = now.AddMinutes(-45),
                    Status = "NotReviewed"
                },

                // In Review
                new()
                {
                    Id = Guid.NewGuid(),
                    SensorId = "SNS-003",
                    Temperature = 44.2f, Smoke = 22.1f, Humidity = 25.0f,
                    FireScore = 58.4,
                    Latitude = 40.7411, Longitude = 31.6201,
                    Timestamp = now.AddHours(-2),
                    Status = "InReview",
                    ReviewedBy = "station1@greenshield.com",
                    ReviewedAt = now.AddHours(-1.5),
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    SensorId = "SNS-004",
                    Temperature = 58.7f, Smoke = 38.4f, Humidity = 16.3f,
                    FireScore = 81.2,
                    Latitude = 40.7442, Longitude = 31.6252,
                    Timestamp = now.AddHours(-3),
                    Status = "InReview",
                    ReviewedBy = "admin@greenshield.com",
                    ReviewedAt = now.AddHours(-2.5),
                },

                // Resolved
                new()
                {
                    Id = Guid.NewGuid(),
                    SensorId = "SNS-001",
                    Temperature = 42.0f, Smoke = 18.5f, Humidity = 28.0f,
                    FireScore = 52.3,
                    Latitude = 40.7350, Longitude = 31.6100,
                    Timestamp = now.AddHours(-6),
                    Status = "Resolved",
                    ReviewedBy = "station2@greenshield.com",
                    ReviewedAt = now.AddHours(-5.5),
                    ResolutionNote = "False alarm caused by nearby agricultural burning. Area inspected and confirmed safe."
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    SensorId = "SNS-002",
                    Temperature = 55.0f, Smoke = 35.0f, Humidity = 18.0f,
                    FireScore = 76.8,
                    Latitude = 40.7380, Longitude = 31.6150,
                    Timestamp = now.AddHours(-10),
                    Status = "Resolved",
                    ReviewedBy = "station1@greenshield.com",
                    ReviewedAt = now.AddHours(-9),
                    ResolutionNote = "Small brush fire detected and extinguished by ground team. No structural damage."
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    SensorId = "SNS-005",
                    Temperature = 38.5f, Smoke = 12.3f, Humidity = 30.0f,
                    FireScore = 45.1,
                    Latitude = 40.7470, Longitude = 31.6300,
                    Timestamp = now.AddHours(-18),
                    Status = "Resolved",
                    ReviewedBy = "admin@greenshield.com",
                    ReviewedAt = now.AddHours(-17),
                    ResolutionNote = "Sensor malfunction due to moisture. Hardware team dispatched for maintenance."
                },

                // Older alerts for chart data
                new()
                {
                    Id = Guid.NewGuid(),
                    SensorId = "SNS-003",
                    Temperature = 41.0f, Smoke = 20.0f, Humidity = 27.0f,
                    FireScore = 55.0,
                    Latitude = 40.7410, Longitude = 31.6200,
                    Timestamp = now.AddHours(-12),
                    Status = "Resolved",
                    ReviewedBy = "station3@greenshield.com",
                    ReviewedAt = now.AddHours(-11),
                    ResolutionNote = "Investigated. Lightning strike caused minor ground fire, rain extinguished it naturally."
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    SensorId = "SNS-004",
                    Temperature = 61.0f, Smoke = 42.0f, Humidity = 13.0f,
                    FireScore = 85.0,
                    Latitude = 40.7440, Longitude = 31.6250,
                    Timestamp = now.AddHours(-15),
                    Status = "Resolved",
                    ReviewedBy = "station1@greenshield.com",
                    ReviewedAt = now.AddHours(-14),
                    ResolutionNote = "Active fire contained by aerial team. Perimeter secured."
                },
            };

            context.Alerts.AddRange(alerts);
            await context.SaveChangesAsync();
        }

        private static async Task CreateUserIfNotExists(
            UserManager<ApplicationUser> userManager,
            string email, string firstName, string lastName,
            string password, string role)
        {
            if (await userManager.FindByEmailAsync(email) != null)
                return;

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                EmailConfirmed = true,
            };

            var result = await userManager.CreateAsync(user, password);
            if (result.Succeeded)
                await userManager.AddToRoleAsync(user, role);
            else
                throw new Exception($"Failed to create {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }
    }
}

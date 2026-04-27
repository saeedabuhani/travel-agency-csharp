using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Travel_Agency.Models;

namespace Travel_Agency.Data
{
    public class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<TravelAgencyDbContext>();
            var userManager = serviceProvider.GetRequiredService<UserManager<AppUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // Ensure database is up to date (works with migrations)
            await context.Database.MigrateAsync();

            // ✅ Backfill visibility for existing packages (created before IsVisible)
            var hiddenPackages = await context.TravelPackages
                .Where(p => !p.IsVisible)
                .ToListAsync();
            if (hiddenPackages.Any())
            {
                foreach (var pkg in hiddenPackages)
                {
                    pkg.IsVisible = true;
                }
                await context.SaveChangesAsync();
            }

            // ⭐ CREATE ROLES
            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                await roleManager.CreateAsync(new IdentityRole("Admin"));
            }

            if (!await roleManager.RoleExistsAsync("User"))
            {
                await roleManager.CreateAsync(new IdentityRole("User"));
            }

            // 👑 CREATE OR UPDATE MASTER ADMIN USER
            var adminEmail = "admin@gmail.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            
            if (adminUser == null)
            {
                // Create new admin
                adminUser = new AppUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "System Administrator",
                    EmailConfirmed = true,
                    DateOfBirth = new DateTime(1990, 1, 1)
                };

                var result = await userManager.CreateAsync(adminUser, "Admin123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                    Console.WriteLine("✅ Admin user created: admin@gmail.com");
                }
            }
            else
            {
                // ⚡ ENSURE existing user has Admin role (fix if registered before seeding)
                if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                    Console.WriteLine("✅ Admin role ADDED to existing user: admin@gmail.com");
                }
                else
                {
                    Console.WriteLine("✅ Admin user already exists with Admin role");
                }
            }

            // 👤 CREATE SAMPLE USERS (NOT ADMINS)
            var sampleUsers = new[]
            {
                new { Email = "user01@gmail.com", Name = "Sarah Johnson", Password = "User@123" },
                new { Email = "user02@gmail.com", Name = "Mike Chen", Password = "User@123" },
                new { Email = "user03@gmail.com", Name = "Emma Davis", Password = "User@123" }
            };

            foreach (var userData in sampleUsers)
            {
                if (await userManager.FindByEmailAsync(userData.Email) == null)
                {
                    var user = new AppUser
                    {
                        UserName = userData.Email,
                        Email = userData.Email,
                        FullName = userData.Name,
                        EmailConfirmed = true,
                        DateOfBirth = new DateTime(1998, 1, 1)
                    };

                    var result = await userManager.CreateAsync(user, userData.Password);
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(user, "User");
                        Console.WriteLine($"✅ User created: {userData.Name}");
                    }
                }
            }

            // 🌍 CREATE TRAVEL PACKAGES (if none exist)
            if (!await context.TravelPackages.AnyAsync())
            {
                var packages = new List<TravelPackage>
                {
                    // GREECE
                    new TravelPackage { Destination = "Santorini", Country = "Greece", Description = "Experience breathtaking sunsets and white-washed buildings", Price = 3500, Discount = null, Nights = 7, AvailableRooms = 10, PackageType = "Luxury", ImageUrl = "https://images.unsplash.com/photo-1613395877344-13d4a8e0d49e" },
                    new TravelPackage { Destination = "Athens", Country = "Greece", Description = "Explore ancient ruins and vibrant modern culture", Price = 2800, Discount = 2500, Nights = 5, AvailableRooms = 15, PackageType = "Cultural", ImageUrl = "https://images.unsplash.com/photo-1555993539-1732b0258235" },
                    new TravelPackage { Destination = "Mykonos", Country = "Greece", Description = "Party paradise with stunning beaches", Price = 4200, Discount = null, Nights = 6, AvailableRooms = 8, PackageType = "Luxury", ImageUrl = "https://images.unsplash.com/photo-1601581987809-a874a81309c9" },

                    // ITALY
                    new TravelPackage { Destination = "Rome", Country = "Italy", Description = "The Eternal City awaits with history at every corner", Price = 3200, Discount = 2900, Nights = 6, AvailableRooms = 12, PackageType = "Cultural", ImageUrl = "https://images.unsplash.com/photo-1552832230-c0197dd311b5" },
                    new TravelPackage { Destination = "Venice", Country = "Italy", Description = "Float through romantic canals", Price = 3800, Discount = null, Nights = 5, AvailableRooms = 10, PackageType = "Romantic", ImageUrl = "https://images.unsplash.com/photo-1523906834658-6e24ef2386f9" },
                    new TravelPackage { Destination = "Florence", Country = "Italy", Description = "Renaissance art and Tuscan charm", Price = 2900, Discount = 2600, Nights = 4, AvailableRooms = 14, PackageType = "Cultural", ImageUrl = "https://images.unsplash.com/photo-1541429756736-4d6b5f7b8e8f" },

                    // FRANCE
                    new TravelPackage { Destination = "Paris", Country = "France", Description = "The City of Light and Love", Price = 4000, Discount = null, Nights = 7, AvailableRooms = 20, PackageType = "Romantic", ImageUrl = "https://images.unsplash.com/photo-1502602898657-3e91760cbb34" },
                    new TravelPackage { Destination = "Nice", Country = "France", Description = "French Riviera luxury and beaches", Price = 3600, Discount = 3200, Nights = 6, AvailableRooms = 12, PackageType = "Beach", ImageUrl = "https://images.unsplash.com/photo-1516825513084-7a3397fcd108" },
                    new TravelPackage { Destination = "Lyon", Country = "France", Description = "Gastronomic capital of France", Price = 2700, Discount = null, Nights = 4, AvailableRooms = 15, PackageType = "Culinary", ImageUrl = "https://images.unsplash.com/photo-1524168272322-bf73616d9cb5" },

                    // SPAIN
                    new TravelPackage { Destination = "Barcelona", Country = "Spain", Description = "Gaudí's masterpieces and Mediterranean vibes", Price = 3100, Discount = 2800, Nights = 6, AvailableRooms = 18, PackageType = "Cultural", ImageUrl = "https://images.unsplash.com/photo-1583422409516-2895a77efded" },
                    new TravelPackage { Destination = "Madrid", Country = "Spain", Description = "Royal palaces and world-class museums", Price = 2900, Discount = null, Nights = 5, AvailableRooms = 16, PackageType = "Cultural", ImageUrl = "https://images.unsplash.com/photo-1539037116277-4db20889f2d4" },
                    new TravelPackage { Destination = "Ibiza", Country = "Spain", Description = "Party island with stunning beaches", Price = 3700, Discount = 3300, Nights = 7, AvailableRooms = 10, PackageType = "Party", ImageUrl = "https://images.unsplash.com/photo-1533105079780-92b9be482077" },

                    // JAPAN
                    new TravelPackage { Destination = "Tokyo", Country = "Japan", Description = "Ultra-modern metropolis meets ancient tradition", Price = 5200, Discount = null, Nights = 8, AvailableRooms = 15, PackageType = "Cultural", ImageUrl = "https://images.unsplash.com/photo-1540959733332-eab4deabeeaf" },
                    new TravelPackage { Destination = "Kyoto", Country = "Japan", Description = "Ancient temples and traditional geisha districts", Price = 4800, Discount = 4400, Nights = 7, AvailableRooms = 12, PackageType = "Cultural", ImageUrl = "https://images.unsplash.com/photo-1493976040374-85c8e12f0c0e" },
                    new TravelPackage { Destination = "Osaka", Country = "Japan", Description = "Food paradise and neon-lit streets", Price = 4500, Discount = null, Nights = 6, AvailableRooms = 14, PackageType = "Culinary", ImageUrl = "https://images.unsplash.com/photo-1590559899731-a382839e5549" },

                    // THAILAND
                    new TravelPackage { Destination = "Bangkok", Country = "Thailand", Description = "Vibrant street life and golden temples", Price = 2400, Discount = 2100, Nights = 6, AvailableRooms = 20, PackageType = "Adventure", ImageUrl = "https://images.unsplash.com/photo-1563492065599-3520f775eeed" },
                    new TravelPackage { Destination = "Phuket", Country = "Thailand", Description = "Tropical paradise with crystal waters", Price = 2800, Discount = null, Nights = 7, AvailableRooms = 18, PackageType = "Beach", ImageUrl = "https://images.unsplash.com/photo-1589394815804-964ed0be2eb5" },
                    new TravelPackage { Destination = "Chiang Mai", Country = "Thailand", Description = "Mountain temples and elephant sanctuaries", Price = 2200, Discount = 1900, Nights = 5, AvailableRooms = 16, PackageType = "Nature", ImageUrl = "https://images.unsplash.com/photo-1598935898639-81586f7d2129" },

                    // UAE
                    new TravelPackage { Destination = "Dubai", Country = "UAE", Description = "Futuristic skyscrapers and luxury shopping", Price = 4500, Discount = null, Nights = 6, AvailableRooms = 15, PackageType = "Luxury", ImageUrl = "https://images.unsplash.com/photo-1512453979798-5ea266f8880c" },
                    new TravelPackage { Destination = "Abu Dhabi", Country = "UAE", Description = "Grand mosque and Ferrari World", Price = 4200, Discount = 3800, Nights = 5, AvailableRooms = 12, PackageType = "Luxury", ImageUrl = "https://images.unsplash.com/photo-1512632578888-169bbbc64f33" },

                    // MALDIVES
                    new TravelPackage { Destination = "Malé", Country = "Maldives", Description = "Overwater bungalows and coral reefs", Price = 6500, Discount = null, Nights = 7, AvailableRooms = 8, PackageType = "Luxury", ImageUrl = "https://images.unsplash.com/photo-1514282401047-d79a71a590e8" },

                    // TURKEY
                    new TravelPackage { Destination = "Istanbul", Country = "Turkey", Description = "Where East meets West", Price = 2600, Discount = 2300, Nights = 6, AvailableRooms = 18, PackageType = "Cultural", ImageUrl = "https://images.unsplash.com/photo-1524231757912-21f4fe3a7200" },
                    new TravelPackage { Destination = "Cappadocia", Country = "Turkey", Description = "Hot air balloons over fairy chimneys", Price = 3100, Discount = null, Nights = 5, AvailableRooms = 10, PackageType = "Adventure", ImageUrl = "https://images.unsplash.com/photo-1541432901042-2d8bd64b4a9b" },

                    // EGYPT
                    new TravelPackage { Destination = "Cairo", Country = "Egypt", Description = "Pyramids of Giza and ancient wonders", Price = 2900, Discount = 2600, Nights = 6, AvailableRooms = 16, PackageType = "Cultural", ImageUrl = "https://images.unsplash.com/photo-1572252009286-268acec5ca0a" },

                    // MOROCCO
                    new TravelPackage { Destination = "Marrakech", Country = "Morocco", Description = "Bustling souks and riads", Price = 2500, Discount = null, Nights = 5, AvailableRooms = 14, PackageType = "Cultural", ImageUrl = "https://images.unsplash.com/photo-1597212618440-806262de4f6b" },

                    // ICELAND
                    new TravelPackage { Destination = "Reykjavik", Country = "Iceland", Description = "Northern lights and geothermal wonders", Price = 4800, Discount = 4400, Nights = 6, AvailableRooms = 10, PackageType = "Nature", ImageUrl = "https://images.unsplash.com/photo-1504893524553-b855bce32c67" },

                    // NEW ZEALAND
                    new TravelPackage { Destination = "Queenstown", Country = "New Zealand", Description = "Adventure capital with stunning landscapes", Price = 5500, Discount = null, Nights = 8, AvailableRooms = 12, PackageType = "Adventure", ImageUrl = "https://images.unsplash.com/photo-1469854523086-cc02fe5d8800" },

                    // MULTI-YEAR DEPARTURES (same destination, different year)
                    new TravelPackage { Destination = "Paris", Country = "France", Description = "Springtime in Paris with a new departure year", Price = 4100, Discount = 3800, Nights = 6, AvailableRooms = 16, PackageType = "Romantic", ImageUrl = "https://images.unsplash.com/photo-1502602898657-3e91760cbb34", StartDate = DateTime.Today.AddDays(400) },
                    new TravelPackage { Destination = "Tokyo", Country = "Japan", Description = "Tokyo highlights - special departure next year", Price = 5400, Discount = null, Nights = 8, AvailableRooms = 14, PackageType = "Cultural", ImageUrl = "https://images.unsplash.com/photo-1540959733332-eab4deabeeaf", StartDate = DateTime.Today.AddDays(430) }
                };

                var baseDate = DateTime.Today.AddDays(30);
                for (int i = 0; i < packages.Count; i++)
                {
                    var package = packages[i];

                    if (package.StartDate == default)
                    {
                        package.StartDate = baseDate.AddDays(i * 7);
                    }

                    if (package.EndDate == default)
                    {
                        package.EndDate = package.StartDate.AddDays(package.Nights);
                    }

                    package.Title ??= $"{package.Destination} {package.PackageType} Package";
                    package.IsVisible = true;

                    if (package.AgeLimit == 0)
                    {
                        package.AgeLimit = package.PackageType.Contains("Party") ? 21 :
                            package.PackageType.Contains("Adventure") ? 16 : 0;
                    }

                    if (package.Discount.HasValue &&
                        (!package.DiscountStartDate.HasValue || !package.DiscountEndDate.HasValue))
                    {
                        package.DiscountStartDate = DateTime.Today.AddDays(1);
                        package.DiscountEndDate = package.DiscountStartDate.Value.AddDays(6);
                    }
                }

                await context.TravelPackages.AddRangeAsync(packages);
                await context.SaveChangesAsync();
            }

            Console.WriteLine("🎉 Seeding complete!");
            Console.WriteLine("👑 Admin: admin@gmail.com / Admin123!");
            Console.WriteLine("👤 Users: user01@gmail.com / User@123");
        }
    }
}
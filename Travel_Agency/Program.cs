

//using Microsoft.AspNetCore.Identity;
//using Microsoft.EntityFrameworkCore;
//using Travel_Agency.Data;
//using Travel_Agency.Models;
//using QuestPDF.Infrastructure;

//// ⭐ QuestPDF License
//QuestPDF.Settings.License = LicenseType.Community;

//var builder = WebApplication.CreateBuilder(args);

//// Database
//builder.Services.AddDbContext<TravelAgencyDbContext>(options =>
//    options.UseSqlServer(
//        builder.Configuration.GetConnectionString("DefaultConnection")
//    )
//);

//// Identity
//builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
//{
//    options.User.RequireUniqueEmail = true;
//    options.SignIn.RequireConfirmedAccount = false;
//})
//.AddEntityFrameworkStores<TravelAgencyDbContext>()
//.AddDefaultTokenProviders();

//// MVC
//builder.Services.AddControllersWithViews();
//builder.Services.AddRazorPages();

//var app = builder.Build();

//// MIDDLEWARE
//if (!app.Environment.IsDevelopment())
//{
//    app.UseExceptionHandler("/Home/Error");
//    app.UseHsts();
//}

//app.UseHttpsRedirection();
//app.UseStaticFiles();
//app.UseRouting();
//app.UseAuthentication();
//app.UseAuthorization();

//// ROUTING
//app.MapControllerRoute(
//    name: "default",
//    pattern: "{controller=Home}/{action=Index}/{id?}"
//);
//app.MapRazorPages();

////
//// ===================================================
//// ★ Seed: Roles + Admin + Users + 25+ Travel Packages
//// ===================================================
//using (var scope = app.Services.CreateScope())
//{
//    var services = scope.ServiceProvider;
//    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
//    var userManager = services.GetRequiredService<UserManager<AppUser>>();

//    try
//    {
//        // ✅ Roles
//        if (!await roleManager.RoleExistsAsync("Admin"))
//            await roleManager.CreateAsync(new IdentityRole("Admin"));
//        if (!await roleManager.RoleExistsAsync("User"))
//            await roleManager.CreateAsync(new IdentityRole("User"));

//        // ✅ Admin יחיד עם Gmail
//        var adminEmail = "admin@gmail.com";
//        var adminUser = await userManager.FindByEmailAsync(adminEmail);
//        if (adminUser == null)
//        {
//            var admin = new AppUser
//            {
//                UserName = adminEmail,
//                Email = adminEmail,
//                FullName = "System Administrator"
//            };
//            var result = await userManager.CreateAsync(admin, "Admin123!");
//            if (result.Succeeded)
//            {
//                await userManager.AddToRoleAsync(admin, "Admin");
//                Console.WriteLine("✅ Admin user created successfully!");
//            }
//        }

//        // ✅ Sample Users (realistic names)
//        var sampleUsers = new[]
//        {
//            new { Email = "sarah.johnson@gmail.com", FullName = "Sarah Johnson", Password = "User@123" },
//            new { Email = "michael.chen@gmail.com", FullName = "Michael Chen", Password = "User@123" },
//            new { Email = "emma.williams@gmail.com", FullName = "Emma Williams", Password = "User@123" }
//        };

//        foreach (var userData in sampleUsers)
//        {
//            var user = await userManager.FindByEmailAsync(userData.Email);
//            if (user == null)
//            {
//                user = new AppUser
//                {
//                    UserName = userData.Email,
//                    Email = userData.Email,
//                    EmailConfirmed = true,
//                    FullName = userData.FullName
//                };

//                var result = await userManager.CreateAsync(user, userData.Password);
//                if (result.Succeeded)
//                {
//                    await userManager.AddToRoleAsync(user, "User");
//                    Console.WriteLine($"✅ User created: {userData.FullName}");
//                }
//            }
//        }

//        // ✅ Seed 25+ Travel Packages
//        await SeedData.Initialize(services, userManager, roleManager);

//        Console.WriteLine("✅ Database seeding completed successfully!");
//    }
//    catch (Exception ex)
//    {
//        var logger = services.GetRequiredService<ILogger<Program>>();
//        logger.LogError(ex, "❌ An error occurred while seeding the database.");
//    }
//}

//app.Run();




using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Travel_Agency.Data;
using Travel_Agency.Models;
using QuestPDF.Infrastructure;

// ⭐ QuestPDF License
QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// Database Configuration
builder.Services.AddDbContext<TravelAgencyDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

// Identity Configuration
builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedAccount = false;

    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
})
.AddEntityFrameworkStores<TravelAgencyDbContext>()
.AddDefaultTokenProviders();

// MVC Services
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Custom Services
builder.Services.AddScoped<Travel_Agency.Services.WaitingListService>();

// Background Services
builder.Services.AddHostedService<Travel_Agency.Services.TripReminderService>();

var app = builder.Build();

// ===================================================
// MIDDLEWARE CONFIGURATION
// ===================================================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// ===================================================
// ROUTING CONFIGURATION
// ===================================================
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);
app.MapRazorPages();

// ===================================================
// DATABASE SEEDING
// ===================================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    try
    {
        // ✅ Call SeedData Initialize method
        await SeedData.Initialize(services);

        Console.WriteLine("✅ Database seeding completed successfully!");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "❌ An error occurred while seeding the database.");
        Console.WriteLine($"❌ Seeding Error: {ex.Message}");
    }
}

Console.WriteLine("🚀 Application started successfully!");

app.Run();
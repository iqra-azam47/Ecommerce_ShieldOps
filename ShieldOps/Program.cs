using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ShieldOps.Data;
using ShieldOps.Models;
using System;

var builder = WebApplication.CreateBuilder(args);

// --- SQLITE DATABASE CONNECTION LOGIC ---
// Local development aur Production dono ke liye appsettings se database path pick hoga
var connectionString = builder.Environment.IsDevelopment()
    ? builder.Configuration.GetConnectionString("DefaultConnection")
    : builder.Configuration.GetConnectionString("ProductionDB");

if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Connection string not found.");
}

// RESTORED REAL DATABASE ENGINE: Swapped SQL Server to SQLite provider
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString)
           .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

// 2. Identity framework architecture rules (Native Identity Restored over SQLite)
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// 3. Register Services
// Verification logic khatam hai, isliye isay comment kar diya hai
// builder.Services.AddScoped<IEmailMockService, EmailMockService>();

// 4. MVC and Session Pipeline Configuration
builder.Services.AddControllersWithViews();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true; // Essential cookie configuration for live environments
});

var app = builder.Build();

// 5. Configure HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Catalog/Index");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// FIXED PIPELINE ORDER: Custom handler clean session execution
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// =========================================================================
// CRITICAL INFRASTRUCTURE GUARD: AUTOMATIC IDENTITY ROLE SEEDING UTILITY
// =========================================================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        // Ensure the core 'Admin' tracking role is registered in the database maps
        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            await roleManager.CreateAsync(new IdentityRole("Admin"));
        }

        // Ensure the core 'Customer' tracking role is registered in the database maps
        if (!await roleManager.RoleExistsAsync("Customer"))
        {
            await roleManager.CreateAsync(new IdentityRole("Customer"));
        }
    }
    catch (Exception ex)
    {
        // Intercept log errors gracefully if any migration execution alerts occur
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding core structural identity roles.");
    }
}
// =========================================================================

// 6. Map routes: Direct entry point set to launch Account/Login right away
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using trendaura.Data;
using trendaura.Models;
using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);

// Read connection string explicitly so we can log/inspect and enable retry on failure
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? throw new InvalidOperationException("DefaultConnection not configured.");

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        // Enable transient fault handling
        sqlOptions.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null);
        sqlOptions.CommandTimeout(180);
    }));

// Add Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequireDigit = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequiredLength = 6;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Configure TWO SEPARATE cookie authentication schemes
builder.Services.AddAuthentication(options =>
{
    // Default scheme for client (public site)
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
})
.AddCookie("AdminCookie", options =>
{
    // Admin-specific cookie configuration
    options.Cookie.Name = ".TrendAura.Admin";
    options.LoginPath = "/Admin/Auth/Login";
    options.AccessDeniedPath = "/Admin/Auth/Login";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// Configure Identity's default cookie
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = ".TrendAura.Client";
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

builder.Services.AddControllersWithViews();

// Add distributed memory cache and session support
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromDays(7);
    options.Cookie.Name = ".TrendAura.Session";
});

// Configure file upload size limits
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 10 * 1024 * 1024; // 10MB
});

var app = builder.Build();

// Log database info early so we can see what the app will try to connect to
var logger = app.Services.GetRequiredService<ILogger<Program>>();
try
{
    var builderCs = new SqlConnectionStringBuilder(connectionString);
    logger.LogInformation("Using SQL Server: {DataSource}, Database: {InitialCatalog}", builderCs.DataSource, builderCs.InitialCatalog);
}
catch
{
    logger.LogInformation("Using connection string from configuration (could not parse details)");
}

// Auto-create or migrate database on startup
using (var scope = app.Services.CreateScope())
{
    try
    {
        var services = scope.ServiceProvider;
        var db = services.GetRequiredService<ApplicationDbContext>();

        // Apply migrations (async) with helpful error handling for login failures
        try
        {
            await db.Database.MigrateAsync();
        }
        catch (SqlException sqlEx)
        {
            logger.LogError(sqlEx, "SQL error while applying migrations. Check connection string and database permissions.");
            throw; // rethrow so the app won't start with a broken DB
        }

        // Seed admin user and roles
        await DbSeeder.SeedAdminUserAsync(services);
    }
    catch (Exception ex)
    {
        var loggerScope = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        loggerScope.LogError(ex, "An error occurred while migrating or seeding the database.");
        // Rethrow so the host fails fast and you can see the error in console/IDE
        throw;
    }
}

// Ensure wwwroot/images directory exists safely
var webRootPath = app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot");
var imagesPath = Path.Combine(webRootPath, "images");

if (!Directory.Exists(imagesPath))
{
    Directory.CreateDirectory(imagesPath);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// Area routes MUST come before default routes
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

// Default route: client home
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

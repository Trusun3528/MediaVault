using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PhotoStorage.Data;
using PhotoStorage.Models;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.IIS;
using System.Diagnostics; // Add this namespace for running Python scripts
using PhotoStorage.Services; // Add this namespace

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? 
    "Data Source=photostorage.db";
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => {
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Add this after the builder is created and before building the app
builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = int.MaxValue; // or set to a very large value
});

builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = long.MaxValue; // Unlimited or specify a large value
});

// Configure the request form limits
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = long.MaxValue; // Unlimited or specify a large value
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartHeadersLengthLimit = int.MaxValue;
});

// Configure authentication cookie
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.None; // Change to Always if using HTTPS
    
    // Critical for cross-machine access:
    options.Cookie.Path = "/";
    options.Cookie.Domain = null; // Let the browser determine the domain
});

// Register the hosted service
builder.Services.AddHostedService<PythonScriptScheduler>();

// Register LMStudioService with HttpClient
builder.Services.AddHttpClient<LMStudioService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
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

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        context.Database.EnsureCreated(); // For simple schema creation
        
        // Or if using migrations:
        // context.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred creating the DB.");
    }
}

app.Run();

// Example of calling a Python script to log user activity
// Call Python script to log user activity
try
{
    var pythonProcess = new Process
    {
        StartInfo = new ProcessStartInfo
        {
            FileName = "python",
            Arguments = "Services/UserActivity/user_activity_logger.py", // Updated path to the Python script
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        }
    };

    pythonProcess.Start();
    string output = pythonProcess.StandardOutput.ReadToEnd();
    string error = pythonProcess.StandardError.ReadToEnd();
    pythonProcess.WaitForExit();

    if (!string.IsNullOrEmpty(error))
    {
        Console.WriteLine($"Python Error: {error}");
    }
    else
    {
        Console.WriteLine($"Python Output: {output}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Failed to execute Python script: {ex.Message}");
}

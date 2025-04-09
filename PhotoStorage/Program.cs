using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PhotoStorage.Data;
using PhotoStorage.Models;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.IIS;

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
    options.Limits.MaxRequestBodySize = int.MaxValue; // or set to a very large value
});

// Configure the request form limits
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = long.MaxValue; // unlimited
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartHeadersLengthLimit = int.MaxValue;
});

// Configure authentication cookie
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

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

app.Run();

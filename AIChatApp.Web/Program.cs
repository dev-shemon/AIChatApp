using AIChatApp.Application.Interfaces;
using AIChatApp.Application.Services;
using AIChatApp.Domain.Interfaces;
using AIChatApp.Infrastructure.Configuration;
using AIChatApp.Infrastructure.Data;
using AIChatApp.Infrastructure.Email;
using AIChatApp.Infrastructure.Repositories;
using AIChatApp.Infrastructure.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("Email"));

// 1. Database Connection
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. DI Container (Wiring up the layers)
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPasswordHasher, CustomPasswordHasher>();
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();

// Antiforgery cookie: ensure secure, appropriate SameSite
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.HeaderName = "X-CSRF-TOKEN"; // optional
});

builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.CheckConsentNeeded = context => false; // Set to true if you have a consent banner
    options.MinimumSameSitePolicy = SameSiteMode.Lax;
});
// 3. Authentication cookie: mark Secure and set SameSite
builder.Services.AddAuthentication("CookieAuth") // Defines the authentication scheme
    .AddCookie("CookieAuth", options =>
    {
        options.Cookie.Name = "AIChatApp.Auth";
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // require Secure cookie
        options.Cookie.SameSite = SameSiteMode.Lax; // choose Lax (or None+Secure if cross-site needed)
        options.LoginPath = "/Auth/Login"; // Redirect path if not logged in
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
    });

builder.Services.AddAuthorization();
builder.Services.AddControllersWithViews()
    .AddCookieTempDataProvider(options =>
    {
        options.Cookie.IsEssential = true; // CRITICAL: Makes TempData survive the redirect
    });

var app = builder.Build();

// Enforce HTTPS and security headers
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    // In development, still redirect to HTTPS so cookies with Secure are sent
    app.UseDeveloperExceptionPage();
}

// Always redirect to HTTPS (recommended)
app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseRouting();

// Important: Authentication middleware must run before Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using WebApp_Core_Identity.Model;
using WebApp_Core_Identity.Services;
using WebApp_Core_Identity.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddDbContext<AuthDbContext>();

// Register Encryption Service
builder.Services.AddSingleton<EncryptionService>();

// Register Audit Service
builder.Services.AddScoped<AuditService>();

// Register Email Service
builder.Services.AddScoped<IEmailService, EmailService>();

// === REGISTER PASSWORD AGE SERVICE ===
builder.Services.Configure<PasswordAgeSettings>(builder.Configuration.GetSection("PasswordAgeSettings"));
builder.Services.AddScoped<IPasswordAgeService, PasswordAgeService>();

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    
    // Enforce unique email
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<AuthDbContext>()
.AddDefaultTokenProviders();

builder.Services.Configure<IdentityOptions>(options =>
{
    // --- FEATURE 2.2: PASSWORD COMPLEXITY ---
    // These settings enforce high entropy to prevent brute-force attacks.
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 12; // High security standard
    options.Password.RequiredUniqueChars = 1;

    // --- FEATURE 2.4: ACCOUNT LOCKOUT POLICY ---
    // 3-minute lockout after 5 failed login attempts (Permanent setting)
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(3);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
});

// Authentication Configuration
builder.Services.AddAuthentication("MyCookieAuth").AddCookie("MyCookieAuth", options =>
{
    options.Cookie.Name = "MyCookieAuth";
    options.LoginPath = "/Login";
    options.AccessDeniedPath = "/AccessDenied";
    
    // === SESSION TIMEOUT CONFIGURATION ===
    // 1-minute session timeout with sliding expiration (Permanent setting)
    options.ExpireTimeSpan = TimeSpan.FromMinutes(1);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

// Authorization Configuration
builder.Services.AddAuthorization(options =>
{
    options.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder("MyCookieAuth")
 .RequireAuthenticatedUser()
.Build();
        
    options.AddPolicy("MustBelongToHRDepartment",
    policy => policy.RequireClaim("Department", "HR"));
});

// Identity Cookie Configuration
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Login";
    options.AccessDeniedPath = "/AccessDenied";
    
    // === SESSION SECURITY CONFIGURATION ===
    // 1-minute session timeout with sliding expiration (Permanent setting)
    options.ExpireTimeSpan = TimeSpan.FromMinutes(1);
    options.SlidingExpiration = true;
  options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.Name = "FreshFarmMarket.Session";
    
// Session timeout handling - redirect to /Login not /Account/Login
    options.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
      context.Response.StatusCode = 401;
        }
else
        {
            // Build correct redirect URL
    var returnUrl = context.Request.Path + context.Request.QueryString;
       context.Response.Redirect($"/Login?ReturnUrl={Uri.EscapeDataString(returnUrl)}");
     }
        return Task.CompletedTask;
 };
});

// === SESSION STATE CONFIGURATION ===
builder.Services.AddSession(options =>
{
    // 1-minute idle timeout (Permanent setting)
    options.IdleTimeout = TimeSpan.FromMinutes(1);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
 options.Cookie.SameSite = SameSiteMode.Strict;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
 app.UseExceptionHandler("/Error500");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

// === CUSTOM ERROR PAGES ===
app.UseStatusCodePagesWithReExecute("/Error{0}");

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

// Middleware must be in this specific order for security to work
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();
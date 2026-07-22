using Microsoft.EntityFrameworkCore;
using WahlMirai.Web.Models;
using WahlMirai.Web.Middleware;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("WahlMiraiDb");
builder.Services.AddDbContext<WahlMiraiDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddScoped<WahlMirai.Web.Services.IAuditService, WahlMirai.Web.Services.AuditService>();
builder.Services.AddScoped<WahlMirai.Web.Services.IAuthService, WahlMirai.Web.Services.AuthService>();
builder.Services.AddScoped<WahlMirai.Web.Services.ICensusService, WahlMirai.Web.Services.CensusService>();
builder.Services.AddScoped<WahlMirai.Web.Services.IVotingService, WahlMirai.Web.Services.VotingService>();
builder.Services.AddScoped<WahlMirai.Web.Services.IPromotionService, WahlMirai.Web.Services.PromotionService>();

builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(2);
    });

builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("ADMIN", policy => policy.RequireRole("ADMIN"));
        options.AddPolicy("ELECTOR", policy => policy.RequireRole("ELECTOR"));
    });

var app = builder.Build();

// Seed initial database data if empty
using (var scope = app.Services.CreateScope())
{
    try
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<WahlMiraiDbContext>();
        WahlMirai.Web.Data.DbInitializer.Initialize(dbContext);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error seeding database: {ex.Message}");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseForcePasswordChange();

app.UseStaticFiles();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


app.Run();

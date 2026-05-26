using ArtClub.DataAccess;
using ArtClub.DataAccess.Interfaces;
using ArtClub.DataAccess.Repositories;
using ArtClub.Models.Entities;
using ArtClub.Services;
using ArtClub.Services.Implementations;
using ArtClub.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using PdfSharp.Fonts;
using PdfSharp.Snippets.Font;
using ArtClub.Services.Helpers;
using ArtClub.Data;
using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

GlobalFontSettings.FontResolver = new FileFontResolver();
var builder = WebApplication.CreateBuilder(args);

// 1. Configurare Bază de Date
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure();
    }));

// 2. Înregistrarea Repository-urilor
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IArtPieceRepository, ArtPieceRepository>();
builder.Services.AddScoped<IReservationRepository, ReservationRepository>();
builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IInvitationRepository, InvitationRepository>();

// 3. Înregistrarea Serviciilor
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IArtPieceService, ArtPieceService>();
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddScoped<IFinanceService, FinanceService>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IInvitationService, InvitationService>();

// 4. Configurare Servicii de Sistem
builder.Services.AddHttpContextAccessor();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddControllersWithViews();

// 5. Configurare Identity
builder.Services.AddDefaultIdentity<User>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddRoles<IdentityRole<int>>()
.AddEntityFrameworkStores<ApplicationDbContext>();

var app = builder.Build();

// 6. Configurare Pipeline HTTP
if (!app.Environment.IsDevelopment())
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

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// 7. PROTECȚIE ȘI EXECUTARE AUTOMATĂ (Prevenire blocaj Test Explorer)
// Verificăm dacă vreun ansamblu încărcat în memorie conține numele unui framework de testare (xUnit, NUnit, MSTest)
var isTestingEnvironment = AppDomain.CurrentDomain.GetAssemblies()
    .Any(a => a.FullName != null && (
        a.FullName.Contains("test", StringComparison.OrdinalIgnoreCase) ||
        a.FullName.Contains("xunit", StringComparison.OrdinalIgnoreCase) ||
        a.FullName.Contains("testhost", StringComparison.OrdinalIgnoreCase)
    ));

if (!isTestingEnvironment)
{
    // Apelare Seeder (Acesta va crea rolurile și adminul la pornire doar în rulare normală)
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            await DbSeeder.SeedAsync(services);
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "A apărut o eroare la popularea bazei de date (Seeding).");
        }
    }

    // Pornim aplicația web propriu-zisă
    await app.RunAsync();
}
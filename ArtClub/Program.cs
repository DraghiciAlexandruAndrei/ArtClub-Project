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

GlobalFontSettings.FontResolver = new FileFontResolver();

var builder = WebApplication.CreateBuilder(args);

// 1. Configurare Bază de Date (Entity Framework + Identity)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// 2. Înregistrarea REPOSITORY-URILOR (Data Access Layer)
// Acestea trebuie înregistrate pentru ca serviciile să le poată folosi
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IArtPieceRepository, ArtPieceRepository>();
builder.Services.AddScoped<IReservationRepository, ReservationRepository>();
builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IInvitationRepository, InvitationRepository>();

// 3. Înregistrarea SERVICIILOR (Business Logic Layer)
// Înregistrăm mai întâi utilitarele independente
builder.Services.AddScoped<INotificationService, NotificationService>();

// Înregistrăm serviciile care folosesc repository-urile de mai sus
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IArtPieceService, ArtPieceService>();
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddScoped<IFinanceService, FinanceService>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IInvitationService, InvitationService>();

// 4. Configurare Servicii de Sistem (Sesiune, HttpContext, MVC)
builder.Services.AddHttpContextAccessor();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddControllersWithViews();

// 5. Configurare ASP.NET Identity
// Înlocuiește autentificarea manuală pe bază de cookie cu sistemul Identity
builder.Services.AddDefaultIdentity<User>(options =>
{
    // Setări mai permisive pentru dezvoltare/testare
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

// 6. Configurare Pipeline HTTP (Middleware)
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();   // necesar pentru CSS/JS/imagini
app.UseRouting();

// Sesiunea trebuie activată înainte de autentificare
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

// 7. Configurare rute
app.MapControllerRoute(
    name: "eventDetails",
    pattern: "Event/Details/{title}",
    defaults: new { controller = "Event", action = "Details" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// 8. Seed pentru rolurile Identity
// Fără aceste roluri, Register va crăpa la AddToRoleAsync(user, "Member")
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();
    // var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

    string[] roles = { "Admin", "Member", "External" };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole<int>(role));
        }
    }

    /*
    // 9. Seed pentru utilizatorul admin implicit
    // Lăsat comentat momentan , admin deja introdus

    const string adminRole = "Admin";
    const string email = "gherasiegabriel@yahoo.com";
    const string password = "Test123*";

    var user = await userManager.FindByEmailAsync(email);
    if (user == null)
    {
        user = new User
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new Exception(string.Join(" | ", result.Errors.Select(e => e.Description)));
        }
    }

    if (!await userManager.IsInRoleAsync(user, adminRole))
    {
        await userManager.AddToRoleAsync(user, adminRole);
    }
    */
}

app.Run();
using Microsoft.EntityFrameworkCore;
using ArtClub.DataAccess;
using ArtClub.DataAccess.Interfaces;
using ArtClub.DataAccess.Repositories;
using ArtClub.Services.Interfaces;
using ArtClub.Services.Implementations;
using ArtClub.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Configurare Bază de Date (Entity Framework)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// 2. Înregistrarea REPOSITORY-URILOR (Data Access Layer)
// Acestea trebuie înregistrate pentru ca Serviciile să le poată folosi
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IArtPieceRepository, ArtPieceRepository>();
builder.Services.AddScoped<IReservationRepository, ReservationRepository>();
builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IInvitationRepository, InvitationRepository>();

// 3. Înregistrarea SERVICIILOR (Business Logic Layer)
// Înregistrăm mai întâi utilitarele independente
builder.Services.AddScoped<INotificationService, NotificationService>();

// Înregistrăm serviciile care folosesc Repository-urile de mai sus
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IArtPieceService, ArtPieceService>();
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddScoped<IFinanceService, FinanceService>();

// EventService depinde de aproape toate celelalte, deci îl punem la final
builder.Services.AddScoped<IEventService, EventService>();

builder.Services.AddScoped<IInvitationService, InvitationService>();
builder.Services.AddHttpContextAccessor(); // trebuie pentru notificări și alte servicii care au nevoie de HttpContext
// 4. Configurare Servicii de Sistem (Sesiune și MVC)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddControllersWithViews();

var app = builder.Build();

// 5. Configurare Pipeline HTTP (Middleware)
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.MapControllerRoute(
    name: "eventDetails",
    pattern: "Event/Details/{title}",
    defaults: new { controller = "Event", action = "Details" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.UseHttpsRedirection();
app.UseRouting();

// Sesiunea trebuie activată ÎNAINTE de Autentificare
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
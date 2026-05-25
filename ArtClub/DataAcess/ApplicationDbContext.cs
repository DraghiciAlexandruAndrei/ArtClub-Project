using Microsoft.EntityFrameworkCore;
using ArtClub.Models.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace ArtClub.DataAccess
{
    // Moștenim IdentityDbContext și specificăm tipul ID-ului ca fiind 'int'
    public class ApplicationDbContext : IdentityDbContext<User, IdentityRole<int>, int>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        // Tabelele specifice aplicației (DbSet-ul de User dispare, e gestionat de base)
        public DbSet<Event> Events { get; set; }
        public DbSet<Resource> Resources { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<ArtPiece> ArtPieces { get; set; }
        public DbSet<EventArtPiece> EventArtPieces { get; set; }
        public DbSet<Invitation> Invitations { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<ClubSettings> ClubSettings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // FOARTE IMPORTANT: Trebuie apelat base.OnModelCreating(modelBuilder) 
            // pentru a configura tabelele interne ale Identity (AspNetUsers, AspNetRoles, etc.)
            base.OnModelCreating(modelBuilder);

            // Înregistrăm configurațiile tale
            modelBuilder.ApplyConfiguration(new UserConfiguration());
            modelBuilder.ApplyConfiguration(new EventConfiguration());

            // Cheia compusă pentru tabela de legătură (Eveniment-Piesă Artă)
            modelBuilder.Entity<EventArtPiece>()
                .HasKey(eap => new { eap.EventId, eap.ArtPieceId });

            // Configurare precizie zecimale pentru SQL Server
            modelBuilder.Entity<Event>()
         .HasOne(e => e.Organizer)
         .WithMany(u => u.OrganizedEvents)
         .HasForeignKey(e => e.OrganizerId)
         .OnDelete(DeleteBehavior.Restrict);

            // Mapare pentru invitații primite
            modelBuilder.Entity<Invitation>()
                .HasOne(i => i.Invitee)
                .WithMany(u => u.ReceivedInvitations)
                .HasForeignKey(i => i.InviteeId); ;

            modelBuilder.Entity<Payment>()
                .Property(p => p.Amount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Resource>()
                .Property(r => r.BasePrice)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<ClubSettings>()
                .Property(cs => cs.NonMemberReservationFeePerDay)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<ClubSettings>()
                .Property(cs => cs.MembershipCost)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<ClubSettings>()
                .Property(cs => cs.EventCostPerArtPiece)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<ClubSettings>()
                .Property(cs => cs.EventCostPerLocation)
                .HasColumnType("decimal(18,2)");
        }
    }
}
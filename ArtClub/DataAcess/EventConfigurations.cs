using ArtClub.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArtClub.DataAccess
{
    public class EventConfiguration : IEntityTypeConfiguration<Event>
    {
        public void Configure(EntityTypeBuilder<Event> builder)
        {
            // 1. Setăm Cheia Primară
            builder.HasKey(e => e.Id);

            // 2. Configurare Proprietăți
            builder.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(e => e.Description)
                .HasMaxLength(1000);

            // Avem grijă cu zecimalele pentru buget (Standard SQL Server)
            builder.Property(e => e.Budget)
                .HasColumnType("decimal(18,2)");

            // 3. Relația cu Organizatorul (User - Identity)
            // Schimbăm 'm.OrganizedEvents' să fie apelat pe clasa User
            builder.HasOne(e => e.Organizer)
                .WithMany(u => u.OrganizedEvents)
                .HasForeignKey(e => e.OrganizerId)
                .OnDelete(DeleteBehavior.Restrict);

            // 4. Relația cu Resursa
            builder.HasOne(e => e.Resource)
                .WithMany(r => r.Events)
                .HasForeignKey(e => e.ResourceId)
                .OnDelete(DeleteBehavior.Restrict);

            // 5. Relația 1:1 cu Reservation
            builder.HasOne(e => e.Reservation)
                .WithOne(r => r.Event)
                .HasForeignKey<Reservation>(r => r.EventId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
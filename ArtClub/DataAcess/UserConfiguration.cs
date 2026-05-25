using ArtClub.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArtClub.DataAccess
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            // 1. NU configura HasKey(u => u.Id), UserName sau Email. 
            // IdentityDbContext se ocupă de ele automat. Dacă le forțezi aici, pot apărea conflicte de lungime (Identity vrea 256, tu ai pus 150).

            // 2. Eliminăm Discriminatorul (TPH)
            // Dacă ai mutat MembershipDate și EventCreationLimit în User, 
            // nu mai avem nevoie de clasele Member/Admin pentru baza de date. 
            // Diferențierea o facem prin coloana 'Role'.

            // 3. Configurăm câmpurile tale custom (cele care NU sunt în IdentityUser)
            builder.Property(u => u.Role)
                   .IsRequired();

            builder.Property(u => u.IsMembershipActive)
                   .HasDefaultValue(false);

            builder.Property(u => u.EventCreationLimit)
                   .HasDefaultValue(1);

            // MembershipDate poate fi null (pentru admini de exemplu)
            builder.Property(u => u.MembershipDate)
                   .IsRequired(false);

            // 4. Indexul unic pe Email este deja gestionat de Identity, 
            // dar îl poți lăsa dacă vrei să fii extra-sigur.
        }
    }
}
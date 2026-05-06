using ArtClub.Models.Entities;

namespace ArtClub.Models.Entities
{
    public class Member : User
    {
        public DateTime MembershipDate { get; set; }

        // REQ-5: Flag pentru upgrade-ul de cont după plată
        public bool IsMembershipActive { get; set; } = false;

        // Limita este 0 la înregistrare și devine 5 (sau altă valoare) după plată
        public int EventCreationLimit { get; set; } = 0;
        public bool CheckEventLimit()
        {
            // Dacă lista de evenimente este null (nu a fost încă încărcată din DB), 
            // considerăm că are 0 evenimente pentru a evita erorile.
            int currentEventsCount = OrganizedEvents?.Count ?? 0;

            return currentEventsCount < EventCreationLimit;
        }

        // Verifică dacă membrul are abonamentul activ ȘI nu a depășit limita
        public bool CanCreateEvent() => IsMembershipActive && OrganizedEvents.Count < EventCreationLimit;

        public virtual ICollection<Event> OrganizedEvents { get; set; } = new List<Event>();
    }
}
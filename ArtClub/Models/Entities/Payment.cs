using ArtClub.Models.Enums;

namespace ArtClub.Models.Entities
{
    public class Payment
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public virtual User User { get; set; }

        public decimal Amount { get; set; }
        public PaymentType Type { get; set; }
        public DateTime Date { get; set; } = DateTime.Now;

        public string Description { get; set; }

        // Determină dacă este venit (true) sau cheltuială (false)
        public bool IsIncome { get; set; }
    }
}
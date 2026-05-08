namespace ArtClub.Models.ViewModels
{
    public class MonthlyReportViewModel
    {
        public int Month { get; set; }
        public int Year { get; set; }

        public decimal TotalIncome { get; set; }
        public decimal TotalExpenses { get; set; }

        public bool MembersBlocked { get; set; }

        public decimal Balance => TotalIncome - TotalExpenses;
    }
}
namespace ArtClub.Models.ViewModels.Admin
{
    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalResources { get; set; }
        public int TotalEvents { get; set; }
        public int TotalPayments { get; set; }

        public decimal MonthlyIncome { get; set; }
        public decimal MonthlyExpenses { get; set; }

        public decimal Balance => MonthlyIncome - MonthlyExpenses;
    }
}
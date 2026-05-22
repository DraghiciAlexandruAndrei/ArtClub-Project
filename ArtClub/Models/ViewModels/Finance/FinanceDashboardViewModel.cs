using ArtClub.Models.Entities;
using System.Collections.Generic;

namespace ArtClub.Models.ViewModels.Finance
{
    public class FinanceDashboardViewModel
    {
        public decimal TotalIncome { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal NetBalance { get; set; }
        public bool HasTheClubEnoughMoney { get; set; }
        public List<string> RecentTransactions { get; set; } = new List<string>();
        public List<Payment> RecentPayments { get; set; } = new List<Payment>();
    }
}
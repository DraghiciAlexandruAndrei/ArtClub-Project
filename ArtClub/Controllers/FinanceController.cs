using System.Text;
using ArtClub.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ArtClub.Controllers
{
    public class FinanceController : Controller
    {
        public IActionResult Index() => Dashboard();

        public IActionResult Dashboard()
        {
            var model = new FinanceDashboardViewModel
            {
                TotalIncome = 24500,
                TotalExpenses = 18200,
                NetBalance = 6300,
                HasTheClubEnoughMoney = true,
                RecentTransactions = new List<string>
                {
                    "Membership fee received - 500 lei",
                    "Resource reservation income - 800 lei",
                    "Workshop expense - 1200 lei",
                    "Monthly balance calculated successfully"
                }
            };

            return View("Index", model);
        }

        public IActionResult Create() => View();

        public IActionResult Edit(int id) => View();

        public IActionResult Delete(int id) => View();

        public IActionResult GenerateReport(int month, int year)
        {
            var content = $"ArtClub finance report for {month:D2}/{year}\nIncome: 24500 lei\nExpenses: 18200 lei\nNet balance: 6300 lei\n";
            return File(Encoding.UTF8.GetBytes(content), "text/plain", $"Raport-{month:D2}-{year}.txt");
        }
    }
}
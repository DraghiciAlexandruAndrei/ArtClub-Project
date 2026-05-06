using ArtClub.Models.Entities;
using ArtClub.Models.Enums;
using ArtClub.Models.ViewModels;
using ArtClub.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ArtClub.Controllers
{
    public class FinanceController : Controller
    {
        private readonly IFinanceService _financeService;
        private readonly UserManager<User> _userManager;

        public FinanceController(IFinanceService financeService, UserManager<User> userManager)
        {
            _financeService = financeService;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            return RedirectToAction(nameof(Dashboard));
        }

        public async Task<IActionResult> Dashboard()
        {
            var income = await _financeService.GetTotalIncomeAsync();
            var expenses = await _financeService.GetTotalExpensesAsync();
            var balance = income - expenses;
            var payments = await _financeService.GetAllPaymentsAsync();

            var model = new FinanceDashboardViewModel
            {
                TotalIncome = income,
                TotalExpenses = expenses,
                NetBalance = balance,
                HasTheClubEnoughMoney = balance >= 0,
                RecentTransactions = payments
                    .Take(5)
                    .Select(p => $"{(p.IsIncome ? "Income" : "Expense")} - {p.Amount} lei - {p.Date:dd.MM.yyyy}")
                    .ToList()
            };

            return View("Index", model);
        }

        public async Task<IActionResult> Payments()
        {
            var payments = await _financeService.GetAllPaymentsAsync();
            return View(payments);
        }

        public IActionResult Create()
        {
            return View(new Payment
            {
                Date = DateTime.Now,
                IsIncome = true
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        
        public async Task<IActionResult> Create(decimal amount, DateTime date, bool isIncome)
        {
            // Extrage ID-ul ca string
            var userIdString = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userIdString))
            {
                ModelState.AddModelError("", "Utilizatorul nu a fost găsit.");
                return View();
            }

            // Convertește-l la int (pentru că User-ul tău e IdentityUser<int>)
            int userIdNumeric = int.Parse(userIdString);

            var payment = new Payment
            {
                UserId = userIdNumeric, // <--- Aceasta este linia critică
                Amount = amount,
                Date = date,
                IsIncome = isIncome,
                Type = isIncome ? PaymentType.Subscription : PaymentType.Expense,
                Description = isIncome ? "Venit nou" : "Cheltuială nouă" // Asigură-te că și Description are o valoare dacă e NOT NULL
            };

            if (ModelState.IsValid)
            {
                await _financeService.CreatePaymentAsync(payment);
                return RedirectToAction(nameof(Payments));
            }

            return View(payment);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var payment = await _financeService.GetPaymentByIdAsync(id);

            if (payment == null)
                return NotFound();

            return View(payment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, decimal amount, DateTime date, bool isIncome)
        {
            var existingPayment = await _financeService.GetPaymentByIdAsync(id);

            if (existingPayment == null)
                return NotFound();

            if (amount <= 0)
            {
                ModelState.AddModelError("Amount", "Amount must be greater than 0.");
                existingPayment.Amount = amount;
                existingPayment.Date = date;
                existingPayment.IsIncome = isIncome;
                return View(existingPayment);
            }

            existingPayment.Amount = amount;
            existingPayment.Date = date;
            existingPayment.IsIncome = isIncome;
            existingPayment.Type = isIncome ? PaymentType.Subscription : PaymentType.Expense;

            var success = await _financeService.UpdatePaymentAsync(existingPayment);

            if (!success)
                return NotFound();

            TempData["StatusMessage"] = "Payment updated successfully.";
            return RedirectToAction(nameof(Payments));
        }

        public async Task<IActionResult> Details(int id)
        {
            var payment = await _financeService.GetPaymentByIdAsync(id);

            if (payment == null)
                return NotFound();

            return View(payment);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var payment = await _financeService.GetPaymentByIdAsync(id);

            if (payment == null)
                return NotFound();

            return View(payment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var success = await _financeService.DeletePaymentAsync(id);

            if (!success)
                return NotFound();

            TempData["StatusMessage"] = "Payment deleted successfully.";
            return RedirectToAction(nameof(Payments));
        }

        public async Task<IActionResult> GenerateReport(int month, int year)
        {
            var report = await _financeService.GenerateMonthlyReportAsync(month, year);

            return File(report, "application/pdf", $"Raport-{month:D2}-{year}.pdf");
        }
    }
}
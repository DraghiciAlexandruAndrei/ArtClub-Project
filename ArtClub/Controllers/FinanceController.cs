using ArtClub.Models.Entities;
using ArtClub.Models.Enums;
using ArtClub.Models.ViewModels.Finance;
using ArtClub.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ArtClub.Controllers
{
    [Authorize(Roles = "Admin")] // Doar Adminii pot accesa modulul financiar
    public class FinanceController : Controller
    {
        private readonly IFinanceService _financeService;
        private readonly UserManager<User> _userManager;
        private readonly IEventService _eventService;

        public FinanceController(IFinanceService financeService, UserManager<User> userManager, IEventService eventService)
        {
            _financeService = financeService;
            _userManager = userManager;
            _eventService = eventService;
        }

        public IActionResult Index() => RedirectToAction(nameof(Dashboard));

        public async Task<IActionResult> Dashboard()
        {
            var income = await _financeService.GetTotalIncomeAsync();
            var expenses = await _financeService.GetTotalExpensesAsync();
            var balance = income - expenses;
            var payments = await _financeService.GetAllPaymentsAsync();

            return View("Index", new FinanceDashboardViewModel
            {
                TotalIncome = income,
                TotalExpenses = expenses,
                NetBalance = balance,
                HasTheClubEnoughMoney = balance >= 0,
                RecentPayments = payments.OrderByDescending(p => p.Date).Take(5).ToList()
            });
        }

        public async Task<IActionResult> Payments() => View(await _financeService.GetAllPaymentsAsync());

        [HttpGet]
        public IActionResult Create() => View(new Payment { Date = DateTime.Now, IsIncome = true });

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(decimal amount, DateTime date, bool isIncome)
        {
            var userIdString = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userIdString)) return Challenge();

            var payment = new Payment
            {
                UserId = int.Parse(userIdString),
                Amount = amount,
                Date = date,
                IsIncome = isIncome,
                Type = isIncome ? PaymentType.Subscription : PaymentType.Expense,
                Description = isIncome ? "Venit nou" : "Cheltuială nouă"
            };

            if (ModelState.IsValid)
            {
                await _financeService.CreatePaymentAsync(payment);
                return RedirectToAction(nameof(Payments));
            }
            return View(payment);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var payment = await _financeService.GetPaymentByIdAsync(id);
            return payment == null ? NotFound() : View(payment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, decimal amount, DateTime date, bool isIncome)
        {
            var existingPayment = await _financeService.GetPaymentByIdAsync(id);
            if (existingPayment == null) return NotFound();

            if (amount <= 0)
            {
                ModelState.AddModelError("Amount", "Suma trebuie să fie pozitivă.");
                return View(existingPayment);
            }

            existingPayment.Amount = amount;
            existingPayment.Date = date;
            existingPayment.IsIncome = isIncome;
            existingPayment.Type = isIncome ? PaymentType.Subscription : PaymentType.Expense;

            await _financeService.UpdatePaymentAsync(existingPayment);
            TempData["StatusMessage"] = "Plata a fost actualizată.";
            return RedirectToAction(nameof(Payments));
        }

        public async Task<IActionResult> Details(int id)
        {
            var payment = await _financeService.GetPaymentByIdAsync(id);
            return payment == null ? NotFound() : View(payment);
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var payment = await _financeService.GetPaymentByIdAsync(id);
            return payment == null ? NotFound() : View(payment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (await _financeService.DeletePaymentAsync(id))
                TempData["StatusMessage"] = "Plata a fost ștearsă.";
            return RedirectToAction(nameof(Payments));
        }

        public async Task<IActionResult> GenerateReport(int month, int year)
        {
            var report = await _financeService.GenerateMonthlyReportAsync(month, year);
            return File(report, "application/pdf", $"Raport-{month:D2}-{year}.pdf");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Pay(int eventId, decimal amount)
        {
            var ev = await _eventService.GetEventByIdAsync(eventId);
            if (ev == null) return NotFound();

            if (await _eventService.MarkEventAsPaidAsync(eventId))
            {
                await _financeService.CreatePaymentAsync(new Payment
                {
                    Amount = amount,
                    Date = DateTime.Now,
                    IsIncome = true,
                    UserId = ev.OrganizerId,
                    Description = $"Încasare finală eveniment: {ev.Title}"
                });
                TempData["StatusMessage"] = "Plata înregistrată.";
            }
            return RedirectToAction("Details", "Event", new { title = ev.Title });
        }
    }
}
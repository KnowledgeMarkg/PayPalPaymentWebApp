using Microsoft.AspNetCore.Mvc;
using PayPalPaymentWebApp.Data;
using PayPalPaymentWebApp.Models;
using System.Diagnostics;

namespace PayPalPaymentWebApp.Controllers
{
    public class HomeController : BaseController
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ApplicationDbContext context, IConfiguration configuration, ILogger<HomeController> logger)
            : base(context, configuration) // Calls the BaseController constructor
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            // ViewData["RemainingTickets"] is already set by BaseController
            ViewData["RemainingTickets"] = GetRemainingTickets();
            return View();
        }

        public IActionResult Privacy()
        {
            // ViewData["RemainingTickets"] is already set by BaseController
            return View();
        }

        public IActionResult ContactUs()
        {
            ViewData["RemainingTickets"] = GetRemainingTickets();
            return View();
        }

        public IActionResult AboutUs()
        {
            ViewData["RemainingTickets"] = GetRemainingTickets();
            return View();
        }

        [HttpPost]
        public IActionResult SubmitInquiry(ContactUsModel model)
        {
            if (ModelState.IsValid)
            {
                // Save the inquiry to the database
                _context.Add(model);
                _context.SaveChanges();

                // Optionally, display a success message or redirect
                ViewData["Message"] = "Thank you for your inquiry. We'll get back to you soon.";
                return RedirectToAction("ContactUs");
            }

            // If we got this far, something failed, redisplay form
            return View("ContactUs", model);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        protected int GetRemainingTickets()
        {
            int maxTickets = int.Parse(_configuration["TicketSettings:TotalTickets"]);
            int currentTicketCount = _context.PaymentTokens.Count();
            return maxTickets - currentTicketCount;
        }
    }
}

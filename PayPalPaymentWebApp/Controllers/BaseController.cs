using Microsoft.AspNetCore.Mvc;
using PayPalPaymentWebApp.Data;

namespace PayPalPaymentWebApp.Controllers
{
    public class BaseController : Controller
    {
        protected readonly ApplicationDbContext _context;
        protected readonly IConfiguration _configuration;

        public BaseController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
            ViewData["RemainingTickets"] = GetRemainingTickets();
        }

        protected int GetRemainingTickets()
        {
            int maxTickets = int.Parse(_configuration["TicketSettings:TotalTickets"]);
            int currentTicketCount = _context.PaymentTokens.Count();
            return maxTickets - currentTicketCount;
        }
    }
}

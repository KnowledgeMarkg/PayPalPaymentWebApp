using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PayPalPaymentWebApp.Data;
using PayPalPaymentWebApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace PayPalPaymentWebApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly PayPalService _payPalService;
        private readonly IConfiguration _configuration;

        public AccountController(ApplicationDbContext context, PayPalService payPalService, IConfiguration configuration)
        {
            _context = context;
            _payPalService = payPalService;
            _configuration = configuration;
            ViewData["RemainingTickets"] = GetRemainingTickets();
        }

        [HttpGet]
        public IActionResult Register()
        {
            ViewData["RemainingTickets"] = GetRemainingTickets();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(User model)
        {
            if (ModelState.IsValid)
            {
                // Check ticket availability
                int maxTickets = int.Parse(_configuration["TicketSettings:TotalTickets"]);
                int currentTicketCount = await _context.PaymentTokens.CountAsync();
                int remainingTickets = maxTickets - currentTicketCount;

                if (model.NumberOfTickets > remainingTickets)
                {
                    ViewData["RemainingTickets"] = remainingTickets; // Directly assign remaining tickets
                    ModelState.AddModelError("", $"You are trying to book {model.NumberOfTickets} tickets, but only {remainingTickets} tickets are available.");
                    return View(model);
                }

                // Save user details to the database
                _context.Users.Add(model);
                await _context.SaveChangesAsync();

                // Redirect to the payment process, passing the user model and number of tickets
                return RedirectToAction("ProcessPayment", new { userId = model.UserId });
            }

            return View(model);
        }

        public async Task<IActionResult> ProcessPayment(int userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    ModelState.AddModelError("", "User not found.");
                    return View("Error");
                }

                // Calculate the total amount based on the number of tickets
                decimal ticketPrice = decimal.Parse(_configuration["TicketSettings:TicketPrice"]);
                decimal totalAmount = ticketPrice * user.NumberOfTickets;

                var accessToken = await _payPalService.GetAccessTokenAsync();
                var paymentResponse = await CreatePayPalPayment(accessToken, user, totalAmount);

                if (paymentResponse != null)
                {
                    var approvalUrl = paymentResponse.GetApprovalUrl();
                    return Redirect(approvalUrl);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing payment: {ex.Message}");
                return View("Error");
            }

            return View("Error");
        }

        public async Task<IActionResult> PaymentSuccess(int userId, string paymentId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                ModelState.AddModelError("", "User not found.");
                return View("Error");
            }

            // Check if payment has already been processed
            if (await _context.PaymentTokens.AnyAsync(t => t.PaymentId == paymentId))
            {
                var tokens = await _context.PaymentTokens
                                           .Where(t => t.PaymentId == paymentId)
                                           .Select(t => t.Token)
                                           .ToListAsync();

                var viewModel = new PaymentSuccessViewModel
                {
                    UserId = userId,
                    TicketTokens = tokens
                };

                ViewData["RemainingTickets"] = GetRemainingTickets();
                return View("Success", viewModel);
            }

            // Generate tokens and proceed as before...

            int maxTickets = int.Parse(_configuration["TicketSettings:TotalTickets"]);
            int currentTicketCount = await _context.PaymentTokens.CountAsync();

            if (currentTicketCount + user.NumberOfTickets > maxTickets)
            {
                ModelState.AddModelError("", "Not enough tickets left.");
                return View("Error");
            }

            int startTokenNumber = 10600;

            List<PaymentToken> paymentTokens = new List<PaymentToken>();
            for (int i = 0; i < user.NumberOfTickets; i++)
            {
                string token = "SHA" + (startTokenNumber - i).ToString();
                paymentTokens.Add(new PaymentToken
                {
                    UserId = userId,
                    Token = token,
                    PaymentDate = DateTime.UtcNow,
                    PaymentId = paymentId
                });
            }

            try
            {
                _context.PaymentTokens.AddRange(paymentTokens);
                await _context.SaveChangesAsync();

                // Send email with ticket information
                /*await SendTicketInformationEmail(user.Email, paymentTokens.Select(t => t.Token).ToList());
*/            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving payment tokens: {ex.Message}");
                return View("Error");
            }

            var tokenStrings = paymentTokens.Select(t => t.Token).ToList();
            var successViewModel = new PaymentSuccessViewModel
            {
                UserId = userId,
                TicketTokens = tokenStrings
            };

            ViewData["RemainingTickets"] = GetRemainingTickets();
            return View("Success", successViewModel);
        }


        [HttpGet]
        public async Task<IActionResult> DownloadTickets(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            var tokens = await _context.PaymentTokens
                                       .Where(t => t.UserId == userId)
                                       .Select(t => t.Token)
                                       .ToListAsync();

            if (tokens == null || !tokens.Any())
            {
                return NotFound("No tickets found.");
            }

            string fileName = $"Tickets_{userId}.txt";
            string fileContent = "Your Tickets:\n" + string.Join("\n", tokens);

            var content = new System.Text.UTF8Encoding().GetBytes(fileContent);
            var contentType = "text/plain";

            return File(content, contentType, fileName);
        }

        private async Task SendTicketInformationEmail(string userEmail, List<string> tokens)
        {
            string subject = "Your Tickets for the Event";
            string body = $"<h2>Thank you for your purchase!</h2><p>Your ticket numbers are:</p><ul>";

            foreach (var token in tokens)
            {
                body += $"<li>{token}</li>";
            }

            body += "</ul><p>We look forward to seeing you at the event!</p>";

            try
            {
                using (var client = new SmtpClient())
                {
                    client.Host = _configuration["Smtp:Host"];
                    client.Port = int.Parse(_configuration["Smtp:Port"]);
                    client.EnableSsl = bool.Parse(_configuration["Smtp:EnableSsl"]);
                    client.Credentials = new NetworkCredential(_configuration["Smtp:Username"], _configuration["Smtp:Password"]);

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(_configuration["Smtp:From"]),
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = true,
                    };
                    mailMessage.To.Add(userEmail);

                    await client.SendMailAsync(mailMessage);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending email: {ex.Message}");
                // Log the exception details here or handle accordingly
                throw; // Optionally, rethrow the exception to be handled higher up
            }
        }


        private async Task<PayPalPaymentResponse> CreatePayPalPayment(string accessToken, User user, decimal totalAmount)
        {
            var paymentRequest = new
            {
                intent = "sale",
                payer = new { payment_method = "paypal" },
                transactions = new[]
                {
                    new
                    {
                        amount = new { total = totalAmount.ToString("F2"), currency = "GBP" },  // Change currency to GBP
                        description = "Registration Fee"
                    }
                },
                redirect_urls = new
                {
                    return_url = Url.Action("PaymentSuccess", "Account", new { userId = user.UserId }, protocol: Request.Scheme),
                    cancel_url = Url.Action("PaymentCancelled", "Account", new { userId = user.UserId }, protocol: Request.Scheme)
                }
            };

            var paymentResponse = await _payPalService.CreatePaymentAsync(accessToken, paymentRequest);
            return paymentResponse;
        }

        protected int GetRemainingTickets()
        {
            int maxTickets = int.Parse(_configuration["TicketSettings:TotalTickets"]);
            int currentTicketCount = _context.PaymentTokens.Count();
            return maxTickets - currentTicketCount;
        }
    }
}

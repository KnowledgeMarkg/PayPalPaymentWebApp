namespace PayPalPaymentWebApp.Models
{
    public class PaymentSuccessViewModel
    {
        public int UserId { get; set; } // The user's ID
        public List<string> TicketTokens { get; set; } // The list of ticket tokens
    }

}

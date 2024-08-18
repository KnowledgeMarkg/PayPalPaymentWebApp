using System.ComponentModel.DataAnnotations;

namespace PayPalPaymentWebApp.Models
{
    public class PaymentToken
    {
        [Key]
        public int TokenId { get; set; }

        public int UserId { get; set; }
        public string Token { get; set; }
        public DateTime PaymentDate { get; set; }

        public User User { get; set; }
        public string PaymentId { get; set; }
    }
}

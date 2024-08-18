using PayPalPaymentWebApp.Controllers;

namespace PayPalPaymentWebApp.Models
{
    public class PayPalPaymentResponse
    {
        public string id { get; set; }
        public string state { get; set; }
        public string intent { get; set; }
        public PayPalPaymentLink[] links { get; set; }

        public string GetApprovalUrl()
        {
            foreach (var link in links)
            {
                if (link.rel == "approval_url")
                {
                    return link.href;
                }
            }
            return null;
        }
    }
}

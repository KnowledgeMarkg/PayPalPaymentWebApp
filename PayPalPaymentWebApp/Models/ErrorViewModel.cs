namespace PayPalPaymentWebApp.Models
{
    public class ErrorViewModel
    {
        public string RequestId { get; set; }
        public int? StatusCode { get; set; }
        public string Message { get; set; }
        public Exception Exception { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }

}

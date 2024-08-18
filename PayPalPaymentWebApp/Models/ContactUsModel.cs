using System.ComponentModel.DataAnnotations;

namespace PayPalPaymentWebApp.Models
{
    public class ContactUsModel
    {
        [Key]
        public int InquiryId { get; set; } // Primary Key

        [Required(ErrorMessage = "Name is required.")]
        [StringLength(100, ErrorMessage = "Name cannot be longer than 100 characters.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid Email Address.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Message is required.")]
        [StringLength(1000, ErrorMessage = "Message cannot be longer than 1000 characters.")]
        public string Message { get; set; }

        public DateTime SubmittedOn { get; set; } = DateTime.Now; // Automatically set the submission time
    }
}

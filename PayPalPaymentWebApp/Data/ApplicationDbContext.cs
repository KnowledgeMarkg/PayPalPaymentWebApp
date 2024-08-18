using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PayPalPaymentWebApp.Models;

namespace PayPalPaymentWebApp.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // Define your DbSets here
        public DbSet<User> Users { get; set; }
        public DbSet<PaymentToken> PaymentTokens { get; set; }
    }

}

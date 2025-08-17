using Microsoft.EntityFrameworkCore;
using RinhaBackend2025.Domain.Models;

namespace RinhaBackend2025.Infra.Database
{
    public class PaymentDbContext : DbContext
    {
        public PaymentDbContext(DbContextOptions<PaymentDbContext> options)
            : base(options)
        {
        }

        public DbSet<Payment> Payments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Payment>()
                .ToTable("tb_payment")
                .HasKey(p => p.CorrelationId);

            modelBuilder.Entity<Payment>()
                .Property(p => p.CorrelationId)
                .HasColumnName("correlation_id");

            modelBuilder.Entity<Payment>()
                .Property(p => p.Amount)
                .HasColumnName("amount");

            modelBuilder.Entity<Payment>()
                .Property(p => p.Processor)
                .HasColumnName("processor");
                
            modelBuilder.Entity<Payment>()
                .Property(p => p.ProcessorAt)
                .HasColumnName("processor_at");
        }
    }
}
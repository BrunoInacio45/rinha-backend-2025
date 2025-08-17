using Microsoft.EntityFrameworkCore;
using RinhaBackend2025.Domain.Models;
using RinhaBackend2025.Models;

namespace RinhaBackend2025.Infra.Database
{
    public class PaymentRepository
    {
        private readonly PaymentDbContext _context;

        public PaymentRepository(PaymentDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Payment payment, CancellationToken cancellationToken = default)
        {
            _context.Payments.Add(payment);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<List<Summary>> GetByDates(DateTime? to, DateTime? from, CancellationToken cancellationToken = default)
        {
            var toUtc = to?.Kind == DateTimeKind.Utc ? to : to?.ToUniversalTime();
            var fromUtc = from?.Kind == DateTimeKind.Utc ? from : from?.ToUniversalTime();

            return await _context.Payments
                .Where(p =>
                    (fromUtc == null || p.ProcessorAt >= fromUtc) &&
                    (toUtc == null || p.ProcessorAt <= toUtc)
                )
                .GroupBy(_ => _.Processor)
                .Select(_ => new Summary(_.Key, _.Count(), _.Sum(p => p.Amount)))
                .ToListAsync(cancellationToken);
    }
}
}
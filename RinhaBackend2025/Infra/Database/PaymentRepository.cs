using Microsoft.EntityFrameworkCore;
using Npgsql;
using RinhaBackend2025.Domain.Models;
using RinhaBackend2025.Models;

namespace RinhaBackend2025.Infra.Database
{
    public class PaymentRepository
    {
        private readonly string _connectionString;

        public PaymentRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        public async Task AddAsync(Payment payment, CancellationToken cancellationToken = default)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var sql = @"
                INSERT INTO tb_payment (correlation_id, amount, processor, processor_at) 
                VALUES (@correlationId, @amount, @processor, @processorAt)";

            await using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("correlationId", payment.CorrelationId);
            cmd.Parameters.AddWithValue("amount", payment.Amount);
            cmd.Parameters.AddWithValue("processor", payment.Processor);
            cmd.Parameters.AddWithValue("processorAt", payment.ProcessorAt);

            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task<List<Summary>> GetByDates(DateTime? to, DateTime? from, CancellationToken cancellationToken = default)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var toUtc = to?.Kind == DateTimeKind.Utc ? to : to?.ToUniversalTime();
            var fromUtc = from?.Kind == DateTimeKind.Utc ? from : from?.ToUniversalTime();

            var sql = @"
                SELECT processor, COUNT(*)::int AS total, SUM(amount)::numeric AS total_amount
                FROM tb_payment
                WHERE (@from IS NULL OR processor_at >= @from)
                AND (@to IS NULL OR processor_at <= @to)
                GROUP BY processor;";

            await using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.Add("from", NpgsqlTypes.NpgsqlDbType.TimestampTz)
                  .Value = (object?)from ?? DBNull.Value;

            cmd.Parameters.Add("to", NpgsqlTypes.NpgsqlDbType.TimestampTz)
                          .Value = (object?)to ?? DBNull.Value;

            var result = new List<Summary>();
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                result.Add(new Summary(
                    reader.GetInt32(0),
                    reader.GetInt32(1),
                    reader.GetDecimal(2)
                ));
            }

            return result;
        }
    }
}
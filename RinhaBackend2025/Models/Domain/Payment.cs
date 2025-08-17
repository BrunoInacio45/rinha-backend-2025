using System.ComponentModel.DataAnnotations.Schema;
using RinhaBackend2025.Models;

namespace RinhaBackend2025.Domain.Models;

public class Payment
{
    public string CorrelationId { get; private set; }
    public decimal Amount { get; private set; }
    public int Processor { get; private set; }
    public DateTime ProcessorAt { get; private set; }

    [NotMapped]
    public decimal NumberTry { get; private set; } = 0;

    public Payment(string correlationId, decimal amount)
    {
        CorrelationId = correlationId;
        Amount = amount;
        ProcessorAt = DateTime.UtcNow;  
    }

    public void IncrementTry()
    {
        NumberTry++;
    }

    public void SetProcessor(PaymentProcessorEnum processor)
    {
        Processor = (int)processor;
    }
}
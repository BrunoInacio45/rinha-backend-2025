namespace RinhaBackend2025.Models;
public record PaymentRequest(string CorrelationId, decimal Amount);
public record PaymentProcessorRequest(string correlationId, decimal amount, DateTimeOffset requestedAt);
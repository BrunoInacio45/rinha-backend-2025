namespace RinhaBackend2025.Models;

public sealed record SummaryData(int TotalRequests,decimal TotalAmount);
public sealed record SummaryResponse(SummaryData Default,SummaryData Fallback);

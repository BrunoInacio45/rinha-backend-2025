using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using RinhaBackend2025.Domain.Models;
using RinhaBackend2025.Infra.Clients;
using RinhaBackend2025.Infra.Database;
using RinhaBackend2025.Infra.Queue;
using RinhaBackend2025.Infra.Worker;
using RinhaBackend2025.Models;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<PaymentDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpClient<ProcessorDefaultClient>(client =>
{
    client.BaseAddress = new Uri("http://payment-processor-default:8080/");
    client.Timeout = TimeSpan.FromSeconds(10);
}); 

builder.Services.AddHttpClient<ProcessorFallbackClient>(client =>
{
    client.BaseAddress = new Uri("http://payment-processor-fallback:8080/");
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect("redis:6379")
);

builder.Services.AddSingleton<PaymentQueue>();
builder.Services.AddScoped<PaymentRepository>();

builder.Services.AddHostedService<PaymentWorker>();
builder.Services.AddHostedService<HealthProcessorWorker>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();


app.MapPost("/payments", async (PaymentQueue queue, PaymentRequest paymentRequest) =>
{
    var payment = new Payment(paymentRequest.CorrelationId, paymentRequest.Amount);
    await queue.PublishAsync(payment);
    
    return Results.Ok(payment);
})
.WithName("Payments")
.WithOpenApi();

app.MapGet("/payments-summary", async (PaymentRepository repository, DateTime? to = null, DateTime? from = null) =>
{
    var payments = await repository.GetByDates(to, from);

    var paymentProcessorDefault = payments?
        .Where(_ => _.Processor == (int)PaymentProcessorEnum.Default)
        .Select(_ => new SummaryData(_.TotalRequests, _.TotalAmount))
        .FirstOrDefault() ?? new SummaryData(0, 0);

    var paymentProcessorFallback = payments?
        .Where(_ => _.Processor == (int)PaymentProcessorEnum.Fallback)
        .Select(_ => new SummaryData(_.TotalRequests, _.TotalAmount))
        .FirstOrDefault() ?? new SummaryData(0, 0);

    var retorno = new SummaryResponse(paymentProcessorDefault, paymentProcessorFallback);
    return Results.Ok(retorno);
})
.WithName("PaymentsSummary")
.WithOpenApi();

app.MapGet("/payments/service-health", () =>
{
    return Results.Ok(new
    {
        failing = false
    });
})
.WithName("HealthCheck")
.WithOpenApi();

app.Run();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

using Microsoft.EntityFrameworkCore;

using PaymentGateway.Logic;
using PaymentGateway.Logic.DataAccess.EntityMapping;
using PaymentGateway.Logic.DataAccess.Interfaces;
using PaymentGateway.Logic.Models.Request;
using PaymentGateway.Logic.Services.Interfaces;

using Serilog;
using Serilog.Filters;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Logger(requestLog => requestLog
        .Filter.ByIncludingOnly(Matching.FromSource("Serilog.AspNetCore.RequestLoggingMiddleware"))
        .WriteTo.File("logs/requests-.log", rollingInterval: RollingInterval.Day))
    .WriteTo.Logger(applicationLog => applicationLog
        .Filter.ByExcluding(Matching.FromSource("Serilog.AspNetCore.RequestLoggingMiddleware"))
        .WriteTo.File("logs/application-.log", rollingInterval: RollingInterval.Day))
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog();

builder.Services.AddOpenApi();
builder.Services.RegisterPaymentGatewayLogic();
builder.Services.RegisterServices(builder.Configuration);
builder.Services.RegisterDataAccess(builder.Configuration);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<PaymentGatewayDbContext>();
    dbContext.Database.Migrate();
}

app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "PaymentGateway API v1");
    });
}

app.UseHttpsRedirection();

app.MapPost("api/payments", async (PaymentRequest request, IPaymentProcessor processingLogic) =>
{
    var result = await processingLogic.ProcessPaymentAsync(request);
    return new
    {
        Message = result
    };
})
.WithName("ProcessPayment");

app.MapGet("api/payments/{id}", async (Guid id, IPaymentHistoryRepository paymentHistoryRepository) =>
{
    var paymentHistory = await paymentHistoryRepository.GetByIdAsync(id);
    return paymentHistory is null ? Results.NotFound() : Results.Ok(paymentHistory.Payment);
})
.WithName("GetPaymentDetails");

try
{
    app.Run();
}
finally
{
    Log.CloseAndFlush();
}

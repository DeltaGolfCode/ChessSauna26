using Microsoft.EntityFrameworkCore;

using PaymentGateway.Logic;
using PaymentGateway.Logic.DataAccess.EntityMapping;
using PaymentGateway.Logic.DataAccess.Interfaces;
using PaymentGateway.Logic.Models.Request;
using PaymentGateway.Logic.Services.Interfaces;

using Serilog;
using Serilog.Filters;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSerilog((services, loggerConfiguration) => loggerConfiguration
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Logger(requestLog => requestLog
        .Filter.ByIncludingOnly(Matching.FromSource("Serilog.AspNetCore.RequestLoggingMiddleware"))
        .WriteTo.File("logs/requests-.log", rollingInterval: RollingInterval.Day))
    .WriteTo.Logger(applicationLog => applicationLog
        .Filter.ByExcluding(Matching.FromSource("Serilog.AspNetCore.RequestLoggingMiddleware"))
        .WriteTo.File("logs/application-.log", rollingInterval: RollingInterval.Day)));

builder.Services.AddOpenApi();
builder.Services.RegisterPaymentGatewayLogic();
builder.Services.RegisterServices(builder.Configuration);
builder.Services.RegisterDataAccess(builder.Configuration);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<PaymentGatewayDbContext>();

    if (dbContext.Database.IsRelational())
    {
        dbContext.Database.Migrate();
    }
    else
    {
        dbContext.Database.EnsureCreated();
    }
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
    return result;
})
.WithName("ProcessPayment");

app.MapGet("api/payments/{id}", async (Guid id, IPaymentProcessor processingLogic) =>
{
    var paymentResponse = await processingLogic.RetrievePaymentInformationAsync(id);
    return paymentResponse is null ? Results.NotFound() : Results.Ok(paymentResponse);
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

public partial class Program;
using System.Diagnostics.CodeAnalysis;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using PaymentGateway.Logic.DataAccess.EntityMapping;
using PaymentGateway.Logic.DataAccess.Interfaces;
using PaymentGateway.Logic.DataAccess.Repositories;

namespace PaymentGateway.Logic;

[ExcludeFromCodeCoverage]
public static class DependencyInjection
{
    public static void RegisterPaymentGatewayLogic(this IServiceCollection services)
    {
        services.AddScoped<Services.Interfaces.IPaymentProcessor, Services.PaymentProcessor>();
        services.AddScoped<ExternalResources.Interfaces.IBankGateway, ExternalResources.BankGateway>();
    }

    public static void RegisterDataAccess(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<PaymentGatewayDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("PaymentGateway")));

        services.AddScoped<IPaymentHistoryRepository, PaymentHistoryRepository>();
    }
}
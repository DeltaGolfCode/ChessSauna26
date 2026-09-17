using Microsoft.Extensions.DependencyInjection;

namespace PaymentGateway.Logic;

public static class DependencyInjection
{
    public static void RegisterPaymentGatewayLogic(this IServiceCollection services)
    {
        services.AddScoped<Services.Interfaces.IPaymentProcessor, Services.PaymentProcessor>();
        services.AddScoped<ExternalResources.Interfaces.IBankGateway, ExternalResources.BankGateway>();
    }
}


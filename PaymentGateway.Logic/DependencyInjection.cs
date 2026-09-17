using Microsoft.Extensions.DependencyInjection;

namespace PaymentGateway.Logic;

public static class DependencyInjection
{
    public static void RegisterPaymentGatewayLogic(this IServiceCollection services)
    {
        // Register your services here
        services.AddHttpClient();
        services.AddScoped<Services.Interfaces.IPaymentProcessor, Services.PaymentProcessor>();
    }
}


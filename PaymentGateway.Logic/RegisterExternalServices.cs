using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace PaymentGateway.Logic;

public static class RegisterExternalServices
{
    public static void RegisterServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient("BankGateway", client =>
        {
            var bankGatewayConfig = configuration.GetSection("Services:BankGateway");
            var baseUrl = bankGatewayConfig["BaseUrl"]
                ?? throw new InvalidOperationException("BankGateway BaseUrl is not configured in appsettings.json");
            var timeoutSeconds = bankGatewayConfig.GetValue<int>("TimeoutSeconds", 10);

            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri))
            {
                throw new InvalidOperationException($"Invalid BankGateway BaseUrl format: {baseUrl}");
            }

            client.BaseAddress = uri;
            client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
        });
    }
}
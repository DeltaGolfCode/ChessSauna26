using System.Net.Http.Json;

using Microsoft.Extensions.Logging;

using PaymentGateway.Logic.ExternalResources.Interfaces;
using PaymentGateway.Logic.ExternalResources.Models.Request;
using PaymentGateway.Logic.ExternalResources.Models.Response;
using PaymentGateway.Logic.Models.Request;

namespace PaymentGateway.Logic.ExternalResources;

public class BankGateway(IHttpClientFactory httpClientFactory, ILogger<BankGateway> _logger) : IBankGateway
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    public async Task<BankPaymentResponse> SendPaymentRequestAsync(PaymentRequest paymentDetails)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("BankGateway");

            using var response = await client.PostAsJsonAsync("/payments", new BankPaymentRequest(paymentDetails));
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<BankPaymentResponse>()
                ?? throw new InvalidOperationException("Empty response from bank.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending payment request to bank gateway.");
            return null;
        }
    }
}


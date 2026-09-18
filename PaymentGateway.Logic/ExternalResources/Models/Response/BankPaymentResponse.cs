using System.Text.Json.Serialization;

namespace PaymentGateway.Logic.ExternalResources.Models.Response;

public class BankPaymentResponse
{
    [JsonPropertyName("authorized")]
    public bool Authorized { get; set; }

    [JsonPropertyName("authorization_code")]
    public string? AuthorizationCode { get; set; }
}
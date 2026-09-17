using System.Text.Json.Serialization;

using PaymentGateway.Logic.Models.Request;

namespace PaymentGateway.Logic.ExternalResources.Models.Request;

public class BankPaymentRequest(PaymentRequest paymentRequest)
{
    [JsonPropertyName("card_number")]
    public string? CardNumber { get; set; } = paymentRequest.CardNumber;

    [JsonPropertyName("expiry_date")]
    public string ExpiryDate { get; set; } = $"{paymentRequest.ExpiryMonth:D2}/{paymentRequest.ExpiryYear:D4}";

    [JsonPropertyName("currency")]
    public string? Currency { get; set; } = paymentRequest.Currency;

    [JsonPropertyName("amount")]
    public int Amount { get; set; } = paymentRequest.Amount;

    [JsonPropertyName("cvv")]
    public string? Cvv { get; set; } = paymentRequest.Cvv;
}

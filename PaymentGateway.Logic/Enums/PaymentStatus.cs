using System.Text.Json.Serialization;

namespace PaymentGateway.Logic.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PaymentStatus
{
    Authorized,
    Declined,
    Rejected
}


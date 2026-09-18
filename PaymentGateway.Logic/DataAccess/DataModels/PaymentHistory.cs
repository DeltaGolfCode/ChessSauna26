using System.Diagnostics.CodeAnalysis;

using PaymentGateway.Logic.Enums;
using PaymentGateway.Logic.Models.Response;

namespace PaymentGateway.Logic.DataAccess.DataModels;

[ExcludeFromCodeCoverage]
public class PaymentHistory
{
    public Guid Id { get; set; }
    public PaymentStatus Status { get; set; }
    public PaymentResponse Payment { get; set; } = null!;
}
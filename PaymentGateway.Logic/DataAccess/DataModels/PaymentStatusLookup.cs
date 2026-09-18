using System.Diagnostics.CodeAnalysis;

using PaymentGateway.Logic.Enums;

namespace PaymentGateway.Logic.DataAccess.DataModels;

[ExcludeFromCodeCoverage]
public class PaymentStatusLookup
{
    public PaymentStatus Id { get; set; }
    public string Name { get; set; } = null!;
}
using PaymentGateway.Logic.Enums;

namespace PaymentGateway.Logic.DataAccess.DataModels;

public class PaymentStatusLookup
{
    public PaymentStatus Id { get; set; }
    public string Name { get; set; } = null!;
}

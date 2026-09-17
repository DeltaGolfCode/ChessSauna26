using PaymentGateway.Logic.Enums;
using PaymentGateway.Logic.Models.Response;

namespace PaymentGateway.Logic.DataAccess.DataModels;

public class PaymentHistory
{
    public string Id { get; set; }
    public PaymentStatus Status { get; set; }
    public PaymentResponse Payment { get; set; }
}

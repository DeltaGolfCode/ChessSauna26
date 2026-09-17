using PaymentGateway.Logic.Enums;
using PaymentGateway.Logic.Models.Request;

namespace PaymentGateway.Logic.Models.Response
{
    public class PaymentResponse
    {
        public PaymentResponse()
        {            
        }

        public PaymentResponse(PaymentRequest request, PaymentStatus status)
        {
            Id = Guid.NewGuid();
            Status = status;
            CardNumberLastFour = int.Parse(request.CardNumber[^4..]);
            ExpiryMonth = request.ExpiryMonth;
            ExpiryYear = request.ExpiryYear;
            Currency = request.Currency;
            Amount = request.Amount;
        }

        public Guid Id { get; set; }
        public PaymentStatus Status { get; set; }
        public int CardNumberLastFour { get; set; }
        public int ExpiryMonth { get; set; }
        public int ExpiryYear { get; set; }
        public string Currency { get; set; }
        public int Amount { get; set; }
    }
}

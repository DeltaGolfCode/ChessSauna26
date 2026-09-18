using PaymentGateway.Logic.ExternalResources.Models.Response;
using PaymentGateway.Logic.Models.Request;

namespace PaymentGateway.Logic.ExternalResources.Interfaces;

public interface IBankGateway
{
    Task<BankPaymentResponse> SendPaymentRequestAsync(PaymentRequest paymentDetails);
}
using PaymentGateway.Logic.Models.Request;
using PaymentGateway.Logic.Models.Response;

namespace PaymentGateway.Logic.Services.Interfaces
{
    public interface IPaymentProcessor
    {
        Task<PaymentResponse> ProcessPaymentAsync(PaymentRequest paymentDetails);

        Task<PaymentResponse?> RetrievePaymentInformationAsync(Guid paymentReference);
    }
}

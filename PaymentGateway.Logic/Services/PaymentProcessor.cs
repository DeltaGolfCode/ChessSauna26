using System.ComponentModel.DataAnnotations;

using PaymentGateway.Logic.DataAccess.DataModels;
using PaymentGateway.Logic.DataAccess.Interfaces;
using PaymentGateway.Logic.Enums;
using PaymentGateway.Logic.ExternalResources.Interfaces;
using PaymentGateway.Logic.Models.Request;
using PaymentGateway.Logic.Models.Response;
using PaymentGateway.Logic.Services.Interfaces;

namespace PaymentGateway.Logic.Services;

public class PaymentProcessor(IBankGateway bankGateway, IPaymentHistoryRepository paymentHistoryRepository) : IPaymentProcessor
{
    public async Task<PaymentResponse> ProcessPaymentAsync(PaymentRequest paymentDetails)
    {
        var validationContext = new ValidationContext(paymentDetails);
        var validationResults = new List<ValidationResult>();

        PaymentResponse paymentResponse = null;

        if (!Validator.TryValidateObject(paymentDetails, validationContext, validationResults, validateAllProperties: true))
        {
            paymentResponse = new PaymentResponse(paymentDetails, PaymentStatus.Rejected);
        }
        else
        {
            var bankResponse = await bankGateway.SendPaymentRequestAsync(paymentDetails);

            paymentResponse = new PaymentResponse(paymentDetails, PaymentStatus.Declined);

            if (bankResponse?.Authorized == true)
            {
                paymentResponse.Status = PaymentStatus.Authorized;
            }

            if (bankResponse is null)
            {
                paymentResponse.Status = PaymentStatus.Rejected;
            }
        }

        await paymentHistoryRepository.CreateAsync(new PaymentHistory
        {
            Id = paymentResponse.Id,
            Status = paymentResponse.Status,
            Payment = paymentResponse
        });

        return paymentResponse;
    }

    public async Task<PaymentResponse?> RetrievePaymentInformationAsync(Guid paymentReference)
    {
        var paymentHistory = await paymentHistoryRepository.GetByIdAsync(paymentReference);
        return paymentHistory?.Payment;
    }
}
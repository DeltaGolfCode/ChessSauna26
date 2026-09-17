using System.ComponentModel.DataAnnotations;

using PaymentGateway.Logic.DataAccess.DataModels;
using PaymentGateway.Logic.DataAccess.Interfaces;
using PaymentGateway.Logic.Enums;
using PaymentGateway.Logic.ExternalResources.Interfaces;
using PaymentGateway.Logic.Models.Request;
using PaymentGateway.Logic.Models.Response;
using PaymentGateway.Logic.Services.Interfaces;

namespace PaymentGateway.Logic.Services;

public class PaymentProcessor(IBankGateway _bankGateway, IPaymentHistoryRepository _paymentHistoryRepository) : IPaymentProcessor
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
            var bankResponse = await _bankGateway.SendPaymentRequestAsync(paymentDetails);

            paymentResponse = new PaymentResponse(paymentDetails, PaymentStatus.Declined);

            if (bankResponse.Authorized)
            {
                paymentResponse.Status = PaymentStatus.Authorized;
            }
        }

        await _paymentHistoryRepository.CreateAsync(new PaymentHistory
        {
            Id = paymentResponse.Id,
            Status = paymentResponse.Status,
            Payment = paymentResponse
        });

        return paymentResponse;
    }
}


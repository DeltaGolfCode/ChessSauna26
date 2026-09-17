using System.ComponentModel.DataAnnotations;

using PaymentGateway.Logic.Enums;
using PaymentGateway.Logic.ExternalResources.Interfaces;
using PaymentGateway.Logic.Models.Request;
using PaymentGateway.Logic.Models.Response;
using PaymentGateway.Logic.Services.Interfaces;

namespace PaymentGateway.Logic.Services;

public class PaymentProcessor(IBankGateway _bankGateway) : IPaymentProcessor
{
    public async Task<PaymentResponse> ProcessPaymentAsync(PaymentRequest paymentDetails)
    {
        var validationContext = new ValidationContext(paymentDetails);
        var validationResults = new List<ValidationResult>();

        if (!Validator.TryValidateObject(paymentDetails, validationContext, validationResults, validateAllProperties: true))
        {
            return new PaymentResponse(paymentDetails, PaymentStatus.Rejected);
        }

        var bankResponse = await _bankGateway.SendPaymentRequestAsync(paymentDetails);

        var response = new PaymentResponse(paymentDetails, PaymentStatus.Declined); 

        if (bankResponse.Authorized)
        {
            response.Status = PaymentStatus.Authorized;
        }

        return response;
    }
}


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
        var bankResponse = await _bankGateway.SendPaymentRequestAsync(paymentDetails);

        var response = new PaymentResponse(paymentDetails, PaymentStatus.Declined); 

        if (bankResponse.Authorized)
        {
            response.Status = PaymentStatus.Authorized;
        }

        // Pass payment to Bank
        // Save response to database.
        // Return response to client.

        return response;
    }
}


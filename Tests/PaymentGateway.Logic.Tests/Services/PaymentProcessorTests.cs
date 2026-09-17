using NSubstitute;
using PaymentGateway.Logic.Enums;
using PaymentGateway.Logic.ExternalResources.Interfaces;
using PaymentGateway.Logic.ExternalResources.Models.Response;
using PaymentGateway.Logic.Models.Request;
using PaymentGateway.Logic.Services;

namespace PaymentGateway.Logic.Tests.Services;

public class PaymentProcessorTests
{
    private static PaymentRequest CreateValidPaymentRequest()
    {
        return new PaymentRequest
        {
            CardNumber = "4532123456789012",
            ExpiryMonth = 12,
            ExpiryYear = DateTime.UtcNow.Year + 1,
            Currency = "GBP",
            Amount = 100,
            Cvv = "123"
        };
    }

    [Fact]
    public async Task ProcessPaymentAsync_InvalidCardNumber_ReturnsRejectedWithoutCallingBank()
    {
        // Arrange
        var request = CreateValidPaymentRequest();
        request.CardNumber = "123";

        var bankGateway = Substitute.For<IBankGateway>();
        var processor = new PaymentProcessor(bankGateway);

        // Act
        var response = await processor.ProcessPaymentAsync(request);

        // Assert
        Assert.Equal(PaymentStatus.Rejected, response.Status);
        await bankGateway.DidNotReceive().SendPaymentRequestAsync(Arg.Any<PaymentRequest>());
    }

    [Fact]
    public async Task ProcessPaymentAsync_InvalidExpiryDate_ReturnsRejectedWithoutCallingBank()
    {
        // Arrange
        var request = CreateValidPaymentRequest();
        request.ExpiryYear = DateTime.UtcNow.Year - 1;

        var bankGateway = Substitute.For<IBankGateway>();
        var processor = new PaymentProcessor(bankGateway);

        // Act
        var response = await processor.ProcessPaymentAsync(request);

        // Assert
        Assert.Equal(PaymentStatus.Rejected, response.Status);
        await bankGateway.DidNotReceive().SendPaymentRequestAsync(Arg.Any<PaymentRequest>());
    }

    [Fact]
    public async Task ProcessPaymentAsync_ValidRequest_CallsBankAndReturnsAuthorized()
    {
        // Arrange
        var request = CreateValidPaymentRequest();
        var bankResponse = new BankPaymentResponse { Authorized = true };

        var bankGateway = Substitute.For<IBankGateway>();
        bankGateway.SendPaymentRequestAsync(request).Returns(bankResponse);

        var processor = new PaymentProcessor(bankGateway);

        // Act
        var response = await processor.ProcessPaymentAsync(request);

        // Assert
        Assert.Equal(PaymentStatus.Authorized, response.Status);
        await bankGateway.Received(1).SendPaymentRequestAsync(request);
    }

    [Fact]
    public async Task ProcessPaymentAsync_ValidRequestDeclinedByBank_ReturnsDeclined()
    {
        // Arrange
        var request = CreateValidPaymentRequest();
        var bankResponse = new BankPaymentResponse { Authorized = false };

        var bankGateway = Substitute.For<IBankGateway>();
        bankGateway.SendPaymentRequestAsync(request).Returns(bankResponse);

        var processor = new PaymentProcessor(bankGateway);

        // Act
        var response = await processor.ProcessPaymentAsync(request);

        // Assert
        Assert.Equal(PaymentStatus.Declined, response.Status);
        await bankGateway.Received(1).SendPaymentRequestAsync(request);
    }

    [Fact]
    public async Task ProcessPaymentAsync_ValidRequest_ReturnsResponseWithCorrectCardNumberLastFour()
    {
        // Arrange
        var request = CreateValidPaymentRequest();
        var bankResponse = new BankPaymentResponse { Authorized = true };

        var bankGateway = Substitute.For<IBankGateway>();
        bankGateway.SendPaymentRequestAsync(request).Returns(bankResponse);

        var processor = new PaymentProcessor(bankGateway);

        // Act
        var response = await processor.ProcessPaymentAsync(request);

        // Assert
        Assert.Equal(9012, response.CardNumberLastFour);
    }

    [Fact]
    public async Task ProcessPaymentAsync_ValidRequest_ReturnsResponseWithAllRequestDetails()
    {
        // Arrange
        var request = CreateValidPaymentRequest();
        var bankResponse = new BankPaymentResponse { Authorized = true };

        var bankGateway = Substitute.For<IBankGateway>();
        bankGateway.SendPaymentRequestAsync(request).Returns(bankResponse);

        var processor = new PaymentProcessor(bankGateway);

        // Act
        var response = await processor.ProcessPaymentAsync(request);

        // Assert
        Assert.NotNull(response.Id);
        Assert.Equal(request.ExpiryMonth, response.ExpiryMonth);
        Assert.Equal(request.ExpiryYear, response.ExpiryYear);
        Assert.Equal(request.Currency, response.Currency);
        Assert.Equal(request.Amount, response.Amount);
    }

    [Fact]
    public async Task ProcessPaymentAsync_InvalidRequest_ReturnsResponseWithSafeCardNumberExtraction()
    {
        // Arrange
        var request = CreateValidPaymentRequest();
        request.CardNumber = "123";

        var bankGateway = Substitute.For<IBankGateway>();
        var processor = new PaymentProcessor(bankGateway);

        // Act
        var response = await processor.ProcessPaymentAsync(request);

        // Assert
        Assert.NotNull(response.Id);
        Assert.Equal(0, response.CardNumberLastFour);
        Assert.Equal(PaymentStatus.Rejected, response.Status);
    }

    [Fact]
    public async Task ProcessPaymentAsync_MultipleValidationErrors_ReturnsRejectedWithoutCallingBank()
    {
        // Arrange
        var request = new PaymentRequest
        {
            CardNumber = "123",
            ExpiryMonth = 13,
            ExpiryYear = DateTime.UtcNow.Year - 1,
            Currency = "XXX",
            Amount = 0,
            Cvv = "12"
        };

        var bankGateway = Substitute.For<IBankGateway>();
        var processor = new PaymentProcessor(bankGateway);

        // Act
        var response = await processor.ProcessPaymentAsync(request);

        // Assert
        Assert.Equal(PaymentStatus.Rejected, response.Status);
        await bankGateway.DidNotReceive().SendPaymentRequestAsync(Arg.Any<PaymentRequest>());
    }

    [Fact]
    public async Task ProcessPaymentAsync_ValidAuthorizationRequest_CallsBankWithCorrectRequest()
    {
        // Arrange
        var request = CreateValidPaymentRequest();
        var bankResponse = new BankPaymentResponse { Authorized = true };

        var bankGateway = Substitute.For<IBankGateway>();
        bankGateway.SendPaymentRequestAsync(request).Returns(bankResponse);

        var processor = new PaymentProcessor(bankGateway);

        // Act
        var response = await processor.ProcessPaymentAsync(request);

        // Assert
        await bankGateway.Received(1).SendPaymentRequestAsync(Arg.Is<PaymentRequest>(r =>
            r.CardNumber == request.CardNumber &&
            r.ExpiryMonth == request.ExpiryMonth &&
            r.ExpiryYear == request.ExpiryYear &&
            r.Cvv == request.Cvv &&
            r.Currency == request.Currency &&
            r.Amount == request.Amount));
    }
}




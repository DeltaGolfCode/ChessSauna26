using System.Diagnostics.CodeAnalysis;

using NSubstitute;

using PaymentGateway.Logic.DataAccess.DataModels;
using PaymentGateway.Logic.DataAccess.Interfaces;
using PaymentGateway.Logic.Enums;
using PaymentGateway.Logic.ExternalResources.Interfaces;
using PaymentGateway.Logic.ExternalResources.Models.Response;
using PaymentGateway.Logic.Models.Request;
using PaymentGateway.Logic.Models.Response;
using PaymentGateway.Logic.Services;

namespace PaymentGateway.Logic.Tests.Services;

[ExcludeFromCodeCoverage]
public class PaymentProcessorTests
{
    [Fact]
    public async Task ProcessPaymentAsync_InvalidCardNumber_ReturnsRejectedWithoutCallingBank()
    {
        // Arrange
        var request = CreateValidPaymentRequest();
        request.CardNumber = "123";

        var bankGateway = Substitute.For<IBankGateway>();
        var processor = CreateProcessor(bankGateway);

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
        var processor = CreateProcessor(bankGateway);

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

        var processor = CreateProcessor(bankGateway);

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

        var processor = CreateProcessor(bankGateway);

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

        var processor = CreateProcessor(bankGateway);

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

        var processor = CreateProcessor(bankGateway);

        // Act
        var response = await processor.ProcessPaymentAsync(request);

        // Assert
        Assert.Multiple(
            () => Assert.NotEqual(Guid.Empty, response.Id),
            () => Assert.Equal(request.ExpiryMonth, response.ExpiryMonth),
            () => Assert.Equal(request.ExpiryYear, response.ExpiryYear),
            () => Assert.Equal(request.Currency, response.Currency),
            () => Assert.Equal(request.Amount, response.Amount));
    }

    [Fact]
    public async Task ProcessPaymentAsync_InvalidRequest_ReturnsResponseWithSafeCardNumberExtraction()
    {
        // Arrange
        var request = CreateValidPaymentRequest();
        request.CardNumber = "123";

        var bankGateway = Substitute.For<IBankGateway>();
        var processor = CreateProcessor(bankGateway);

        // Act
        var response = await processor.ProcessPaymentAsync(request);

        // Assert
        Assert.Multiple(
            () => Assert.NotEqual(Guid.Empty, response.Id),
            () => Assert.Equal(0, response.CardNumberLastFour),
            () => Assert.Equal(PaymentStatus.Rejected, response.Status));
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
        var processor = CreateProcessor(bankGateway);

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

        var processor = CreateProcessor(bankGateway);

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

    [Fact]
    public async Task ProcessPaymentAsync_ValidRequest_SavesAuthorizedPaymentToHistory()
    {
        // Arrange
        var request = CreateValidPaymentRequest();
        var bankResponse = new BankPaymentResponse { Authorized = true };

        var bankGateway = Substitute.For<IBankGateway>();
        bankGateway.SendPaymentRequestAsync(request).Returns(bankResponse);

        var paymentHistoryRepository = Substitute.For<IPaymentHistoryRepository>();
        var processor = CreateProcessor(bankGateway, paymentHistoryRepository);

        // Act
        var response = await processor.ProcessPaymentAsync(request);

        // Assert
        await paymentHistoryRepository.Received(1).CreateAsync(Arg.Is<PaymentHistory>(h =>
            h.Id == response.Id &&
            h.Status == PaymentStatus.Authorized &&
            h.Payment == response));
    }

    [Fact]
    public async Task ProcessPaymentAsync_InvalidRequest_SavesRejectedPaymentToHistory()
    {
        // Arrange
        var request = CreateValidPaymentRequest();
        request.CardNumber = "123";

        var bankGateway = Substitute.For<IBankGateway>();
        var paymentHistoryRepository = Substitute.For<IPaymentHistoryRepository>();
        var processor = CreateProcessor(bankGateway, paymentHistoryRepository);

        // Act
        var response = await processor.ProcessPaymentAsync(request);

        // Assert
        await paymentHistoryRepository.Received(1).CreateAsync(Arg.Is<PaymentHistory>(h =>
            h.Id == response.Id &&
            h.Status == PaymentStatus.Rejected &&
            h.Payment == response));
    }

    [Fact]
    public async Task RetrievePaymentInformationAsync_ExistingReference_ReturnsAssociatedPayment()
    {
        // Arrange
        var paymentReference = Guid.NewGuid();
        var expectedPayment = new PaymentResponse
        {
            Id = paymentReference,
            Status = PaymentStatus.Authorized
        };
        var paymentHistory = new PaymentHistory
        {
            Id = paymentReference,
            Status = PaymentStatus.Authorized,
            Payment = expectedPayment
        };

        var bankGateway = Substitute.For<IBankGateway>();
        var paymentHistoryRepository = Substitute.For<IPaymentHistoryRepository>();
        paymentHistoryRepository.GetByIdAsync(paymentReference).Returns(paymentHistory);

        var processor = CreateProcessor(bankGateway, paymentHistoryRepository);

        // Act
        var result = await processor.RetrievePaymentInformationAsync(paymentReference);

        // Assert
        Assert.Same(expectedPayment, result);
    }

    [Fact]
    public async Task RetrievePaymentInformationAsync_UnknownReference_ReturnsNull()
    {
        // Arrange
        var paymentReference = Guid.NewGuid();

        var bankGateway = Substitute.For<IBankGateway>();
        var paymentHistoryRepository = Substitute.For<IPaymentHistoryRepository>();
        paymentHistoryRepository.GetByIdAsync(paymentReference).Returns((PaymentHistory?)null);

        var processor = CreateProcessor(bankGateway, paymentHistoryRepository);

        // Act
        var result = await processor.RetrievePaymentInformationAsync(paymentReference);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task RetrievePaymentInformationAsync_ValidReference_QueriesRepositoryWithProvidedReference()
    {
        // Arrange
        var paymentReference = Guid.NewGuid();

        var bankGateway = Substitute.For<IBankGateway>();
        var paymentHistoryRepository = Substitute.For<IPaymentHistoryRepository>();
        paymentHistoryRepository.GetByIdAsync(paymentReference).Returns((PaymentHistory?)null);

        var processor = CreateProcessor(bankGateway, paymentHistoryRepository);

        // Act
        await processor.RetrievePaymentInformationAsync(paymentReference);

        // Assert
        await paymentHistoryRepository.Received(1).GetByIdAsync(paymentReference);
    }

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

    private static PaymentProcessor CreateProcessor(
        IBankGateway bankGateway,
        IPaymentHistoryRepository? paymentHistoryRepository = null)
    {
        return new PaymentProcessor(bankGateway, paymentHistoryRepository ?? Substitute.For<IPaymentHistoryRepository>());
    }
}


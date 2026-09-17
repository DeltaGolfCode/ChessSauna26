using PaymentGateway.Logic.Enums;
using PaymentGateway.Logic.Models.Request;
using PaymentGateway.Logic.Models.Response;

namespace PaymentGateway.Logic.Tests.Models.Response;

public class PaymentResponseTests
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
    public void Constructor_ValidRequest_GeneratesNonEmptyId()
    {
        // Arrange
        var request = CreateValidPaymentRequest();

        // Act
        var response = new PaymentResponse(request, PaymentStatus.Authorized);

        // Assert
        Assert.NotEqual(Guid.Empty, response.Id);
    }

    [Fact]
    public void Constructor_ValidRequest_CopiesRequestDetails()
    {
        // Arrange
        var request = CreateValidPaymentRequest();

        // Act
        var response = new PaymentResponse(request, PaymentStatus.Authorized);

        // Assert
        Assert.Multiple(
            () => Assert.Equal(request.ExpiryMonth, response.ExpiryMonth),
            () => Assert.Equal(request.ExpiryYear, response.ExpiryYear),
            () => Assert.Equal(request.Currency, response.Currency),
            () => Assert.Equal(request.Amount, response.Amount),
            () => Assert.Equal(PaymentStatus.Authorized, response.Status));
    }

    [Fact]
    public void Constructor_CardNumberWithFourOrMoreDigits_ExtractsLastFourDigits()
    {
        // Arrange
        var request = CreateValidPaymentRequest();
        request.CardNumber = "4532123456789012";

        // Act
        var response = new PaymentResponse(request, PaymentStatus.Authorized);

        // Assert
        Assert.Equal(9012, response.CardNumberLastFour);
    }

    [Fact]
    public void Constructor_CardNumberShorterThanFourDigits_ReturnsZeroForLastFour()
    {
        // Arrange
        var request = CreateValidPaymentRequest();
        request.CardNumber = "12";

        // Act
        var response = new PaymentResponse(request, PaymentStatus.Rejected);

        // Assert
        Assert.Equal(0, response.CardNumberLastFour);
    }

    [Fact]
    public void Constructor_NullCardNumber_ReturnsZeroForLastFour()
    {
        // Arrange
        var request = CreateValidPaymentRequest();
        request.CardNumber = null;

        // Act
        var response = new PaymentResponse(request, PaymentStatus.Rejected);

        // Assert
        Assert.Equal(0, response.CardNumberLastFour);
    }

    [Fact]
    public void Constructor_MultipleInstances_GeneratesUniqueIds()
    {
        // Arrange
        var request = CreateValidPaymentRequest();

        // Act
        var first = new PaymentResponse(request, PaymentStatus.Authorized);
        var second = new PaymentResponse(request, PaymentStatus.Authorized);

        // Assert
        Assert.NotEqual(first.Id, second.Id);
    }
}

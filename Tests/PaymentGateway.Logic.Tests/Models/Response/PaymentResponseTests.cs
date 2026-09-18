using System.Diagnostics.CodeAnalysis;

using PaymentGateway.Logic.Enums;
using PaymentGateway.Logic.Models.Request;
using PaymentGateway.Logic.Models.Response;

namespace PaymentGateway.Logic.Tests.Models.Response;

[ExcludeFromCodeCoverage]
public class PaymentResponseTests
{
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

    [Theory]
    [InlineData("4532123456789012", "9012")]
    [InlineData("4532123456780012", "0012")]
    public void Constructor_CardNumberWithFourOrMoreDigits_ExtractsLastFourDigits(string cardNumber, string expectedLastFour)
    {
        // Arrange
        var request = CreateValidPaymentRequest();
        request.CardNumber = cardNumber;

        // Act
        var response = new PaymentResponse(request, PaymentStatus.Authorized);

        // Assert
        Assert.Equal(expectedLastFour, response.CardNumberLastFour);
    }

    [Fact]
    public void Constructor_CardNumberShorterThanFourDigits_ReturnsEmptyForLastFour()
    {
        // Arrange
        var request = CreateValidPaymentRequest();
        request.CardNumber = "12";

        // Act
        var response = new PaymentResponse(request, PaymentStatus.Rejected);

        // Assert
        Assert.Equal(string.Empty, response.CardNumberLastFour);
    }

    [Fact]
    public void Constructor_NullCardNumber_ReturnsEmptyForLastFour()
    {
        // Arrange
        var request = CreateValidPaymentRequest();
        request.CardNumber = null;

        // Act
        var response = new PaymentResponse(request, PaymentStatus.Rejected);

        // Assert
        Assert.Equal(string.Empty, response.CardNumberLastFour);
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
}

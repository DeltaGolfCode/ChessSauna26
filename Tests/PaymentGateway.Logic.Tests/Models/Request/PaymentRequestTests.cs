using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

using PaymentGateway.Logic.Models.Request;

namespace PaymentGateway.Logic.Tests.Models.Request;

[ExcludeFromCodeCoverage]
public class PaymentRequestValidationTests
{
    private PaymentRequest CreateValidPaymentRequest()
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

    private IEnumerable<ValidationResult> ValidateRequest(PaymentRequest request)
    {
        var context = new ValidationContext(request);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(request, context, results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void CardNumber_Required_FailsWhenNull()
    {
        // Arrange
        var request = CreateValidPaymentRequest();
        request.CardNumber = null;

        // Act
        var validationErrors = ValidateRequest(request);

        // Assert
        Assert.Single(validationErrors, e => e.MemberNames.Contains(nameof(PaymentRequest.CardNumber)));
    }

    [Fact]
    public void CardNumber_TooShort_FailsWhenLessThan14Digits()
    {
        // Arrange
        var request = CreateValidPaymentRequest();
        request.CardNumber = "12345678901";

        // Act
        var validationErrors = ValidateRequest(request);

        // Assert
        Assert.Single(validationErrors, e => e.MemberNames.Contains(nameof(PaymentRequest.CardNumber)));
    }

    [Fact]
    public void CardNumber_TooLong_FailsWhenMoreThan19Digits()
    {
        // Arrange
        var request = CreateValidPaymentRequest();
        request.CardNumber = "12345678901234567890";

        // Act
        var validationErrors = ValidateRequest(request);

        // Assert
        Assert.Single(validationErrors, e => e.MemberNames.Contains(nameof(PaymentRequest.CardNumber)));
    }

    [Fact]
    public void CardNumber_InvalidFormat_FailsWhenContainsNonNumeric()
    {
        // Arrange
        var request = CreateValidPaymentRequest();
        request.CardNumber = "453212345678901A";

        // Act
        var validationErrors = ValidateRequest(request);

        // Assert
        Assert.Single(validationErrors, e => e.MemberNames.Contains(nameof(PaymentRequest.CardNumber)));
    }

    [Theory]
    [InlineData("12345678901234")]
    [InlineData("1234567890123456789")]
    [InlineData("4532123456789012")]
    public void CardNumber_ValidLength_Passes(string cardNumber)
    {
        // Arrange
        var request = CreateValidPaymentRequest();
        request.CardNumber = cardNumber;

        // Act
        var validationErrors = ValidateRequest(request);

        // Assert
        Assert.Empty(validationErrors.Where(e => e.MemberNames.Contains(nameof(PaymentRequest.CardNumber))));
    }

    [Fact]
    public void ExpiryMonth_ZeroValue_Fails()
    {
        // Arrange
        var request = CreateValidPaymentRequest();
        request.ExpiryMonth = 0;

        // Act
        var validationErrors = ValidateRequest(request);

        // Assert
        Assert.Single(validationErrors, e => e.MemberNames.Contains(nameof(PaymentRequest.ExpiryMonth)));
    }

    [Fact]
    public void ExpiryMonth_LargerThan12_Fails()
    {
        // Arrange
        var request = CreateValidPaymentRequest();
        request.ExpiryMonth = 13;

        // Act
        var validationErrors = ValidateRequest(request);

        // Assert
        Assert.Single(validationErrors, e => e.MemberNames.Contains(nameof(PaymentRequest.ExpiryMonth)));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(6)]
    [InlineData(12)]
    public void ExpiryMonth_ValidMonths_Passes(int month)
    {
        // Arrange
        var request = CreateValidPaymentRequest();
        request.ExpiryMonth = month;

        // Act
        var validationErrors = ValidateRequest(request);

        // Assert
        Assert.Empty(validationErrors.Where(e => e.MemberNames.Contains(nameof(PaymentRequest.ExpiryMonth))));
    }

    [Fact]
    public void Currency_Required_FailsWhenNull()
    {
        // Arrange
        var request = CreateValidPaymentRequest();
        request.Currency = null;

        // Act
        var validationErrors = ValidateRequest(request);

        // Assert
        Assert.Single(validationErrors, e => e.MemberNames.Contains(nameof(PaymentRequest.Currency)));
    }

    [Fact]
    public void Currency_WrongLength_FailsWhenLessThan3Characters()
    {
        // Arrange
        var request = CreateValidPaymentRequest();
        request.Currency = "GB";

        // Act
        var validationErrors = ValidateRequest(request);

        // Assert
        Assert.Single(validationErrors, e => e.MemberNames.Contains(nameof(PaymentRequest.Currency)));
    }

    [Fact]
    public void Currency_WrongLength_FailsWhenMoreThan3Characters()
    {
        // Arrange
        var request = CreateValidPaymentRequest();
        request.Currency = "GBPA";

        // Act
        var validationErrors = ValidateRequest(request);

        // Assert
        Assert.Single(validationErrors, e => e.MemberNames.Contains(nameof(PaymentRequest.Currency)));
    }

    [Fact]
    public void Currency_NotAllowed_FailsWhenNotInAllowedList()
    {
        // Arrange
        var request = CreateValidPaymentRequest();
        request.Currency = "JPY";

        // Act
        var customValidationErrors = request.Validate(new ValidationContext(request));

        // Assert
        Assert.Single(customValidationErrors, e => e.MemberNames.Contains(nameof(PaymentRequest.Currency)));
    }

    [Theory]
    [InlineData("GBP")]
    [InlineData("USD")]
    [InlineData("EUR")]
    public void Currency_Allowed_Passes(string currency)
    {
        // Arrange
        var request = CreateValidPaymentRequest();
        request.Currency = currency;

        // Act
        var customValidationErrors = request.Validate(new ValidationContext(request));

        // Assert
        Assert.Empty(customValidationErrors.Where(e => e.MemberNames.Contains(nameof(PaymentRequest.Currency))));
    }

    [Fact]
    public void Amount_ZeroOrNegative_FailsWhenZero()
    {
        // Arrange
        var request = CreateValidPaymentRequest();
        request.Amount = 0;

        // Act
        var validationErrors = ValidateRequest(request);

        // Assert
        Assert.Single(validationErrors, e => e.MemberNames.Contains(nameof(PaymentRequest.Amount)));
    }

    [Fact]
    public void Amount_ZeroOrNegative_FailsWhenNegative()
    {
        // Arrange
        var request = CreateValidPaymentRequest();
        request.Amount = -100;

        // Act
        var validationErrors = ValidateRequest(request);

        // Assert
        Assert.Single(validationErrors, e => e.MemberNames.Contains(nameof(PaymentRequest.Amount)));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(999999)]
    public void Amount_PositiveValue_Passes(int amount)
    {
        // Arrange
        var request = CreateValidPaymentRequest();
        request.Amount = amount;

        // Act
        var validationErrors = ValidateRequest(request);

        // Assert
        Assert.Empty(validationErrors.Where(e => e.MemberNames.Contains(nameof(PaymentRequest.Amount))));
    }

    [Fact]
    public void Cvv_Required_FailsWhenNull()
    {
        // Arrange
        var request = CreateValidPaymentRequest();
        request.Cvv = null;

        // Act
        var validationErrors = ValidateRequest(request);

        // Assert
        Assert.Single(validationErrors, e => e.MemberNames.Contains(nameof(PaymentRequest.Cvv)));
    }

    [Fact]
    public void Cvv_TooShort_FailsWhenLessThan3Digits()
    {
        // Arrange
        var request = CreateValidPaymentRequest();
        request.Cvv = "12";

        // Act
        var validationErrors = ValidateRequest(request);

        // Assert
        Assert.Single(validationErrors, e => e.MemberNames.Contains(nameof(PaymentRequest.Cvv)));
    }

    [Fact]
    public void Cvv_TooLong_FailsWhenMoreThan4Digits()
    {
        // Arrange
        var request = CreateValidPaymentRequest();
        request.Cvv = "12345";

        // Act
        var validationErrors = ValidateRequest(request);

        // Assert
        Assert.Single(validationErrors, e => e.MemberNames.Contains(nameof(PaymentRequest.Cvv)));
    }

    [Fact]
    public void Cvv_InvalidFormat_FailsWhenContainsNonNumeric()
    {
        // Arrange
        var request = CreateValidPaymentRequest();
        request.Cvv = "12A";

        // Act
        var validationErrors = ValidateRequest(request);

        // Assert
        Assert.Single(validationErrors, e => e.MemberNames.Contains(nameof(PaymentRequest.Cvv)));
    }

    [Theory]
    [InlineData("123")]
    [InlineData("1234")]
    public void Cvv_ValidLength_Passes(string cvv)
    {
        // Arrange
        var request = CreateValidPaymentRequest();
        request.Cvv = cvv;

        // Act
        var validationErrors = ValidateRequest(request);

        // Assert
        Assert.Empty(validationErrors.Where(e => e.MemberNames.Contains(nameof(PaymentRequest.Cvv))));
    }

    [Fact]
    public void ExpiryDate_InThePast_Fails()
    {
        // Arrange
        var request = CreateValidPaymentRequest();
        request.ExpiryYear = DateTime.UtcNow.Year - 1;
        request.ExpiryMonth = 1;

        // Act
        var customValidationErrors = request.Validate(new ValidationContext(request)).ToList();

        // Assert
        Assert.NotEmpty(customValidationErrors);
        Assert.Single(customValidationErrors, e => 
            e.MemberNames.Contains(nameof(PaymentRequest.ExpiryMonth)) ||
            e.MemberNames.Contains(nameof(PaymentRequest.ExpiryYear)));
    }

    [Fact]
    public void ExpiryDate_NextMonth_Passes()
    {
        // Arrange
        var request = CreateValidPaymentRequest();
        var nextMonth = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(1);

        request.ExpiryYear = nextMonth.Year;
        request.ExpiryMonth = nextMonth.Month;

        // Act
        var customValidationErrors = request.Validate(new ValidationContext(request)).ToList();

        // Assert
        Assert.Empty(customValidationErrors.Where(e =>
            e.MemberNames.Contains(nameof(PaymentRequest.ExpiryMonth)) ||
            e.MemberNames.Contains(nameof(PaymentRequest.ExpiryYear))));
    }

    [Fact]
    public void ExpiryDate_FarFuture_Passes()
    {
        // Arrange
        var request = CreateValidPaymentRequest();
        request.ExpiryYear = DateTime.UtcNow.Year + 5;
        request.ExpiryMonth = 12;

        // Act
        var customValidationErrors = request.Validate(new ValidationContext(request)).ToList();

        // Assert
        Assert.Empty(customValidationErrors.Where(e =>
            e.MemberNames.Contains(nameof(PaymentRequest.ExpiryMonth)) ||
            e.MemberNames.Contains(nameof(PaymentRequest.ExpiryYear))));
    }

    [Fact]
    public void ValidPaymentRequest_AllValidData_Passes()
    {
        // Arrange
        var request = CreateValidPaymentRequest();

        // Act
        var validationErrors = ValidateRequest(request);
        var customValidationErrors = request.Validate(new ValidationContext(request));

        // Assert
        Assert.Empty(validationErrors);
        Assert.Empty(customValidationErrors);
    }

    [Fact]
    public void PaymentRequest_WithMultipleErrors_ReturnsAllErrors()
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

        // Act
        var validationErrors = ValidateRequest(request);
        var customValidationErrors = request.Validate(new ValidationContext(request));

        // Assert
        var totalErrors = validationErrors.Concat(customValidationErrors);
        Assert.NotEmpty(totalErrors);
    }
}


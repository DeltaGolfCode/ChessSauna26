using System.ComponentModel.DataAnnotations;

namespace PaymentGateway.Logic.Models.Request
{
    public class PaymentRequest : IValidatableObject
    {
        [Required]
        [StringLength(19, MinimumLength = 14, ErrorMessage = "Invalid card number")]
        [RegularExpression("^[0-9]+$", ErrorMessage = "Invalid card number")]
        public string? CardNumber { get; set; }

        [Required]
        [Range(1, 12, ErrorMessage = "Invalid expiry month")]
        public int ExpiryMonth { get; set; }

        [Required]
        [Range(2026, 2126, ErrorMessage = "Invalid expiry year")]
        public int ExpiryYear { get; set; }

        [Required]
        [StringLength(3, MinimumLength = 3, ErrorMessage = "Invalid currency code")]
        public string? Currency { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Amount must be greater than zero")]
        public int Amount { get; set; }

        [Required]
        [StringLength(4, MinimumLength = 3, ErrorMessage = "Invalid CVV code")]
        [RegularExpression("^[0-9]+$", ErrorMessage = "Invalid CVV code")]
        public string? Cvv { get; set; }

        private static readonly string[] AllowedCurrencies = ["GBP", "USD", "EUR"];

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var lastDayOfExpiryMonth = new DateOnly(ExpiryYear, ExpiryMonth, 1)
                .AddMonths(1)
                .AddDays(-1);

            if (lastDayOfExpiryMonth < DateOnly.FromDateTime(DateTime.UtcNow))
            {
                yield return new ValidationResult(
                    "Expiry date must be in the future",
                    [nameof(ExpiryMonth), nameof(ExpiryYear)]);
            }

            if (!AllowedCurrencies.Contains(Currency, StringComparer.OrdinalIgnoreCase))
            {
                yield return new ValidationResult(
                    $"Currency code is not recognised",
                    [nameof(Currency)]);
            }
        }
    }
}

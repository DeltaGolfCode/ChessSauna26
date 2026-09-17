using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using NSubstitute;

using PaymentGateway.Logic.DataAccess.EntityMapping;
using PaymentGateway.Logic.Enums;
using PaymentGateway.Logic.ExternalResources.Interfaces;
using PaymentGateway.Logic.ExternalResources.Models.Response;
using PaymentGateway.Logic.Models.Request;
using PaymentGateway.Logic.Models.Response;

namespace PaymentGatway.Api.IntegrationTests
{
    public class ProcessPaymentIntegrationTests
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        [Fact]
        public async Task ProcessesAuthorizedPaymentSuccessfully()
        {
            // Arrange
            var bankGateway = Substitute.For<IBankGateway>();
            bankGateway.SendPaymentRequestAsync(Arg.Any<PaymentRequest>())
                .Returns(new BankPaymentResponse { Authorized = true, AuthorizationCode = Guid.NewGuid().ToString() });

            using var factory = CreateFactory(bankGateway);
            var client = factory.CreateClient();
            var request = CreateValidPaymentRequest(cardNumber: "4000000000000001");

            // Act
            var response = await client.PostAsJsonAsync("api/payments", request);
            var paymentResponse = await ReadPaymentResponseAsync(response);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(paymentResponse);
            Assert.Equal(PaymentStatus.Authorized, paymentResponse.Status);
        }

        [Fact]
        public async Task ProcessesDeclinedPaymentWhenBankDeclines()
        {
            // Arrange
            var bankGateway = Substitute.For<IBankGateway>();
            bankGateway.SendPaymentRequestAsync(Arg.Any<PaymentRequest>())
                .Returns(new BankPaymentResponse { Authorized = false, AuthorizationCode = "" });

            using var factory = CreateFactory(bankGateway);
            var client = factory.CreateClient();
            var request = CreateValidPaymentRequest(cardNumber: "4000000000000002");

            // Act
            var response = await client.PostAsJsonAsync("api/payments", request);
            var paymentResponse = await ReadPaymentResponseAsync(response);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(paymentResponse);
            Assert.Equal(PaymentStatus.Declined, paymentResponse.Status);
        }

        [Fact]
        public async Task RejectsPaymentWhenCardNumberIsInvalid()
        {
            // Arrange
            var bankGateway = Substitute.For<IBankGateway>();
            using var factory = CreateFactory(bankGateway);
            var client = factory.CreateClient();
            var request = CreateValidPaymentRequest(cardNumber: "1234");

            // Act
            var response = await client.PostAsJsonAsync("api/payments", request);
            var paymentResponse = await ReadPaymentResponseAsync(response);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(paymentResponse);
            Assert.Equal(PaymentStatus.Rejected, paymentResponse.Status);
            await bankGateway.DidNotReceive().SendPaymentRequestAsync(Arg.Any<PaymentRequest>());
        }

        [Fact]
        public async Task RejectsPaymentWhenCvvIsInvalid()
        {
            // Arrange
            var bankGateway = Substitute.For<IBankGateway>();
            using var factory = CreateFactory(bankGateway);
            var client = factory.CreateClient();
            var request = CreateValidPaymentRequest(cvv: "12");

            // Act
            var response = await client.PostAsJsonAsync("api/payments", request);
            var paymentResponse = await ReadPaymentResponseAsync(response);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(paymentResponse);
            Assert.Equal(PaymentStatus.Rejected, paymentResponse.Status);
            await bankGateway.DidNotReceive().SendPaymentRequestAsync(Arg.Any<PaymentRequest>());
        }

        [Fact]
        public async Task RejectsPaymentWhenExpiryDateIsInThePast()
        {
            // Arrange
            var bankGateway = Substitute.For<IBankGateway>();
            using var factory = CreateFactory(bankGateway);
            var client = factory.CreateClient();
            var lastMonth = DateTime.UtcNow.AddMonths(-1);
            var request = CreateValidPaymentRequest(expiryMonth: lastMonth.Month, expiryYear: lastMonth.Year);

            // Act
            var response = await client.PostAsJsonAsync("api/payments", request);
            var paymentResponse = await ReadPaymentResponseAsync(response);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(paymentResponse);
            Assert.Equal(PaymentStatus.Rejected, paymentResponse.Status);
            await bankGateway.DidNotReceive().SendPaymentRequestAsync(Arg.Any<PaymentRequest>());
        }

        [Fact]
        public async Task RejectsPaymentWhenCurrencyIsNotRecognised()
        {
            // Arrange
            var bankGateway = Substitute.For<IBankGateway>();
            using var factory = CreateFactory(bankGateway);
            var client = factory.CreateClient();
            var request = CreateValidPaymentRequest(currency: "XYZ");

            // Act
            var response = await client.PostAsJsonAsync("api/payments", request);
            var paymentResponse = await ReadPaymentResponseAsync(response);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(paymentResponse);
            Assert.Equal(PaymentStatus.Rejected, paymentResponse.Status);
            await bankGateway.DidNotReceive().SendPaymentRequestAsync(Arg.Any<PaymentRequest>());
        }

        [Fact]
        public async Task ReturnsServerErrorWhenBankIsUnavailable()
        {
            // Arrange
            var bankGateway = Substitute.For<IBankGateway>();
            bankGateway.SendPaymentRequestAsync(Arg.Any<PaymentRequest>())
                .Returns<BankPaymentResponse>(_ => throw new HttpRequestException(
                    "Service unavailable", null, HttpStatusCode.ServiceUnavailable));

            using var factory = CreateFactory(bankGateway);
            var client = factory.CreateClient();
            var request = CreateValidPaymentRequest(cardNumber: "4000000000000000");

            // Act
            var response = await client.PostAsJsonAsync("api/payments", request);

            // Assert
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        private static WebApplicationFactory<Program> CreateFactory(IBankGateway bankGateway)
        {
            var databaseName = Guid.NewGuid().ToString();

            return new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                    builder.ConfigureServices(services =>
                    {
                        services.RemoveAll<DbContextOptions<PaymentGatewayDbContext>>();
                        services.RemoveAll<IDbContextOptionsConfiguration<PaymentGatewayDbContext>>();
                        services.AddDbContext<PaymentGatewayDbContext>(options =>
                            options.UseInMemoryDatabase(databaseName));

                        services.RemoveAll<IBankGateway>();
                        services.AddScoped(_ => bankGateway);
                    }));
        }

        private static PaymentRequest CreateValidPaymentRequest(
            string cardNumber = "4000000000000001",
            string cvv = "123",
            string currency = "GBP",
            int? expiryMonth = null,
            int? expiryYear = null,
            int amount = 1050)
        {
            var expiry = DateTime.UtcNow.AddYears(1);

            return new PaymentRequest
            {
                CardNumber = cardNumber,
                ExpiryMonth = expiryMonth ?? expiry.Month,
                ExpiryYear = expiryYear ?? expiry.Year,
                Currency = currency,
                Amount = amount,
                Cvv = cvv
            };
        }

        private static async Task<PaymentResponse?> ReadPaymentResponseAsync(HttpResponseMessage response)
        {
            var envelope = await response.Content.ReadFromJsonAsync<ProcessPaymentResponseEnvelope>(JsonOptions);
            return envelope?.Message;
        }

        private sealed class ProcessPaymentResponseEnvelope
        {
            public PaymentResponse? Message { get; set; }
        }
    }
}

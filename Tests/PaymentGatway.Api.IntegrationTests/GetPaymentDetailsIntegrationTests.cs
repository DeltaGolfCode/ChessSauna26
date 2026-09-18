using System.Net;
using System.Net.Http.Json;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using PaymentGateway.Logic.DataAccess.DataModels;
using PaymentGateway.Logic.DataAccess.EntityMapping;
using PaymentGateway.Logic.Enums;
using PaymentGateway.Logic.Models.Response;

namespace PaymentGatway.Api.IntegrationTests
{
    public class GetPaymentDetailsIntegrationTests
    {
        private readonly Random _random = new();

        [Fact]
        public async Task RetrievesAPaymentSuccessfully()
        {
            // Arrange
            using var factory = CreateFactory();

            var payment = new PaymentResponse
            {
                Id = Guid.NewGuid(),
                Status = PaymentStatus.Authorized,
                ExpiryYear = _random.Next(2023, 2030),
                ExpiryMonth = _random.Next(1, 12),
                Amount = _random.Next(1, 10000),
                CardNumberLastFour = _random.Next(0, 10000).ToString("D4"),
                Currency = "GBP"
            };

            await SeedPaymentAsync(factory, payment);

            var client = factory.CreateClient();

            // Act
            var response = await client.GetAsync($"/api/payments/{payment.Id}", TestContext.Current.CancellationToken);
            var paymentResponse = await response.Content.ReadFromJsonAsync<PaymentResponse>(cancellationToken: TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(paymentResponse);

            Assert.Multiple(
                () => Assert.Equal(payment.Id, paymentResponse.Id),
                () => Assert.Equal(payment.Status, paymentResponse.Status),
                () => Assert.Equal(payment.CardNumberLastFour, paymentResponse.CardNumberLastFour),
                () => Assert.Equal(payment.ExpiryMonth, paymentResponse.ExpiryMonth),
                () => Assert.Equal(payment.ExpiryYear, paymentResponse.ExpiryYear),
                () => Assert.Equal(payment.Currency, paymentResponse.Currency),
                () => Assert.Equal(payment.Amount, paymentResponse.Amount));
        }

        [Fact]
        public async Task PreservesLeadingZerosInCardNumberLastFour()
        {
            // Arrange
            using var factory = CreateFactory();

            var payment = new PaymentResponse
            {
                Id = Guid.NewGuid(),
                Status = PaymentStatus.Authorized,
                ExpiryYear = 2030,
                ExpiryMonth = 12,
                Amount = 100,
                CardNumberLastFour = "0012",
                Currency = "GBP"
            };

            await SeedPaymentAsync(factory, payment);

            var client = factory.CreateClient();

            // Act
            var response = await client.GetAsync($"/api/payments/{payment.Id}", TestContext.Current.CancellationToken);
            var paymentResponse = await response.Content.ReadFromJsonAsync<PaymentResponse>(cancellationToken: TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(paymentResponse);
            Assert.Equal("0012", paymentResponse.CardNumberLastFour);
        }

        [Fact]
        public async Task Returns404IfPaymentNotFound()
        {
            // Arrange
            using var factory = CreateFactory();
            var client = factory.CreateClient();

            // Act
            var response = await client.GetAsync($"/api/payments/{Guid.NewGuid()}", TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        private static WebApplicationFactory<Program> CreateFactory()
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
                    }));
        }

        private async Task SeedPaymentAsync(WebApplicationFactory<Program> factory, PaymentResponse payment)
        {
            using var scope = factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<PaymentGatewayDbContext>();

            dbContext.PaymentHistories.Add(new PaymentHistory
            {
                Id = payment.Id,
                Status = payment.Status,
                Payment = payment
            });

            await dbContext.SaveChangesAsync();
        }
    }
}
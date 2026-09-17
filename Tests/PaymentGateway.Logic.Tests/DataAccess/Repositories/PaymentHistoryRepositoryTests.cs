using System.Diagnostics.CodeAnalysis;

using Microsoft.EntityFrameworkCore;

using PaymentGateway.Logic.DataAccess.DataModels;
using PaymentGateway.Logic.DataAccess.EntityMapping;
using PaymentGateway.Logic.DataAccess.Repositories;
using PaymentGateway.Logic.Enums;
using PaymentGateway.Logic.Models.Response;

namespace PaymentGateway.Logic.Tests.DataAccess.Repositories;

[ExcludeFromCodeCoverage]
public class PaymentHistoryRepositoryTests
{
    private static PaymentGatewayDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<PaymentGatewayDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new PaymentGatewayDbContext(options);
    }

    private static PaymentHistory CreatePaymentHistory(Guid id, PaymentStatus status = PaymentStatus.Authorized)
    {
        return new PaymentHistory
        {
            Id = id,
            Status = status,
            Payment = new PaymentResponse
            {
                Id = id,
                Status = status,
                CardNumberLastFour = 9012,
                ExpiryMonth = 12,
                ExpiryYear = DateTime.UtcNow.Year + 1,
                Currency = "GBP",
                Amount = 100
            }
        };
    }

    [Fact]
    public async Task CreateAsync_NewPaymentHistory_PersistsToDatabase()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var repository = new PaymentHistoryRepository(dbContext);
        var paymentHistory = CreatePaymentHistory(Guid.NewGuid());

        // Act
        await repository.CreateAsync(paymentHistory);

        // Assert
        var persisted = await dbContext.PaymentHistories.AsNoTracking().SingleAsync(x => x.Id == paymentHistory.Id);
        Assert.Equal(paymentHistory.Status, persisted.Status);
    }

    [Fact]
    public async Task CreateAsync_NewPaymentHistory_ReturnsSameInstance()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var repository = new PaymentHistoryRepository(dbContext);
        var paymentHistory = CreatePaymentHistory(Guid.NewGuid());

        // Act
        var result = await repository.CreateAsync(paymentHistory);

        // Assert
        Assert.Same(paymentHistory, result);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsMatchingPaymentHistory()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var repository = new PaymentHistoryRepository(dbContext);
        var paymentHistory = CreatePaymentHistory(Guid.NewGuid(), PaymentStatus.Declined);
        await repository.CreateAsync(paymentHistory);

        // Act
        var result = await repository.GetByIdAsync(paymentHistory.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(paymentHistory.Id, result.Id);
        Assert.Equal(PaymentStatus.Declined, result.Status);
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var repository = new PaymentHistoryRepository(dbContext);

        // Act
        var result = await repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }
}

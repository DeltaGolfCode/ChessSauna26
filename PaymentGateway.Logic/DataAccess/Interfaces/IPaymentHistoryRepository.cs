using PaymentGateway.Logic.DataAccess.DataModels;

namespace PaymentGateway.Logic.DataAccess.Interfaces;

public interface IPaymentHistoryRepository
{
    Task<PaymentHistory> CreateAsync(PaymentHistory paymentHistory, CancellationToken cancellationToken = default);

    Task<PaymentHistory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
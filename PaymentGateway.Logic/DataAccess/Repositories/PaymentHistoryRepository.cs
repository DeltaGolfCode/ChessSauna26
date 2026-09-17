using Microsoft.EntityFrameworkCore;

using PaymentGateway.Logic.DataAccess.DataModels;
using PaymentGateway.Logic.DataAccess.EntityMapping;
using PaymentGateway.Logic.DataAccess.Interfaces;

namespace PaymentGateway.Logic.DataAccess.Repositories;

public class PaymentHistoryRepository(PaymentGatewayDbContext _dbContext) : IPaymentHistoryRepository
{
    public async Task<PaymentHistory> CreateAsync(PaymentHistory paymentHistory, CancellationToken cancellationToken = default)
    {
        _dbContext.PaymentHistories.Add(paymentHistory);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return paymentHistory;
    }

    public async Task<PaymentHistory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.PaymentHistories
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }
}

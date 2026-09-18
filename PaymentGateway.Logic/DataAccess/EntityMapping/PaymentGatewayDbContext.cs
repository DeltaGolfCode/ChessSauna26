using Microsoft.EntityFrameworkCore;

using PaymentGateway.Logic.DataAccess.DataModels;

namespace PaymentGateway.Logic.DataAccess.EntityMapping;

public class PaymentGatewayDbContext(DbContextOptions<PaymentGatewayDbContext> options) : DbContext(options)
{
    public DbSet<PaymentHistory> PaymentHistories => Set<PaymentHistory>();

    public DbSet<PaymentStatusLookup> PaymentStatuses => Set<PaymentStatusLookup>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PaymentGatewayDbContext).Assembly);
    }
}
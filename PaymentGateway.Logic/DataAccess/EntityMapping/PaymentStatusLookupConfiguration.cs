using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using PaymentGateway.Logic.DataAccess.DataModels;
using PaymentGateway.Logic.Enums;

namespace PaymentGateway.Logic.DataAccess.EntityMapping;

public class PaymentStatusLookupConfiguration : IEntityTypeConfiguration<PaymentStatusLookup>
{
    public void Configure(EntityTypeBuilder<PaymentStatusLookup> builder)
    {
        builder.ToTable("PaymentStatuses");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion<int>()
            .ValueGeneratedNever();

        builder.Property(x => x.Name)
            .HasMaxLength(20)
            .IsRequired();

        builder.HasData(Enum.GetValues<PaymentStatus>()
            .Select(status => new PaymentStatusLookup
            {
                Id = status,
                Name = status.ToString()
            }));
    }
}
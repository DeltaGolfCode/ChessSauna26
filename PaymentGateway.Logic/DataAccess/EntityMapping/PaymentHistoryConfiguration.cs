using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using PaymentGateway.Logic.DataAccess.DataModels;
using PaymentGateway.Logic.Models.Response;

namespace PaymentGateway.Logic.DataAccess.EntityMapping;

public class PaymentHistoryConfiguration : IEntityTypeConfiguration<PaymentHistory>
{
    public void Configure(EntityTypeBuilder<PaymentHistory> builder)
    {
        builder.ToTable("PaymentHistories");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Payment)
            .HasConversion(
                payment => JsonSerializer.Serialize(payment, (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<PaymentResponse>(json, (JsonSerializerOptions?)null)!)
            .HasColumnName("Payment")
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.HasOne<PaymentStatusLookup>()
            .WithMany()
            .HasForeignKey(x => x.Status)
            .HasConstraintName("FK_PaymentHistories_PaymentStatuses_Status")
            .OnDelete(DeleteBehavior.Restrict);
    }
}

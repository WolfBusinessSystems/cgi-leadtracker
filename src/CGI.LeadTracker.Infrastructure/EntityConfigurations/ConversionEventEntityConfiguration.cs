using CGI.LeadTracker.Domain.AggregatesModel.Lead;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CGI.LeadTracker.Infrastructure.EntityConfigurations;

public class ConversionEventEntityConfiguration : IEntityTypeConfiguration<ConversionEvent>
{
    public void Configure(EntityTypeBuilder<ConversionEvent> builder)
    {
        builder.ToTable("ConversionEvents");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.LeadId).IsRequired();

        builder.Property(e => e.Platform)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.EventName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Stage)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.ContractValue)
            .HasColumnType("decimal(18,2)");

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.ExternalEventId).HasMaxLength(500);

        builder.Property(e => e.ErrorMessage).HasMaxLength(2000);

        builder.Property(e => e.CreatedAt).IsRequired();

        // índice para as queries de deduplicação
        builder.HasIndex(e => new { e.LeadId, e.Platform, e.Stage, e.Status });
    }
}

using CGI.LeadTracker.Domain.AggregatesModel.Lead;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CGI.LeadTracker.Infrastructure.EntityConfigurations;

public class LeadEntityConfiguration : IEntityTypeConfiguration<Lead>
{
    public void Configure(EntityTypeBuilder<Lead> builder)
    {
        builder.ToTable("Leads");

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).ValueGeneratedNever();

        builder.Property(l => l.RdStationId)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(l => l.RdStationId).IsUnique();

        builder.OwnsOne(l => l.Identifier, id =>
        {
            id.Property(i => i.Type)
                .HasColumnName("IdentifierType")
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            id.Property(i => i.Value)
                .HasColumnName("IdentifierValue")
                .IsRequired()
                .HasMaxLength(500);

            id.HasIndex(i => new { i.Type, i.Value }).IsUnique();
        });

        builder.OwnsOne(l => l.PersonalData, pd =>
        {
            pd.Property(p => p.Name)
                .HasColumnName("Name")
                .IsRequired()
                .HasMaxLength(200);

            pd.Property(p => p.Email)
                .HasColumnName("Email")
                .IsRequired()
                .HasMaxLength(200);

            pd.Property(p => p.Phone)
                .HasColumnName("Phone")
                .HasMaxLength(30);

            pd.Property(p => p.Cpf)
                .HasColumnName("Cpf")
                .HasMaxLength(20);
        });

        builder.Property(l => l.ContractValue)
            .HasColumnType("decimal(18,2)");

        builder.Property(l => l.CurrentStage)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();
        builder.Property(l => l.CreatedAt).IsRequired();
        builder.Property(l => l.UpdatedAt).IsRequired();

        builder.HasMany(l => l.ConversionEvents)
            .WithOne()
            .HasForeignKey(e => e.LeadId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(l => l.ConversionEvents)
            .HasField("_conversionEvents")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(l => l.DomainEvents);
    }
}

using IoTSensorMonitoring.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IoTSensorMonitoring.Infrastructure.Persistence.Configurations;

public class IntegrationClientConfiguration : IEntityTypeConfiguration<IntegrationClient>
{
    public void Configure(EntityTypeBuilder<IntegrationClient> builder)
    {
        builder.ToTable("integration_clients");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CompanyId).HasColumnName("company_id").IsRequired();
        builder.Property(x => x.ClientId).HasColumnName("client_id").HasMaxLength(100).IsRequired();
        builder.Property(x => x.ClientSecretHash).HasColumnName("client_secret_hash").HasMaxLength(200).IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasIndex(x => x.ClientId).IsUnique();
        builder.HasIndex(x => x.CompanyId);

        builder.HasOne(x => x.Company)
            .WithMany(company => company.IntegrationClients)
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

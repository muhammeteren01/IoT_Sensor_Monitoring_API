using IoTSensorMonitoring.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IoTSensorMonitoring.Infrastructure.Persistence.Configurations;

public class AlertRuleConfiguration : IEntityTypeConfiguration<AlertRule>
{
    public void Configure(EntityTypeBuilder<AlertRule> builder)
    {
        builder.ToTable("alert_rules");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.SensorId).HasColumnName("sensor_id").IsRequired();
        builder.Property(x => x.Metric).HasColumnName("metric").HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.Operator).HasColumnName("comparison_operator").HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.Threshold).HasColumnName("threshold").HasPrecision(10, 2).IsRequired();
        builder.Property(x => x.Severity).HasColumnName("severity").HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);

        builder.HasIndex(x => x.SensorId);

        builder.HasOne(x => x.Sensor)
            .WithMany(x => x.AlertRules)
            .HasForeignKey(x => x.SensorId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class AlertHistoryConfiguration : IEntityTypeConfiguration<AlertHistory>
{
    public void Configure(EntityTypeBuilder<AlertHistory> builder)
    {
        builder.ToTable("alert_history");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.AlertRuleId).HasColumnName("alert_rule_id").IsRequired();
        builder.Property(x => x.SensorId).HasColumnName("sensor_id").IsRequired();
        builder.Property(x => x.TriggeredValue).HasColumnName("triggered_value").HasPrecision(10, 2).IsRequired();
        builder.Property(x => x.Message).HasColumnName("message").HasMaxLength(500).IsRequired();
        builder.Property(x => x.TriggeredAt).HasColumnName("triggered_at").IsRequired();
        builder.Property(x => x.IsResolved).HasColumnName("is_resolved").HasDefaultValue(false);
        builder.Property(x => x.ResolvedAt).HasColumnName("resolved_at");
        builder.Property(x => x.ResolvedByUserId).HasColumnName("resolved_by_user_id");

        builder.HasIndex(x => new { x.SensorId, x.TriggeredAt });
        builder.HasIndex(x => x.AlertRuleId);
        builder.HasIndex(x => x.IsResolved);

        builder.HasOne(x => x.AlertRule)
            .WithMany(x => x.AlertHistories)
            .HasForeignKey(x => x.AlertRuleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Sensor)
            .WithMany(x => x.AlertHistories)
            .HasForeignKey(x => x.SensorId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class MaintenanceLogConfiguration : IEntityTypeConfiguration<MaintenanceLog>
{
    public void Configure(EntityTypeBuilder<MaintenanceLog> builder)
    {
        builder.ToTable("maintenance_logs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.SensorId).HasColumnName("sensor_id").IsRequired();
        builder.Property(x => x.ActionType).HasColumnName("action_type").HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.Description).HasColumnName("description");
        builder.Property(x => x.PerformedAt).HasColumnName("performed_at").IsRequired();
        builder.Property(x => x.NextDueDate).HasColumnName("next_due_date");

        builder.HasIndex(x => x.SensorId);
        builder.HasIndex(x => x.NextDueDate);

        builder.HasOne(x => x.Sensor)
            .WithMany(x => x.MaintenanceLogs)
            .HasForeignKey(x => x.SensorId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

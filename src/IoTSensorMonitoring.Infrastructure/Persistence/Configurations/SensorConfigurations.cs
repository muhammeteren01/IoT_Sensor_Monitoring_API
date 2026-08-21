using IoTSensorMonitoring.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IoTSensorMonitoring.Infrastructure.Persistence.Configurations;

public class DeviceModelConfiguration : IEntityTypeConfiguration<DeviceModel>
{
    public void Configure(EntityTypeBuilder<DeviceModel> builder)
    {
        builder.ToTable("device_models");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CompanyId).HasColumnName("company_id").IsRequired();
        builder.Property(x => x.Manufacturer).HasColumnName("manufacturer").HasMaxLength(200).IsRequired();
        builder.Property(x => x.ModelNumber).HasColumnName("model_number").HasMaxLength(100).IsRequired();
        builder.Property(x => x.SupportedMetrics).HasColumnName("supported_metrics").HasMaxLength(500).IsRequired();
        builder.Property(x => x.CalibrationPeriodDays).HasColumnName("calibration_period_days");

        builder.HasIndex(x => x.CompanyId)
            .HasDatabaseName("IX_device_models_company_id");
        builder.HasIndex(x => new { x.CompanyId, x.Manufacturer, x.ModelNumber })
            .IsUnique()
            .HasDatabaseName("IX_device_models_company_id_manufacturer_model_number");

        builder.HasOne(x => x.Company)
            .WithMany(x => x.DeviceModels)
            .HasForeignKey(x => x.CompanyId)
            .HasConstraintName("FK_device_models_companies_company_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Sensors)
            .WithOne(x => x.DeviceModel)
            .HasForeignKey(x => x.DeviceModelId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class SensorConfiguration : IEntityTypeConfiguration<Sensor>
{
    public void Configure(EntityTypeBuilder<Sensor> builder)
    {
        builder.ToTable("sensors");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.ZoneId).HasColumnName("zone_id").IsRequired();
        builder.Property(x => x.DeviceModelId).HasColumnName("device_model_id").IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(x => x.MacAddress).HasColumnName("mac_address").HasMaxLength(64).IsRequired();
        builder.Property(x => x.FirmwareVersion).HasColumnName("firmware_version").HasMaxLength(50);
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.LastCalibrationDate).HasColumnName("last_calibration_date");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasIndex(x => x.MacAddress).IsUnique();
        builder.HasIndex(x => x.ZoneId);
        builder.HasIndex(x => x.DeviceModelId);
        builder.HasIndex(x => x.Status);
    }
}

public class SensorMeasurementConfiguration : IEntityTypeConfiguration<SensorMeasurement>
{
    public void Configure(EntityTypeBuilder<SensorMeasurement> builder)
    {
        builder.ToTable("sensor_measurements");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.SensorId).HasColumnName("sensor_id").IsRequired();
        builder.Property(x => x.Temperature).HasColumnName("temperature").HasPrecision(8, 2);
        builder.Property(x => x.Humidity).HasColumnName("humidity").HasPrecision(8, 2);
        builder.Property(x => x.Pressure).HasColumnName("pressure").HasPrecision(10, 2);
        builder.Property(x => x.BatteryLevel).HasColumnName("battery_level").HasPrecision(5, 2);
        builder.Property(x => x.SignalStrength).HasColumnName("signal_strength");
        builder.Property(x => x.MeasurementDate).HasColumnName("measurement_date").IsRequired();

        builder.HasIndex(x => new { x.SensorId, x.MeasurementDate }).IsUnique();

        builder.HasOne(x => x.Sensor)
            .WithMany(x => x.Measurements)
            .HasForeignKey(x => x.SensorId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

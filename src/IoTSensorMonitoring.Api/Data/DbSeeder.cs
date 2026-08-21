using IoTSensorMonitoring.Application.Interfaces.Services;
using IoTSensorMonitoring.Application.Settings;
using IoTSensorMonitoring.Domain.Entities;
using IoTSensorMonitoring.Domain.Enums;
using IoTSensorMonitoring.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace IoTSensorMonitoring.Api.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var settings = scope.ServiceProvider.GetRequiredService<IOptions<SeedSettings>>().Value;

        if (!settings.Enabled)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(settings.Email) || string.IsNullOrWhiteSpace(settings.Password))
        {
            throw new InvalidOperationException(
                "SeedSettings.Enabled=true requires Email and Password.");
        }

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordService = scope.ServiceProvider.GetRequiredService<IPasswordService>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");
        var passwordHash = passwordService.HashPassword(settings.Password);

        await SeedSuperAdminAsync(db, settings, passwordHash, logger, cancellationToken);
    }

    public static Task SeedSuperAdminAsync(
        IServiceProvider services,
        CancellationToken cancellationToken = default) =>
        SeedAsync(services, cancellationToken);

    private static async Task SeedSuperAdminAsync(
        AppDbContext db,
        SeedSettings settings,
        string passwordHash,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var exists = await db.Users
            .IgnoreQueryFilters()
            .AnyAsync(user => user.Email == settings.Email, cancellationToken);

        if (exists)
        {
            logger.LogInformation("SuperAdmin seed skipped; email already exists: {Email}", settings.Email);
            return;
        }

        db.Users.Add(new User
        {
            CompanyId = null,
            FirstName = settings.FirstName,
            LastName = settings.LastName,
            Email = settings.Email.Trim(),
            PasswordHash = passwordHash,
            Role = UserRole.SuperAdmin,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("SuperAdmin seed completed: {Email}", settings.Email);
    }
}

using IoTSensorMonitoring.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IoTSensorMonitoring.Api.Data;

public static class DatabaseInitializer
{
    public static async Task MigrateAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync(cancellationToken);
    }
}

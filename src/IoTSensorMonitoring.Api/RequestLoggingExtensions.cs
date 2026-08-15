using Serilog;
using Serilog.Events;

namespace IoTSensorMonitoring.Api;

public static class RequestLoggingExtensions
{
    private static readonly string[] StaticExtensions =
        [".js", ".css", ".map", ".png", ".ico", ".svg", ".woff", ".woff2"];

    public static IApplicationBuilder UseApiRequestLogging(this IApplicationBuilder app)
    {
        return app.UseSerilogRequestLogging(options =>
        {
            options.GetLevel = (httpContext, _, exception) =>
            {
                var path = httpContext.Request.Path.Value ?? string.Empty;
                if (IsNoise(path))
                {
                    return LogEventLevel.Verbose;
                }

                if (exception is not null || httpContext.Response.StatusCode >= 500)
                {
                    return LogEventLevel.Error;
                }

                if (httpContext.Response.StatusCode >= 400)
                {
                    return LogEventLevel.Warning;
                }

                return LogEventLevel.Information;
            };

            options.MessageTemplate =
                "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
        });
    }

    private static bool IsNoise(string path)
    {
        if (path is "/" or "/index.html" or "/index.js" or "/index.css" or "/favicon.ico")
        {
            return true;
        }

        if (path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return StaticExtensions.Any(extension => path.EndsWith(extension, StringComparison.OrdinalIgnoreCase));
    }
}

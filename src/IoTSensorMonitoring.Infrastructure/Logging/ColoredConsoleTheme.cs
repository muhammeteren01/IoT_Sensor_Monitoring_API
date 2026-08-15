using Serilog.Sinks.SystemConsole.Themes;

namespace IoTSensorMonitoring.Infrastructure.Logging;

public static class ColoredConsoleTheme
{
    public static AnsiConsoleTheme Instance { get; } = new(new Dictionary<ConsoleThemeStyle, string>
    {
        [ConsoleThemeStyle.Text] = "\x1b[38;5;252m",
        [ConsoleThemeStyle.SecondaryText] = "\x1b[38;5;246m",
        [ConsoleThemeStyle.TertiaryText] = "\x1b[38;5;242m",
        [ConsoleThemeStyle.Invalid] = "\x1b[33;1m",
        [ConsoleThemeStyle.Null] = "\x1b[38;5;38m",
        [ConsoleThemeStyle.Name] = "\x1b[38;5;81m",
        [ConsoleThemeStyle.String] = "\x1b[38;5;150m",
        [ConsoleThemeStyle.Number] = "\x1b[38;5;151m",
        [ConsoleThemeStyle.Boolean] = "\x1b[38;5;38m",
        [ConsoleThemeStyle.Scalar] = "\x1b[38;5;79m",
        [ConsoleThemeStyle.LevelVerbose] = "\x1b[37m",
        [ConsoleThemeStyle.LevelDebug] = "\x1b[37m",
        [ConsoleThemeStyle.LevelInformation] = "\x1b[32;1m",
        [ConsoleThemeStyle.LevelWarning] = "\x1b[33;1m",
        [ConsoleThemeStyle.LevelError] = "\x1b[31;1m",
        [ConsoleThemeStyle.LevelFatal] = "\x1b[31;1m"
    });
}


using System;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace MovieFinder.Services;

public class AppLogger : IAppLogger, IDisposable
{
    public event Action<string>? OnLogMessage;
    private Action<string>? _logAction;
    private readonly IConfiguration _configuration;
    private readonly StreamWriter? _logWriter;
    private readonly LogLevel _minLogLevel;

    public AppLogger(IConfiguration configuration)
    {
        _configuration = configuration;
        var logFile = _configuration["LOGFILE_LOCATION"];

        if (!string.IsNullOrEmpty(logFile))
        {
            var logDirectory = Path.GetDirectoryName(logFile);
            if (!string.IsNullOrEmpty(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }
            _logWriter = new StreamWriter(logFile, append: true) { AutoFlush = true };
        }

        var logLevelStr = _configuration["Logging:LogLevel:Default"] ?? "Information";
        Enum.TryParse(logLevelStr, out _minLogLevel);
    }

    public void Initialize(Action<string> logAction)
    {
        _logAction = logAction;
    }

    public void Information(string message)
    {
        Log(LogLevel.Information, message);
    }

    public void Warn(string message)
    {
        Log(LogLevel.Warning, message);
    }

    public void Debug(string message)
    {
        Log(LogLevel.Debug, message);
    }

    public void Error(string message)
    {
        Log(LogLevel.Error, message);
    }

    public void Event(string message)
    {
        Log(LogLevel.Event, message);
    }

    public void Trace(string message)
    {
        Log(LogLevel.Trace, message);
    }

    public void Log(string message)
    {
        Information(message);
    }

    private void Log(LogLevel level, string message)
    {
        if (level < _minLogLevel && level != LogLevel.Event)
        {
            return;
        }

        var logMessage = $"[{level.ToString().ToUpper()}] {DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}";
        OnLogMessage?.Invoke(logMessage);
        _logAction?.Invoke(logMessage);
        _logWriter?.WriteLine(logMessage);
    }

    public void Dispose()
    {
        _logWriter?.Dispose();
    }
}

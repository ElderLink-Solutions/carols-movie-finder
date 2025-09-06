using System;

namespace MovieFinder.Services;

public enum LogLevel
{
    Trace,
    Debug,
    Information,
    Warning,
    Error,
    Event
}

public interface IAppLogger
{
    event Action<string> OnLogMessage;
    void Information(string message);
    void Warn(string message);
    void Debug(string message);
    void Error(string message);
    void Trace(string message);
    void Event(string message);
    void Log(string message);
}
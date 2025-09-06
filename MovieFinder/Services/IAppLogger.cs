
namespace MovieFinder.Services;

public interface IAppLogger
{
    void Information(string message);
    void Warn(string message);
    void Debug(string message);
    void Error(string message);
    void Trace(string message);
    void Log(string message);
}

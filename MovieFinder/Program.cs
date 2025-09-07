using Avalonia;
using System;
using System.Threading; // Added for Mutex

namespace MovieFinder;

class Program
{
    private static Mutex? _mutex = null; // Declare a static Mutex

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        const string appName = "MovieFinderSingleInstance"; // Unique name for the mutex
        bool createdNew;
        _mutex = new Mutex(true, appName, out createdNew); // Create or open the mutex

        if (!createdNew)
        {
            // Another instance is already running
            Console.WriteLine("Another instance of Carol's Movie Finder is already running. Exiting.");
            return; // Exit the application
        }

        try
        {
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unhandled exception in Main: {ex}");
            throw;
        }
        finally
        {
            Console.WriteLine("Main method finished. Releasing mutex.");
            _mutex?.ReleaseMutex();
            _mutex?.Dispose();
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}

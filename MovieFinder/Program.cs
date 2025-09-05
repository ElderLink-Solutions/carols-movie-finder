using Avalonia;
using System;

namespace MovieFinder;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        Console.WriteLine("=== MovieFinder Program.cs: Main starting ===");
        try
        {
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
            Console.WriteLine("=== MovieFinder Program.cs: Main completed successfully ===");
        }
        catch (Exception ex)
        {
            Console.WriteLine("=== MovieFinder Program.cs: Exception occurred ===");
            Console.WriteLine(ex.ToString());
            throw;
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}

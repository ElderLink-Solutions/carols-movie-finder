using CommunityToolkit.Mvvm.ComponentModel;

namespace MovieFinder.Models;

public partial class LogEntry : ObservableObject
{
    public string Message { get; set; }

    [ObservableProperty]
    private bool _isCopiedVisible;

    public LogEntry(string message)
    {
        Message = message;
    }
}

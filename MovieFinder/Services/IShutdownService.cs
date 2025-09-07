
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MovieFinder.Services
{
    public interface IShutdownService
    {
        CancellationToken ShutdownToken { get; }
        void RequestShutdown();
        void RegisterTask(Task task, string name);
        Task WaitForShutdownAsync(TimeSpan timeout);
    }
}


using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MovieFinder.Services
{
    public class ShutdownService : IShutdownService
    {
        private readonly IAppLogger _logger;
        private readonly CancellationTokenSource _shutdownCts = new CancellationTokenSource();
        private readonly Dictionary<string, Task> _tasks = new Dictionary<string, Task>();
        private readonly object _lock = new object();

        public ShutdownService(IAppLogger logger)
        {
            _logger = logger;
        }

        public CancellationToken ShutdownToken => _shutdownCts.Token;

        public void RequestShutdown()
        {
            _logger.Log("Shutdown requested.");
            _shutdownCts.Cancel();
        }

        public void RegisterTask(Task task, string name)
        {
            lock (_lock)
            {
                _logger.Debug($"Registering task '{name}' (Id: {task.Id}, Status: {task.Status}).");
                _tasks[name] = task;
                task.ContinueWith(t =>
                {
                    lock (_lock)
                    {
                        _tasks.Remove(name);
                        _logger.Debug($"Task '{name}' (Id: {t.Id}) completed with status {t.Status}. Remaining tasks: {_tasks.Count}");
                    }
                }, TaskContinuationOptions.ExecuteSynchronously);
            }
        }

        public async Task WaitForShutdownAsync(TimeSpan timeout)
        {
            _logger.Debug("Waiting for registered tasks to complete...");
            // Create a copy of tasks to avoid collection modified exception
            List<Task> tasksToWait;
            lock (_lock)
            {
                tasksToWait = new List<Task>(_tasks.Values);
            }

            if (tasksToWait.Count == 0)
            {
                _logger.Warn("No tasks registered to wait for.");
                return;
            }

            var allTasks = Task.WhenAll(tasksToWait);
            var completedTask = await Task.WhenAny(allTasks, Task.Delay(timeout));

            if (completedTask == allTasks)
            {
                _logger.Information("All tasks completed.");
            }
            else
            {
                _logger.Warn("Timeout waiting for tasks to complete.");
                lock (_lock)
                {
                    foreach (var entry in _tasks) // Iterate over _tasks directly to see what's left
                    {
                        if (!entry.Value.IsCompleted)
                        {
                            _logger.Information($"Task '{entry.Key}' (Id: {entry.Value.Id}, Status: {entry.Value.Status}) is still running.");
                        }
                    }
                }
            }
        }
    }
}

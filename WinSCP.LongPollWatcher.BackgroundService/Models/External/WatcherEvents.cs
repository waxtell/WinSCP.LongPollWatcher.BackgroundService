using System;
using System.Threading;
using System.Threading.Tasks;

namespace WinSCP.LongPollWatcher.BackgroundService.Models.External
{
    public class WatcherEvents
    {
        public Func<string, SessionOptions, CancellationToken, Task> OnFileAdded { get; set; } = async (remoteFileName, sessionOptions, cancellationToken) => await Task.CompletedTask;
        public Func<string, CancellationToken, Task> OnFileDeleted { get; set; } = async (remoteFileName, cancellationToken) => await Task.CompletedTask;
        public Func<string, CancellationToken, Task> OnFileModified { get; set; } = async (remoteFileName, cancellationToken) => await Task.CompletedTask;
        public Func<Exception, CancellationToken, Task<bool>> OnException { get; set; } = async (exception, cancellationToken) => await Task.FromException<bool>(exception);
    }
}

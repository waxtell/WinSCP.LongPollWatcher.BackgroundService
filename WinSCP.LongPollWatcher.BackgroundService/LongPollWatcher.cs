using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WinSCP.LongPollWatcher.BackgroundService.Models.External;
using WinSCP.LongPollWatcher.BackgroundService.Models.Internal;

namespace WinSCP.LongPollWatcher.BackgroundService
{
    public class LongPollWatcher : Microsoft.Extensions.Hosting.BackgroundService
    {
        private readonly WatcherEvents _events;
        private readonly LongPollWatcherOptions _options;
        private readonly SessionOptions _sessionOptions;

        public LongPollWatcher(WatcherEvents events, LongPollWatcherOptions options, SessionOptions sessionOptions)
        {
            _events = events ?? new WatcherEvents();
            _options = options;
            _sessionOptions = sessionOptions;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            IList<TrackedFile> GetRemoteFiles(Session session)
            {
                return
                    session
                        .EnumerateRemoteFiles
                        (
                            _options.RemotePath,
                            _options.Mask,
                            _options.IncludeSubdirectories
                                ? EnumerationOptions.AllDirectories
                                : EnumerationOptions.None
                        )
                        .Select
                        (
                            val => new TrackedFile
                            (
                                val.FullName,
                                val.Length,
                                val.LastWriteTime
                            )
                        )
                        .ToList();
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var session = new Session())
                    {
                        if (!string.IsNullOrEmpty(_options.SessionExecutablePath))
                        {
                            session.ExecutablePath = _options.SessionExecutablePath;
                        }

                        IList<TrackedFile> previousFiles = null;

                        while (!stoppingToken.IsCancellationRequested)
                        {
                            if (!session.Opened)
                            {
                                session.Open(_sessionOptions);
                            }

                            var currentFiles = GetRemoteFiles(session);

                            if (previousFiles != null)
                            {
                                var modifiedFiles =
                                        from previousFile in previousFiles
                                        join currentFile in currentFiles on previousFile.FullName equals currentFile.FullName
                                        where
                                        (
                                            previousFile.LastModified != currentFile.LastModified ||
                                            previousFile.SizeInBytes != currentFile.SizeInBytes
                                        )
                                        select currentFile;

                                Task
                                    .WaitAll
                                    (
                                        modifiedFiles
                                            .Select
                                            (
                                                file => _events
                                                            .OnFileModified
                                                            (
                                                                file.FullName,
                                                                _sessionOptions,
                                                                stoppingToken
                                                            )
                                            )
                                            .ToArray()
                                    );

                                Task
                                    .WaitAll
                                    (
                                        currentFiles
                                            .Except(previousFiles)
                                            .Select
                                            (
                                                file => _events
                                                            .OnFileAdded
                                                            (
                                                                file.FullName,
                                                                _sessionOptions,
                                                                stoppingToken
                                                            )
                                            )
                                            .ToArray()
                                    );

                                Task
                                    .WaitAll
                                    (
                                        previousFiles
                                            .Except(currentFiles)
                                            .Select
                                            (
                                                file => _events
                                                            .OnFileDeleted
                                                            (
                                                                file.FullName,
                                                                stoppingToken
                                                            )
                                            )
                                            .ToArray()
                                    );
                            }

                            previousFiles = currentFiles;

                            await
                                Task.Delay(_options.SleepIntervalMilliseconds, stoppingToken);
                        }
                    }
                }
                catch (TaskCanceledException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    var shouldContinue = await
                                            _events
                                                .OnException(e, stoppingToken);

                    if (!shouldContinue)
                    {
                        await
                            StopAsync(stoppingToken);
                    }
                }
            }
        }
    }
}
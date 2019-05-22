using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WinSCP;
using WinSCP.LongPollWatcher.BackgroundService;
using WinSCP.LongPollWatcher.BackgroundService.Models.External;

// ReSharper disable StringLiteralTypo

namespace SampleApp9000
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            await
                CreateHostBuilder(args)
                    .RunConsoleAsync();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            new HostBuilder()
                .ConfigureAppConfiguration
                (
                    (hostContext, config) =>
                    {
                        config.SetBasePath(Directory.GetCurrentDirectory());
                        config.AddEnvironmentVariables();
                        config.AddJsonFile("appsettings.json", optional: true);
                        config.AddJsonFile($"appsettings.{hostContext.HostingEnvironment.EnvironmentName}.json",
                            optional: true);
                        config.AddCommandLine(args);
                    }
                )
                .ConfigureServices
                (
                    (context, collection) =>
                    {
                        collection
                            .AddSingleton
                            (
                                provider =>
                                {
                                    return new WatcherEvents
                                    {
                                        OnFileAdded = async (remoteFileName, sessionOptions, cancellationToken) =>
                                        {
                                            Console.WriteLine($"Added: {remoteFileName}");
                                            await Task.CompletedTask; 
                                        },
                                        OnFileDeleted = async (remoteFileName, cancellationToken) =>
                                        {
                                            Console.WriteLine($"Deleted: {remoteFileName}");
                                            await Task.CompletedTask;
                                        },
                                        OnFileModified = async (remoteFileName, sessionOptions, cancellationToken) =>
                                        {
                                            Console.WriteLine($"Modified: {remoteFileName}");
                                            await Task.CompletedTask;
                                        },
                                        OnException = async (exception, cancellationToken) =>
                                        {
                                            Console.WriteLine($"Exception: {exception.Message}");

                                            // Return false to quit, true to continue trying...
                                            Thread.Sleep(10000);
                                            return await Task.FromResult(true);
                                        }
                                    };
                                }
                            );
                        collection
                            .AddSingleton
                            (
                                provider => provider
                                                .GetService<IConfiguration>()
                                                .GetSection("SessionOptions")
                                                .Get<SessionOptions>()
                            );

                        collection
                            .AddSingleton
                            (
                                provider => provider
                                                .GetService<IConfiguration>()
                                                .GetSection("LongPollOptions")
                                                .Get<LongPollWatcherOptions>()
                            );

                        collection.AddHostedService<LongPollWatcher>();
                    }
                );
    }
}

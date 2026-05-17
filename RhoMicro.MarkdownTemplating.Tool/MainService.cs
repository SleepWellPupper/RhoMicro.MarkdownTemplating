// SPDX-License-Identifier: MPL-2.0

namespace RhoMicro.MarkdownTemplating;

using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

internal sealed partial class MainService(
    MarkdownRewriterService service,
    IOptions<MainServiceOptions> options,
    IHostApplicationLifetime lifetime,
    ILogger<MainService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var paths = Directory.EnumerateFiles(
                options.Value.WorkingDirectory,
                options.Value.SearchPattern,
                options.Value.Recurse
                    ? SearchOption.AllDirectories
                    : SearchOption.TopDirectoryOnly)
            .ToArray();

        LogFoundCountPathsToRewrite(logger, paths.Length, paths);

        if (options.Value.Watch)
        {
            await Watch(paths, ct);
        }
        else
        {
            await RunOnce(paths, ct);
        }

        lifetime.StopApplication();
    }

    private async Task Watch(String[] paths, CancellationToken ct)
    {
        LogWatchModeEnabled(logger);
        
        using var watcher = new FileSystemWatcher(
            path: options.Value.WorkingDirectory,
            filter: options.Value.SearchPattern);

        watcher.IncludeSubdirectories = options.Value.Recurse;
        watcher.EnableRaisingEvents = true;
        watcher.NotifyFilter = NotifyFilters.LastWrite;
        var channel = Channel.CreateUnbounded<String>(
            new UnboundedChannelOptions
            {
                SingleReader = true, 
                SingleWriter = false,
                AllowSynchronousContinuations = true
            });
        watcher.Changed += (_, e) => channel.Writer.TryWrite(e.FullPath);
        await RunOnce(paths, ct);
        while (await channel.Reader.WaitToReadAsync(ct))
        {
            while (channel.Reader.TryRead(out var path))
            {
                await service.Rewrite(path, ct);
            }
        }
    }

    private async Task RunOnce(String[] paths, CancellationToken stoppingToken)
        => await service.Rewrite(paths, stoppingToken);

    [LoggerMessage(LogLevel.Information, "Found {Count} paths to rewrite: {Paths}")]
    static partial void LogFoundCountPathsToRewrite(ILogger<MainService> logger, Int32 count, String[] paths);

    [LoggerMessage(LogLevel.Information, "Watch mode enabled.")]
    static partial void LogWatchModeEnabled(ILogger<MainService> logger);
}

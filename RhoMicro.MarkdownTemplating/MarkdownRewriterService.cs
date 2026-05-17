// SPDX-License-Identifier: MPL-2.0

namespace RhoMicro.MarkdownTemplating;

using Microsoft.Extensions.Logging;

/// <summary>
/// Provides a façade for rewriting Markdown files from a collection of paths. 
/// </summary>
/// <param name="context">
/// The context using which to rewrite files.
/// </param>
/// <param name="logger">
/// The logger to use when logging rewriting events.
/// </param>
/// <param name="loggers">
/// The logger factory to use when creating loggers for new rewriter instances.
/// </param>
public sealed partial class MarkdownRewriterService(
    MarkdownRewriterContext context,
    ILogger<MarkdownRewriterService> logger,
    ILoggerFactory loggers)
{
    /// <summary>
    /// Rewrites all Markdown files at the paths provided.
    /// </summary>
    /// <param name="paths">
    /// The paths identifying the files to rewrite.
    /// </param>
    /// <param name="ct">
    /// A cancellation token used to request rewriting to be canceled.
    /// </param>
    /// <returns>
    /// A task representing the rewriting operation.
    /// </returns>
    public async ValueTask Rewrite(IEnumerable<String> paths, CancellationToken ct = default)
        => await Parallel.ForEachAsync(paths, ct, Rewrite);

    /// <summary>
    /// Rewrites the Markdown files at the path provided.
    /// </summary>
    /// <param name="path">
    /// The path identifying the file to rewrite.
    /// </param>
    /// <param name="ct">
    /// A cancellation token used to request rewriting to be canceled.
    /// </param>
    /// <returns>
    /// A task representing the rewriting operation.
    /// </returns>
    public async ValueTask Rewrite(String path, CancellationToken ct)
    {
        LogRewritingPath(logger, path);
        try
        {
            var rewriter = await MarkdownRewriter.Create(
                path,
                context,
                loggers.CreateLogger<MarkdownRewriter>(),
                ct);

            try
            {
                var rewritten = await rewriter.Rewrite(ct);
                if (rewritten != rewriter.MarkdownSource)
                {
                    await using var fs = File.CreateText(path);
                    await fs.WriteAsync(rewritten);
                }

                LogDoneRewritingPath(logger, path);
            }
            catch
            {
                await using var fs = File.CreateText(path);
                await fs.WriteAsync(rewriter.MarkdownSource);
                throw;
            }
        }
        catch (Exception ex)
        {
            LogErrorWhileRewritingPath(logger, ex, path);
        }
    }

    [LoggerMessage(LogLevel.Information, "Rewriting `{Path}`.")]
    static partial void LogRewritingPath(ILogger<MarkdownRewriterService> logger, String path);

    [LoggerMessage(LogLevel.Debug, "Done rewriting `{Path}`.")]
    static partial void LogDoneRewritingPath(ILogger<MarkdownRewriterService> logger, String path);

    [LoggerMessage(LogLevel.Error, "Error while rewriting `{Path}`.")]
    static partial void LogErrorWhileRewritingPath(ILogger<MarkdownRewriterService> logger, Exception ex, String path);
}

// SPDX-License-Identifier: MPL-2.0

namespace RhoMicro.MarkdownTemplating;

using Microsoft.Extensions.Logging;

/// <summary>
/// Provides file contents using the <see cref="File.ReadAllTextAsync(string,CancellationToken)"/> api.
/// </summary>
/// <param name="logger">
/// The logger to use when loggin file accesses.
/// </param>
public sealed partial class MarkdownRewriterSystemFileProvider(
    ILogger<MarkdownRewriterSystemFileProvider> logger)
    : IMarkdownRewriterFileProvider
{
    /// <inheritdoc />
    public async ValueTask<String> ReadAllText(String path, String baseDirectory, CancellationToken ct = default)
    {
        LogReadingFilePath(logger, path);

        var fullPath = baseDirectory is not []
            ? Path.GetFullPath(
                path: path,
                basePath: Path.GetFullPath(baseDirectory))
            : path;
        var result = await File.ReadAllTextAsync(fullPath, ct);

        return result;
    }

    [LoggerMessage(LogLevel.Debug, "Reading file `{Path}`.")]
    static partial void LogReadingFilePath(ILogger<MarkdownRewriterSystemFileProvider> logger, String path);
}

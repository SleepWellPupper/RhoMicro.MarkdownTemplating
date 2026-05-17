// SPDX-License-Identifier: MPL-2.0

namespace RhoMicro.MarkdownTemplating;

/// <summary>
/// Provides files contents from memory.
/// </summary>
/// <param name="files">
/// The files and their content to make available.
/// </param>
public sealed class MarkdownRewriterMemoryFileProvider(
    IReadOnlyDictionary<String, String> files)
    : IMarkdownRewriterFileProvider
{
    /// <inheritdoc />
    public async ValueTask<String> ReadAllText(
        String path,
        String baseDirectory,
        CancellationToken ct = default)
        => files[path];
}

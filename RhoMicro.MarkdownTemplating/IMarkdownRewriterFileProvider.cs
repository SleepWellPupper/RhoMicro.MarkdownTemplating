// SPDX-License-Identifier: MPL-2.0

namespace RhoMicro.MarkdownTemplating;

/// <summary>
/// Provides file contents to the <see cref="MarkdownRewriter"/>.
/// </summary>
public interface IMarkdownRewriterFileProvider
{
    /// <summary>
    /// Reads all the text from a file into memory.
    /// </summary>
    /// <param name="path">
    /// The path of the file whose contents to load.
    /// </param>
    /// <param name="baseDirectory">
    /// The directory relative to which to resolve the file path.
    /// </param>
    /// <param name="ct">
    /// A cancellation token used to request file loading to be canceled.
    /// </param>
    /// <returns>
    /// A task that, upon completion, contains the contents of the file.
    /// </returns>
    ValueTask<String> ReadAllText(String path, String baseDirectory, CancellationToken ct = default);
}

// SPDX-License-Identifier: MPL-2.0

namespace RhoMicro.MarkdownTemplating;

/// <summary>
/// Provides options for formatting syntax nodes using <see cref="NormalizedSyntaxNodeFormatter"/>.
/// </summary>
/// <param name="Indentation">
/// The indentation to use when formatting.
/// </param>
/// <param name="Newline">
/// The newline to use when formatting.
/// </param>
public sealed record NormalizedSyntaxNodeFormatterOptions(
    String Indentation,
    String Newline)
{
    /// <summary>
    /// Gets the default instance.
    /// </summary>
    public static NormalizedSyntaxNodeFormatterOptions Default { get; } = new(
        Indentation: "    ",
        Newline: Environment.NewLine);
}

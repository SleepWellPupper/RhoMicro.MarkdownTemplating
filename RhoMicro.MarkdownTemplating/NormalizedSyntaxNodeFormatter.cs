// SPDX-License-Identifier: MPL-2.0

namespace RhoMicro.MarkdownTemplating;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

/// <summary>
/// Formats syntax nodes using whitespace normalization.
/// </summary>
/// <param name="options">
/// The options used by the formatter.
/// </param>
public sealed class NormalizedSyntaxNodeFormatter(
    NormalizedSyntaxNodeFormatterOptions options)
    : ISyntaxNodeFormatter
{
    /// <summary>
    /// Gets an instance using the <see cref="NormalizedSyntaxNodeFormatterOptions.Default"/> options.
    /// </summary>
    public static NormalizedSyntaxNodeFormatter Default { get; } = new(NormalizedSyntaxNodeFormatterOptions.Default);

    /// <inheritdoc />
    public async ValueTask Format(TextWriter target, CSharpSyntaxNode node, CancellationToken ct)
        => node.NormalizeWhitespace(
                indentation: options.Indentation,
                eol: options.Newline)
            .WriteTo(target);
}

// SPDX-License-Identifier: MPL-2.0

namespace RhoMicro.MarkdownTemplating;

using Microsoft.CodeAnalysis.CSharp;

/// <summary>
/// Formats syntax nodes when rewriting Markdown source texts.
/// </summary>
public interface ISyntaxNodeFormatter
{
    /// <summary>
    /// Formats a syntax node.
    /// </summary>
    /// <param name="target">
    /// The writer to write the formatted node text to.
    /// </param>
    /// <param name="node">
    /// The node to format.
    /// </param>
    /// <param name="ct">
    /// A cancellation token used to request formatting to be canceled.
    /// </param>
    /// <returns>
    /// A task representing the formatting operation.
    /// </returns>
    ValueTask Format(TextWriter target, CSharpSyntaxNode node, CancellationToken ct);
}

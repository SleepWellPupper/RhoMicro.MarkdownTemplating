// SPDX-License-Identifier: MPL-2.0

namespace RhoMicro.MarkdownTemplating;

using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;

/// <summary>
/// Rewrites Markdown files, replacing template code regions with source texts.
/// </summary>
public sealed partial class MarkdownRewriter
{
    private MarkdownRewriter(String markdownSource, MarkdownRewriterContext context, ILogger<MarkdownRewriter> logger)
    {
        _markdownSource = markdownSource;
        _context = context;
        _logger = logger;
    }

    private readonly ILogger<MarkdownRewriter> _logger;
    private readonly MarkdownRewriterContext _context;
    private readonly String _markdownSource;
    private readonly List<(CodeRegion region, CompilationUnitSyntax replacementNodeRoot)> _replacements = [];

    /// <summary>
    /// Gets the original Markdown source text being rewritten.
    /// </summary>
    public String MarkdownSource => _markdownSource;

    /// <summary>
    /// Creates a new rewriter.
    /// </summary>
    /// <param name="path">
    /// The path of the Markdown file to rewrite.
    /// </param>
    /// <param name="context">
    /// The rewriter context to use when initializing the rewriter.
    /// </param>
    /// <param name="logger">
    /// The logger to use when logging rewriting events.
    /// </param>
    /// <param name="ct">
    /// A cancellation token used to request rewriter initialization to be canceled.
    /// </param>
    /// <returns>
    /// A new Markdown rewriter for the specified file.
    /// </returns>
    public static async ValueTask<MarkdownRewriter> Create(
        String path,
        MarkdownRewriterContext context,
        ILogger<MarkdownRewriter> logger,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        ct.ThrowIfCancellationRequested();

        LogCreatingRewriter(logger, path);

        var directoryName = Path.GetDirectoryName(path) ?? String.Empty;
        var markdownSource = await context.FileProvider.ReadAllText(
            path: path,
            baseDirectory: String.Empty,
            ct);
        var result = new MarkdownRewriter(markdownSource, context, logger);

        var regions = CodeRegion.ParseAll(markdownSource);

        result.LogParsingRegions(path);

        foreach (var region in regions)
        {
            ct.ThrowIfCancellationRequested();

            result.LogFoundRegion(region.Path, region.Selector);

            var replacementNodeRoot = await context.GetCompilationUnit(
                path: region.Path,
                baseDirectory: directoryName,
                ct);
            result._replacements.Add((region, replacementNodeRoot));
        }

        result.LogDoneParsingRegions(path);

        return result;
    }

    /// <summary>
    /// Rewrites the stored Markdown to a string, replacing template directives with source texts.
    /// </summary>
    /// <param name="ct">
    /// A cancellation token used to request rewriting to be canceled.
    /// </param>
    /// <returns>
    /// A task that, upon completion, contains the rewritten Markdown source text.
    /// </returns>
    public async ValueTask<String> Rewrite(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var resultBuilder = new StringBuilder();

        await using (var target = new StringWriter(resultBuilder))
        {
            await Rewrite(target, ct);
        }

        var result = resultBuilder.ToString();

        return result;
    }

    /// <summary>
    /// Rewrites the stored Markdown to a text writer, replacing template directives with source texts.
    /// </summary>
    /// <param name="target">
    /// The writer to write the rewritten Markdown source text to.
    /// </param>
    /// <param name="ct">
    /// A cancellation token used to request rewriting to be canceled.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous rewriting operation.
    /// </returns>
    public async ValueTask Rewrite(TextWriter target, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(target);

        ct.ThrowIfCancellationRequested();

        var lastRegionEnd = 0;

        foreach (var (region, replacementNodeRoot) in _replacements)
        {
            ct.ThrowIfCancellationRequested();

            LogReplacingRegion(region.Path, region.Selector);

            var (codeStart, codeLength) = region.CodeRange.GetOffsetAndLength(_markdownSource.Length);

            var textBeforeRegion = _markdownSource.AsMemory(
                start: lastRegionEnd,
                length: codeStart - lastRegionEnd);

            await target.WriteAsync(textBeforeRegion, ct);
            await target.WriteLineAsync("```cs".AsMemory(), ct);

            var replacementNodes = _context.Selector.Select(replacementNodeRoot, region.Selector.Span, ct);

            LogWritingNodesToTarget(replacementNodes.Length);

            for (var i = 0; i < replacementNodes.Length; i++)
            {
                ct.ThrowIfCancellationRequested();

                if (i is not 0)
                {
                    await target.WriteLineAsync(ReadOnlyMemory<Char>.Empty, ct);
                }

                var replacementNode = replacementNodes[i];

                await _context.Formatter.Format(target, replacementNode, ct);
            }

            await target.WriteLineAsync(ReadOnlyMemory<Char>.Empty, ct);
            await target.WriteAsync("```".AsMemory(), ct);

            lastRegionEnd = codeStart + codeLength;
        }

        var tail = _markdownSource.AsMemory(lastRegionEnd);
        await target.WriteAsync(tail, ct);
    }

    [LoggerMessage(LogLevel.Debug, "Creating markdown rewriter for `{Path}`.")]
    static partial void LogCreatingRewriter(ILogger<MarkdownRewriter> logger, String path);

    [LoggerMessage(LogLevel.Debug, "Found code region referencing path `{Path}` and selector `{Selector}`.")]
    partial void LogFoundRegion(String Path, ReadOnlyMemory<Char> Selector);

    [LoggerMessage(LogLevel.Debug, "Found code region referencing path `{Path}` and selector `{Selector}`.")]
    partial void LogReplacingRegion(String Path, ReadOnlyMemory<Char> Selector);

    [LoggerMessage(LogLevel.Debug, "Parsing code regions in `{Path}`.")]
    partial void LogParsingRegions(String Path);

    [LoggerMessage(LogLevel.Debug, "Done parsing code regions in `{Path}`.")]
    partial void LogDoneParsingRegions(String Path);

    [LoggerMessage(LogLevel.Debug, "Writing `{count}` nodes to target.")]
    private partial void LogWritingNodesToTarget(Int32 count);
}

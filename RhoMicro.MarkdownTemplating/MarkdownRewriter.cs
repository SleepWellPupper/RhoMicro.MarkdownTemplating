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
        List<(Int32 index, CodeRegion region, Task<CompilationUnitSyntax> rootTask)>? lateReplacements = null;

        LogParsingRegions(logger, path);

        foreach (var region in regions)
        {
            ct.ThrowIfCancellationRequested();

            LogFoundRegion(logger, region.Path, region.Selector);

            var compilationUnitTask = context.GetCompilationUnit(
                path: region.Path,
                baseDirectory: directoryName,
                ct);
            if (compilationUnitTask is { IsCompletedSuccessfully: true, Result: var replacementNodeRoot })
            {
                result._replacements.Add((region, replacementNodeRoot));
            }
            else
            {
                lateReplacements ??= [];
                lateReplacements.Add((
                    index: result._replacements.Count,
                    region,
                    rootTask: compilationUnitTask.AsTask()));
                result._replacements.Add(default);
            }
        }

        LogDoneParsingRegions(logger, path);

        if (lateReplacements is null)
        {
            return result;
        }

        foreach (var (index, region, rootTask) in lateReplacements)
        {
            ct.ThrowIfCancellationRequested();

            var replacementNodeRoot = await rootTask.WaitAsync(ct);
            result._replacements[index] = (region, replacementNodeRoot);
        }

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

            LogReplacingRegion(_logger, region.Path, region.Selector);

            var (codeStart, codeLength) = region.CodeRange.GetOffsetAndLength(_markdownSource.Length);

            var textBeforeRegion = _markdownSource.AsMemory(
                start: lastRegionEnd,
                length: codeStart - lastRegionEnd);

            await target.WriteAsync(textBeforeRegion, ct);
            await target.WriteLineAsync("```cs".AsMemory(), ct);

            var replacementNodes = replacementNodeRoot.Select(region.Selector.Span, ct);

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
    static partial void LogFoundRegion(ILogger<MarkdownRewriter> logger, String Path, ReadOnlyMemory<Char> Selector);

    [LoggerMessage(LogLevel.Debug, "Found code region referencing path `{Path}` and selector `{Selector}`.")]
    static partial void LogReplacingRegion(
        ILogger<MarkdownRewriter> logger,
        String Path,
        ReadOnlyMemory<Char> Selector);

    [LoggerMessage(LogLevel.Debug, "Parsing code regions in `{Path}`.")]
    static partial void LogParsingRegions(ILogger<MarkdownRewriter> logger, String Path);

    [LoggerMessage(LogLevel.Debug, "Done parsing code regions in `{Path}`.")]
    static partial void LogDoneParsingRegions(ILogger<MarkdownRewriter> logger, String Path);
}

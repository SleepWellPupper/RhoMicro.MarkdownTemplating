// SPDX-License-Identifier: MPL-2.0

namespace RhoMicro.MarkdownTemplating;

using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

/// <summary>
/// Provides contextual services for <see cref="MarkdownRewriter"/> instances.
/// </summary>
/// <param name="options">
/// The parse options to use when parsing C# source files.
/// </param>
/// <param name="fileProvider">
/// The file provider to use when loading file contents into memory.
/// </param>
/// <param name="formatter">
/// The formatter to use when writing syntax nodes to the target stream.
/// </param>
public sealed class MarkdownRewriterContext(
    CSharpParseOptions options,
    IMarkdownRewriterFileProvider fileProvider,
    ISyntaxNodeFormatter formatter)
{
    /// <summary>
    /// Creates a context instance with default service implementations.
    /// </summary>
    /// <returns>
    /// A new context instance.
    /// </returns>
    public static MarkdownRewriterContext CreateDefault()
        => CreateDefault(NullLoggerFactory.Instance);

    /// <summary>
    /// Creates a context instance with default service implementations.
    /// </summary>
    /// <param name="loggers">
    /// The factory to use when creating loggers for context services.
    /// </param>
    /// <returns>
    /// A new context instance.
    /// </returns>
    public static MarkdownRewriterContext CreateDefault(ILoggerFactory loggers) => new(
        CSharpParseOptions.Default,
        new MarkdownRewriterSystemFileProvider(
            loggers.CreateLogger<MarkdownRewriterSystemFileProvider>()),
        NormalizedSyntaxNodeFormatter.Default);

    private readonly ConcurrentDictionary<String, CompilationUnitSyntax> _syntaxTrees
        = new();

    /// <summary>
    /// Gets the formatter to use when writing syntax nodes to the target stream.
    /// </summary>
    public ISyntaxNodeFormatter Formatter { get; } = formatter;

    /// <summary>
    /// Gets the file provider to use when loading files into memory.
    /// </summary>
    public IMarkdownRewriterFileProvider FileProvider { get; } = fileProvider;

    /// <summary>
    /// Gets the compilation unit root of the specified source file.
    /// </summary>
    /// <param name="path">
    /// The path to the C# source file.
    /// </param>
    /// <param name="baseDirectory">
    /// The directory relative to which to resolve the C# source file path.
    /// </param>
    /// <param name="ct">
    /// A cancellation token used to request compilation unit loading to be canceled. 
    /// </param>
    /// <returns>
    /// A task that, upon completion, contains the loaded compilation unit syntax.
    /// </returns>
    public async ValueTask<CompilationUnitSyntax> GetCompilationUnit(
        String path,
        String baseDirectory,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var key = Path.GetFullPath(
            path: path,
            basePath: Path.GetFullPath(baseDirectory));

        if (_syntaxTrees.TryGetValue(key, out var result))
        {
            return result;
        }

        var sourceText = await FileProvider.ReadAllText(
            path: path,
            baseDirectory: baseDirectory,
            ct);
        var cst = CSharpSyntaxTree.ParseText(
            sourceText,
            options,
            path,
            cancellationToken: ct);
        result = cst.GetCompilationUnitRoot(ct);

        _syntaxTrees[key] = result;

        return result;
    }
}

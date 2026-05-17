// SPDX-License-Identifier: MPL-2.0

namespace RhoMicro.MarkdownTemplating;

using System.Collections;
using System.Text.RegularExpressions;

/// <summary>
/// Models the parsed values for a code region and accompanying member path and selector.
/// </summary>
public readonly partial record struct CodeRegion
{
    /// <summary>
    /// Parses and enumerates code regions from a Markdown source text.
    /// </summary>
    /// <param name="markdownSource">
    /// The Markdown source text to parse.
    /// </param>
    public ref struct Enumerator(String markdownSource) : IEnumerator<CodeRegion>
    {
        private Regex.ValueMatchEnumerator _matches = GetFullPattern().EnumerateMatches(markdownSource);

        /// <inheritdoc />
        public CodeRegion Current
        {
            get
            {
                var matchRange = _matches.Current;
                var match = markdownSource.AsMemory(matchRange.Index, matchRange.Length);

                var atIndex = match.Span.LastIndexOf('@');
                var selector = ReadOnlyMemory<Char>.Empty;
                if (atIndex is not -1)
                {
                    var selectorStart = atIndex + 1; // selector starts after @
                    var selectorEnd = match.Span.LastIndexOf(')');
                    selector = match.Slice(
                        start: selectorStart,
                        length: selectorEnd - selectorStart);
                }

                var lastNewline = match.Span.LastIndexOfAny('\n', '\r');
                // path starts on next line + comment_syntax.Length
                var pathStart = lastNewline + 1 + "[//]:(rmmt://".Length;
                var pathEnd = atIndex is -1 // no selector?
                    ? match.Length - 1 // handle closing parenthesis
                    : atIndex;
                var path = match[pathStart..pathEnd].ToString();

                var codeLength = lastNewline;
                var codeRange = new Range(
                    start: matchRange.Index,
                    end: matchRange.Index + codeLength);

                var result = new CodeRegion(
                    codeRange,
                    path: path,
                    selector: selector);

                return result;
            }
        }


        /// <inheritdoc />
        public Boolean MoveNext() => _matches.MoveNext();

        Object IEnumerator.Current => throw new NotSupportedException();

        void IEnumerator.Reset()
        {
        }

        void IDisposable.Dispose()
        {
        }

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
        public Enumerator GetEnumerator() => this;
    }

    private CodeRegion(
        Range codeRange,
        String path,
        ReadOnlyMemory<Char> selector)
    {
        CodeRange = codeRange;
        Path = path;
        Selector = selector;
    }

    /// <summary>
    /// Gets the location in the Markdown source text to be replaced by code.
    /// </summary>
    public Range CodeRange { get; }

    /// <summary>
    /// Gets the member selector identifying the member to copy into the code region.
    /// </summary>
    public ReadOnlyMemory<Char> Selector { get; }

    /// <summary>
    /// Gets the file path referenced by the code region.
    /// </summary>
    public String Path { get; }

    /// <summary>
    /// Parses all code regions in the Markdown source text.
    /// </summary>
    /// <param name="markdownSource">
    /// The source text from which to parse code regions.
    /// </param>
    /// <returns>
    /// A new enumerator for enumerating parsed code regions.
    /// </returns>
    public static Enumerator ParseAll(String markdownSource) => new(markdownSource);

    [GeneratedRegex(
        @"```cs(\r|\n|\r\n)((\1(``?)?)|[^`])*```(\r|\n|\r\n)\[\/\/\]:\(rmmt:\/\/.*(@[a-zA-Z0-9]+(.[a-zA-Z0-9]+)*)?\)",
        RegexOptions.Multiline,
        matchTimeoutMilliseconds: 1000)]
    private static partial Regex GetFullPattern();
}

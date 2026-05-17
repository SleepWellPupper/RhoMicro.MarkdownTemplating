// SPDX-License-Identifier: MPL-2.0

namespace RhoMicro.MarkdownTemplating;

using System.Buffers;
using System.Collections;
using System.Diagnostics;
using System.Security.Principal;
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
    public struct Enumerator(String markdownSource) : IEnumerator<CodeRegion>
    {
        private Int32 _index;
        // private Regex.ValueMatchEnumerator _matches = GetFullPattern().EnumerateMatches(markdownSource);

        /// <inheritdoc />
        public CodeRegion Current { get; private set; }

        /// <inheritdoc />
        public override String ToString() => $"Tail: {markdownSource[_index..]}";

        private static readonly SearchValues<Char> _newlineSearchValues =
            SearchValues.Create("\n\r\f\u0085\u2028\u2029");

        /// <inheritdoc />
        public Boolean MoveNext()
        {
            var index = _index;

            const String codeStartToken = "```cs";
            var codeStart = index;
            var source = markdownSource.AsMemory(index);
            var lines = source.Span.SplitAny(_newlineSearchValues);

            while (true)
            {
                if (!lines.MoveNext())
                {
                    return false;
                }

                var line = source.Span[lines.Current];

                if (line.SequenceEqual(codeStartToken))
                {
                    break;
                }

                var lineLength = line.Length + 1;
                index += lineLength;
                codeStart += lineLength;
            }

            const String codeEndToken = "```";
            var codeLength = codeStartToken.Length;

            while (true)
            {
                if (!lines.MoveNext())
                {
                    return false;
                }

                var line = source.Span[lines.Current];
                var lineLength = line.Length + 1;
                index += lineLength;
                codeLength += lineLength;

                if (line.SequenceEqual(codeEndToken))
                {
                    break;
                }
            }

            var codeEnd = codeStart + codeLength;
            var codeRange = new Range(
                start: Index.FromStart(codeStart),
                end: Index.FromStart(codeEnd));

            while (true)
            {
                if (!lines.MoveNext())
                {
                    return false;
                }

                if (lines.Current.Start.Equals(lines.Current.End))
                {
                    index += 1;
                    continue;
                }

                break;
            }

            const String pathStart = "[//]:(rmmdt://";

            var lastLine = source[lines.Current];

            if (!lastLine.Span.StartsWith(pathStart))
            {
                return false;
            }

            lastLine = lastLine[pathStart.Length..];

            var pathLength = lastLine.Span.IndexOf('@');
            var selectorLength = 0;
            var selectorStart = pathLength + 1;

            if (pathLength is -1)
            {
                pathLength = lastLine.Length - 1;
                selectorStart = 0;
            }
            else
            {
                selectorLength = lastLine.Length - pathLength - 2;
            }

            var path = lastLine[..pathLength].ToString();
            var selector = lastLine.Slice(selectorStart, selectorLength);

            Current = new(codeRange, path, selector);

            _index = index;

            return true;
        }

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
}

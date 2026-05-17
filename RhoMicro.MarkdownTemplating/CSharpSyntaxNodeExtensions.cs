// SPDX-License-Identifier: MPL-2.0

namespace RhoMicro.MarkdownTemplating;

using System.Collections.Immutable;
using System.Globalization;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

/// <summary>
/// Provides extensions for finding syntax nodes based on a selector string.
/// </summary>
public static class CSharpSyntaxNodeExtensions
{
    sealed class MemberNameGetter : CSharpSyntaxVisitor<String>
    {
        private MemberNameGetter()
        {
        }

        public override String? VisitNamespaceDeclaration(NamespaceDeclarationSyntax node) => node.Name.ToString();
        public override String? VisitClassDeclaration(ClassDeclarationSyntax node) => node.Identifier.ValueText;
        public override String? VisitInterfaceDeclaration(InterfaceDeclarationSyntax node) => node.Identifier.ValueText;
        public override String? VisitStructDeclaration(StructDeclarationSyntax node) => node.Identifier.ValueText;
        public override String? VisitDelegateDeclaration(DelegateDeclarationSyntax node) => node.Identifier.ValueText;
        public override String? VisitEnumDeclaration(EnumDeclarationSyntax node) => node.Identifier.ValueText;
        public override String? VisitRecordDeclaration(RecordDeclarationSyntax node) => node.Identifier.ValueText;
        public override String? VisitMethodDeclaration(MethodDeclarationSyntax node) => node.Identifier.ValueText;
        public override String? VisitPropertyDeclaration(PropertyDeclarationSyntax node) => node.Identifier.ValueText;

        public static MemberNameGetter Instance { get; } = new();
    }

    extension(CSharpSyntaxNode node)
    {
        /// <summary>
        /// Finds descendant nodes identified by the selector.
        /// </summary>
        /// <param name="selector">
        /// The selector identifying descendant nodes.
        /// </param>
        /// <param name="ct">
        /// A cancellation token used to request selection to be canceled.
        /// </param>
        /// <returns>
        /// The selected descendant nodes.
        /// </returns>
        public ImmutableArray<CSharpSyntaxNode> Select(ReadOnlySpan<Char> selector, CancellationToken ct = default)
        {
            var selected = ImmutableArray.CreateBuilder<CSharpSyntaxNode>();
            node.Select(selector, selected, ct);
            var result = selected.DrainToImmutable();

            return result;
        }

        private void Select(
            ReadOnlySpan<Char> selector,
            ImmutableArray<CSharpSyntaxNode>.Builder selected,
            CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            var remainingSelectors = selector.Split('.');
            // The split enumerator is guaranteed to have elements,
            // as an empty enumeration yields an empty range.
            // This fits our syntax of an empty selector being a
            // valid (self) selector.
            remainingSelectors.MoveNext();
            node.Select(selector, remainingSelectors, selected, ct);
        }

        private void Select(
            ReadOnlySpan<Char> fullSelector,
            MemoryExtensions.SpanSplitEnumerator<Char> currentSelector,
            ImmutableArray<CSharpSyntaxNode>.Builder selected,
            CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var currentRange = currentSelector.Current;
            var (selectorIndex, selectorLength) = currentRange.GetOffsetAndLength(fullSelector.Length);

            // Empty selectors defined as self-selecting.
            if (selectorLength is 0)
            {
                node.SelectNext(
                    fullSelector,
                    currentSelector,
                    selected,
                    ct);

                return;
            }

            var selector = fullSelector[currentRange];

            // Integer selectors are defined as index selectors,
            // selecting the nth direct child of the current node.
            if (Int32.TryParse(selector, CultureInfo.InvariantCulture, out var index))
            {
                var i = 0;

                foreach (var childNode in node.ChildNodes())
                {
                    ct.ThrowIfCancellationRequested();

                    if (childNode is not CSharpSyntaxNode csharpChild)
                    {
                        continue;
                    }

                    if (i++ > index)
                    {
                        throw new InvalidOperationException(
                            $"""
                             Found invalid index selector at index {selectorIndex}:
                             {fullSelector}
                             {new String(' ', selectorIndex)}{new String('^', selectorLength)}
                             """);
                    }

                    if (i < index)
                    {
                        continue;
                    }

                    csharpChild.SelectNext(
                        fullSelector,
                        currentSelector,
                        selected,
                        ct);

                    return;
                }
            }

            // Wildcard name selectors match all children.
            if (selector is not ['*'])
            {
                foreach (var childNode in node.ChildNodes())
                {
                    ct.ThrowIfCancellationRequested();

                    if (childNode is not CSharpSyntaxNode csharpChild)
                    {
                        continue;
                    }

                    csharpChild.SelectNext(
                        fullSelector,
                        currentSelector,
                        selected,
                        ct);
                }

                return;
            }

            // All other selectors are defined as name selectors,
            // selecting all descendants of the current node that
            // start with the selector.
            foreach (var descendantNode in node.DescendantNodes())
            {
                ct.ThrowIfCancellationRequested();

                if (descendantNode is not CSharpSyntaxNode csharpDescendant)
                {
                    continue;
                }

                // Does the descendant have a name?
                if (csharpDescendant.Accept(MemberNameGetter.Instance) is not { } descendantName)
                {
                    continue;
                }

                if (!descendantName.StartsWith(selector, StringComparison.Ordinal))
                {
                    continue;
                }

                csharpDescendant.SelectNext(
                    fullSelector,
                    currentSelector,
                    selected,
                    ct);
            }
        }

        private void SelectNext(
            ReadOnlySpan<Char> fullSelector,
            MemoryExtensions.SpanSplitEnumerator<Char> currentSelector,
            ImmutableArray<CSharpSyntaxNode>.Builder selected,
            CancellationToken ct)
        {
            var currentRange = currentSelector.Current;
            var (selectorIndex, selectorLength) = currentRange.GetOffsetAndLength(fullSelector.Length);
            var selectorEnd = selectorIndex + selectorLength;

            if (currentSelector.MoveNext())
            {
                var remainingSelectors = fullSelector[(selectorEnd + 1)..].Split('.');
                remainingSelectors.MoveNext();
                node.Select(fullSelector, currentSelector, selected, ct);
            }
            else
            {
                selected.Add(node);
            }
        }
    }
}

// SPDX-License-Identifier: MPL-2.0

namespace RhoMicro.MarkdownTemplating;

using System.Collections.Immutable;
using System.Globalization;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;

/// <summary>
/// Finds syntax nodes based on a selector string.
/// </summary>
public sealed partial class CSharpSyntaxNodeSelector(ILogger<CSharpSyntaxNodeSelector> logger)
{
    sealed class MemberNameGetter : CSharpSyntaxVisitor<String>
    {
        private MemberNameGetter()
        {
        }

        public override String? VisitExtensionBlockDeclaration(ExtensionBlockDeclarationSyntax node) => node.Identifier.ValueText;
        public override String? VisitLocalFunctionStatement(LocalFunctionStatementSyntax node) => node.Identifier.ValueText;
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

    /// <summary>
    /// Finds descendant nodes identified by the selector.
    /// </summary>
    /// <param name="node">
    /// The node to select from.
    /// </param>
    /// <param name="path">
    /// The selector identifying descendant nodes.
    /// </param>
    /// <param name="ct">
    /// A cancellation token used to request selection to be canceled.
    /// </param>
    /// <returns>
    /// The selected descendant nodes.
    /// </returns>
    public ImmutableArray<CSharpSyntaxNode> Select(
        CSharpSyntaxNode node,
        ReadOnlySpan<Char> path,
        CancellationToken ct = default)
    {
        var selected = ImmutableArray.CreateBuilder<CSharpSyntaxNode>();
        Select(node, path, selected, ct);
        var result = selected.DrainToImmutable();

        return result;
    }

    private void Select(
        CSharpSyntaxNode node,
        ReadOnlySpan<Char> path,
        ImmutableArray<CSharpSyntaxNode>.Builder selected,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var remainingSelectors = path.Split('.');
        // The split enumerator is guaranteed to have elements,
        // as an empty enumeration yields an empty range.
        // This fits our syntax of an empty selector being a
        // valid (self) selector.
        remainingSelectors.MoveNext();
        Select(node, path, remainingSelectors, selected, ct);
    }

    private void Select(
        CSharpSyntaxNode node,
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
            LogSelfSelection(logger, node.Kind());

            SelectNext(node,
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

                LogIndexSelection(logger, csharpChild.Kind(), index);

                SelectNext(
                    csharpChild,
                    fullSelector,
                    currentSelector,
                    selected,
                    ct);

                return;
            }
        }

        // Wildcard name selectors match all children.
        if (selector is ['*'])
        {
            foreach (var childNode in node.ChildNodes())
            {
                ct.ThrowIfCancellationRequested();

                if (childNode is not CSharpSyntaxNode csharpChild)
                {
                    continue;
                }

                LogWildcardSelection(logger, csharpChild.Kind());

                SelectNext(
                    csharpChild,
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

            LogNameSelection(logger, selector, csharpDescendant.Kind());

            SelectNext(
                csharpDescendant,
                fullSelector,
                currentSelector,
                selected,
                ct);
        }
    }

    private void SelectNext(
        CSharpSyntaxNode node,
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
            Select(node, fullSelector, currentSelector, selected, ct);
        }
        else
        {
            selected.Add(node);
        }
    }

    [LoggerMessage(LogLevel.Debug, "Self selector selected `{node}`.")]
    static partial void LogSelfSelection(ILogger<CSharpSyntaxNodeSelector> logger, SyntaxKind node);

    [LoggerMessage(LogLevel.Debug, "Index selector selected child `{node}` at index `{index}`.")]
    static partial void LogIndexSelection(
        ILogger<CSharpSyntaxNodeSelector> logger,
        SyntaxKind node,
        Int32 index);

    [LoggerMessage(LogLevel.Debug, "Wildcard selector selected `{node}`.")]
    static partial void LogWildcardSelection(ILogger<CSharpSyntaxNodeSelector> logger, SyntaxKind node);

    [LoggerMessage(LogLevel.Debug, "Name selector `{selector}` selected `{node}`.")]
    static partial void LogNameSelection(ILogger<CSharpSyntaxNodeSelector> logger, String selector, SyntaxKind node);

    private static void LogNameSelection(
        ILogger<CSharpSyntaxNodeSelector> logger,
        ReadOnlySpan<Char> selector,
        SyntaxKind node)
    {
        if (logger.IsEnabled(LogLevel.Debug))
        {
            LogNameSelection(logger, selector.ToString(), node);
        }
    }
}

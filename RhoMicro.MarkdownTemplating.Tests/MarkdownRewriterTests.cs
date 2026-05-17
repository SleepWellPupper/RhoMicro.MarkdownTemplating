// SPDX-License-Identifier: MPL-2.0

namespace RhoMicro.MarkdownTemplating.Tests;

using Meziantou.Extensions.Logging.Xunit.v3;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging.Abstractions;

public class MarkdownRewriterTests
{
    [Fact]
    public async Task ParsesSpacedPath()
    {
        // Arrange
        var context = new MarkdownRewriterContext(
            CSharpParseOptions.Default,
            new MarkdownRewriterMemoryFileProvider(
                new Dictionary<String, String>
                {
                    ["docs/readme.md"] =
                        """
                        # Sample 1
                        ```cs
                        ```
                        
                        [//]:(rmmdt://../src/Class.cs@Class.2)

                        # Sample 2
                        ```cs
                        old
                        code
                        ```
                        
                        [//]:(rmmdt://../src/Struct.cs)

                        - bullet point
                        """,
                    ["../src/Struct.cs"] =
                        """
                        namespace Foo;

                        public struct Struct;
                        """,
                    ["../src/Class.cs"] =
                        """
                        public class Class
                        {
                            public int Property1 { get; }
                            public int Property2 { get; }
                            public int Property3 { get; }
                        }
                        """,
                }),
            NormalizedSyntaxNodeFormatter.Default,
            new CSharpSyntaxNodeSelector(
                XUnitLogger.CreateLogger<CSharpSyntaxNodeSelector>()));

        // Act
        var rewriter = await MarkdownRewriter.Create(
            "docs/readme.md",
            context,
            NullLogger<MarkdownRewriter>.Instance,
            TestContext.Current.CancellationToken);
        var actual = await rewriter.Rewrite(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(
            """
            # Sample 1
            ```cs
            public int Property2 { get; }
            ```
            
            [//]:(rmmdt://../src/Class.cs@Class.2)

            # Sample 2
            ```cs
            namespace Foo;
            public struct Struct;
            ```
            
            [//]:(rmmdt://../src/Struct.cs)

            - bullet point
            """,
            actual);
    }
    
    [Fact]
    public async Task RewritesMarkdown()
    {
        // Arrange
        var context = new MarkdownRewriterContext(
            CSharpParseOptions.Default,
            new MarkdownRewriterMemoryFileProvider(
                new Dictionary<String, String>
                {
                    ["docs/readme.md"] =
                        """
                        # Sample 1
                        ```cs
                        ```
                        [//]:(rmmdt://../src/Class.cs@Class.2)

                        # Sample 2
                        ```cs
                        old
                        code
                        ```
                        [//]:(rmmdt://../src/Struct.cs)

                        - bullet point
                        """,
                    ["../src/Struct.cs"] =
                        """
                        namespace Foo;

                        public struct Struct;
                        """,
                    ["../src/Class.cs"] =
                        """
                        public class Class
                        {
                            public int Property1 { get; }
                            public int Property2 { get; }
                            public int Property3 { get; }
                        }
                        """,
                }),
            NormalizedSyntaxNodeFormatter.Default,
            new CSharpSyntaxNodeSelector(
                XUnitLogger.CreateLogger<CSharpSyntaxNodeSelector>()));

        // Act
        var rewriter = await MarkdownRewriter.Create(
            "docs/readme.md",
            context,
            NullLogger<MarkdownRewriter>.Instance,
            TestContext.Current.CancellationToken);
        var actual = await rewriter.Rewrite(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(
            """
            # Sample 1
            ```cs
            public int Property2 { get; }
            ```
            [//]:(rmmdt://../src/Class.cs@Class.2)

            # Sample 2
            ```cs
            namespace Foo;
            public struct Struct;
            ```
            [//]:(rmmdt://../src/Struct.cs)

            - bullet point
            """,
            actual);
    }
}

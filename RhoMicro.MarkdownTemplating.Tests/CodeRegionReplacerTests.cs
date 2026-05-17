// SPDX-License-Identifier: MPL-2.0

namespace RhoMicro.MarkdownTemplating.Tests;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging.Abstractions;

public class MarkdownRewriterTests
{
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
                        [//]:(rmmt://../src/Class.cs@Class.1)

                        # Sample 2
                        ```cs
                        old
                        code
                        ```
                        [//]:(rmmt://../src/Struct.cs)
                        
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
            NormalizedSyntaxNodeFormatter.Default);

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
            [//]:(rmmt://../src/Class.cs@Class.1)

            # Sample 2
            ```cs
            namespace Foo;
            public struct Struct;
            ```
            [//]:(rmmt://../src/Struct.cs)

            - bullet point
            """,
            actual);
    }
}

// SPDX-License-Identifier: MPL-2.0

namespace RhoMicro.MarkdownTemplating.Tests;

using System.Globalization;

public class CodeRegionParserTests
{
    [Theory]
    [InlineData(
        """
        # Sample Code

        {0}
        [//]:(rmmt://{1}@{2})
        [The method to include.](RhoMicro.MarkdownTemplating.Tests/UnitTest.cs)

        """,
        """
        ```cs
        previous code
        ```
        """,
        "RhoMicro.MarkdownTemplating.Tests/UnitTest.cs",
        "0.Foo.MethodToInclude")]
    public void ParsesSingleCodeRegion(
        String markdownSourceFormat,
        String expectedCode,
        String expectedPath,
        String expectedSelector)
    {
        // Arrange
        var markdownSource = String.Format(
            CultureInfo.InvariantCulture,
            markdownSourceFormat,
            expectedCode,
            expectedPath,
            expectedSelector);

        // Act
        var regions = CodeRegion.ParseAll(markdownSource);

        // Assert
        Assert.True(regions.MoveNext());
        Assert.False(regions.MoveNext());
        var region = regions.Current;
        Assert.Equal(expectedCode, markdownSource[region.CodeRange]);
        Assert.Equal(expectedPath, region.Path);
        Assert.Equal(expectedSelector, region.Selector.Span);
    }

    [Fact]
    public void ParsesMultipleCodeRegions()
    {
        // Arrange
        var markdownSource =
            """
            # Sample 1
            ```cs
            ```
            [//]:(rmmt://path.cs@Class.Property)

            # Sample 2
            ```cs
            old
            code
            ```
            [//]:(rmmt://file.cs)

            - bullet point
            """;

        // Act
        var regions = CodeRegion.ParseAll(markdownSource);

        // Assert
        Assert.True(regions.MoveNext());
        var firstRegion = regions.Current;
        Assert.Equal(
            """
            ```cs
            ```
            """
            , markdownSource[firstRegion.CodeRange]);
        Assert.Equal("path.cs", firstRegion.Path);
        Assert.Equal("Class.Property", firstRegion.Selector.Span);

        Assert.True(regions.MoveNext());
        var secondRegion = regions.Current;
        Assert.Equal(
            """
            ```cs
            old
            code
            ```
            """, markdownSource[secondRegion.CodeRange]);
        Assert.Equal("file.cs", secondRegion.Path);
        Assert.Equal(String.Empty, secondRegion.Selector.Span);
    }
}

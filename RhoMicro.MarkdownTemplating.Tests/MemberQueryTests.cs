// SPDX-License-Identifier: MPL-2.0

namespace RhoMicro.MarkdownTemplating.Tests;

using Microsoft.CodeAnalysis.CSharp;

public class MemberQueryTests
{
    [Theory]
    [InlineData(
        """
        public class Foo
        {
            public void Bar(){}
        }
        """,
        "public void Bar(){}",
        "0.0")]
    [InlineData(
        """
        public class Foo
        {
            public void Bar(){}
        }
        """,
        "public void Bar(){}",
        "0.Bar")]
    [InlineData(
        """
        public class Foo
        {
            public void Bar(){}
        }
        """,
        "public void Bar(){}",
        "Foo.0")]
    [InlineData(
        """
        public class Foo
        {
            public void Bar(){}
        }
        """,
        "public void Bar(){}",
        "Foo.Bar")]
    [InlineData(
        """
        public class Foo
        {
            public void Bar1(){}
            public void Bar2(){}
        }
        """,
        """
        public void Bar1(){}
        public void Bar2(){}
        """,
        "Foo.Bar")]
    [InlineData(
        """
        public class Foo
        {
            public void Bar1(){}
            public void Bar2(){}
        }
        """,
        """
        public void Bar1(){}
        public void Bar2(){}
        """,
        "Foo.Bar.*")]
    [InlineData(
        """
        public class Foo
        {
            public void Bar1(){}
            public void Bar2(){}
        }
        """,
        """
        public void Bar1(){}
        """,
        "Foo.Bar.0")]
    [InlineData(
        """
        public class Foo
        {
            public void Bar1(){}
            public void Bar2(){}
        }
        """,
        """
        public void Bar2(){}
        """,
        "Foo.Bar.1")]
    [InlineData(
        """
        public class Foo
        {
            public void Bar1(int value){}
            public void Bar2(int value){}
        }
        """,
        """
        (int value)
        (int value)
        """,
        "Foo.Bar.*.0")]
    [InlineData(
        """
        public class Foo
        {
            public void Bar1(int value){}
            public void Bar2(int value){}
        }
        """,
        """
        (int value)
        """,
        "Foo.Bar.0.0")]
    public void QueriesExpectedSourceText(
        String sourceText,
        String expected,
        String selector)
    {
        // Arrange
        TestContext.Current.TestOutputHelper?.WriteLine($"Source text:\n{sourceText}");
        TestContext.Current.TestOutputHelper?.WriteLine($"Selector:\n{selector}");
        TestContext.Current.TestOutputHelper?.WriteLine($"Expected:\n{expected}");
        var root = CSharpSyntaxTree.ParseText(sourceText, cancellationToken: TestContext.Current.CancellationToken)
            .GetCompilationUnitRoot(cancellationToken: TestContext.Current.CancellationToken);

        // Act
        var selected = root.Select(selector, TestContext.Current.CancellationToken);

        // Assert
        var actual = String.Join("\n", selected);
        TestContext.Current.TestOutputHelper?.WriteLine($"Actual:\n{actual}");
        Assert.Equal(expected, actual);
    }
}

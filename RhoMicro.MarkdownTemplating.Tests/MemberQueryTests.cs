// SPDX-License-Identifier: MPL-2.0

namespace RhoMicro.MarkdownTemplating.Tests;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.DependencyInjection;

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
        "Foo.*")]
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
        "Foo.0")]
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
        "Foo.2")]
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
        "Foo.Bar.2")]
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
        "Foo.0.2")]
    [InlineData(
        """
        // SPDX-License-Identifier: MPL-2.0

        using Microsoft.CodeAnalysis.CSharp;
        using Microsoft.Extensions.Hosting;
        using RhoMicro.MarkdownTemplating;

        internal sealed class InteractiveService(IHostApplicationLifetime lifetime) : BackgroundService
        {
            protected override async Task ExecuteAsync(CancellationToken ct)
            {
                while (!ct.IsCancellationRequested)
                {
                    try
                    {
                        Console.Clear();
                        Console.WriteLine("Welcome to the interactive mode.");
                        Console.Write("Enter a C# source file path: ");
                        var path = Console.ReadLine() ?? String.Empty;
                        var context = MarkdownRewriterContext.CreateDefault();
                        var compilation = await context.GetCompilationUnit(
                            path,
                            baseDirectory: Environment.CurrentDirectory,
                            ct: ct);

                        var selector = String.Empty;
                        while (!ct.IsCancellationRequested)
                        {
                            Console.Clear();
                            Console.WriteLine("Welcome to interactive mode.");
                            Console.WriteLine("Press ESC to exit the application.");
                            Console.WriteLine($"Source file: {path}");
                            Console.WriteLine("Press ENTER to select a different source file.");
                            Console.WriteLine();

                            try
                            {
                                var selectedNodes = compilation.Select(selector, ct);

                                if (selectedNodes is not [])
                                {
                                    foreach (var t in selectedNodes)
                                    {
                                        ct.ThrowIfCancellationRequested();
                                        
                                        var selectedNode = t;

                                        await context.Formatter.Format(Console.Out, selectedNode, ct);
                                        await Console.Out.WriteLineAsync(ReadOnlyMemory<Char>.Empty, ct);
                                    }

                                    Console.WriteLine($"Selected {String.Join(", ", selectedNodes.Select(n => n.Kind()))}:");
                                }
                                else
                                {
                                    Console.WriteLine("No matches.");
                                }
                            }
                            catch (OperationCanceledException) when (ct.IsCancellationRequested)
                            {
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("Error:");
                                Console.WriteLine(e);
                            }

                            Console.WriteLine($"Last selector: {selector}");
                            Console.Write($">> {selector}");

                            var key = Console.ReadKey();

                            if (key.Key is ConsoleKey.Escape)
                            {
                                lifetime.StopApplication();
                            }

                            if (key.Key is ConsoleKey.Enter)
                            {
                                break;
                            }

                            if (key.Key is ConsoleKey.Backspace)
                            {
                                selector = selector[..Math.Max(0,selector.Length-1)];
                                continue;
                            }

                            selector += key.KeyChar;
                        }
                    }
                    catch (OperationCanceledException) when (ct.IsCancellationRequested)
                    {
                        return;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error:");
                        Console.WriteLine(e);
                        Console.WriteLine("Press any key to continue.");
                        Console.ReadKey();
                    }
                }
            }
        }

        """,
        """
        internal sealed class InteractiveService(IHostApplicationLifetime lifetime) : BackgroundService
        {
            protected override async Task ExecuteAsync(CancellationToken ct)
            {
                while (!ct.IsCancellationRequested)
                {
                    try
                    {
                        Console.Clear();
                        Console.WriteLine("Welcome to the interactive mode.");
                        Console.Write("Enter a C# source file path: ");
                        var path = Console.ReadLine() ?? String.Empty;
                        var context = MarkdownRewriterContext.CreateDefault();
                        var compilation = await context.GetCompilationUnit(
                            path,
                            baseDirectory: Environment.CurrentDirectory,
                            ct: ct);

                        var selector = String.Empty;
                        while (!ct.IsCancellationRequested)
                        {
                            Console.Clear();
                            Console.WriteLine("Welcome to interactive mode.");
                            Console.WriteLine("Press ESC to exit the application.");
                            Console.WriteLine($"Source file: {path}");
                            Console.WriteLine("Press ENTER to select a different source file.");
                            Console.WriteLine();

                            try
                            {
                                var selectedNodes = compilation.Select(selector, ct);

                                if (selectedNodes is not [])
                                {
                                    foreach (var t in selectedNodes)
                                    {
                                        ct.ThrowIfCancellationRequested();
                                        
                                        var selectedNode = t;

                                        await context.Formatter.Format(Console.Out, selectedNode, ct);
                                        await Console.Out.WriteLineAsync(ReadOnlyMemory<Char>.Empty, ct);
                                    }

                                    Console.WriteLine($"Selected {String.Join(", ", selectedNodes.Select(n => n.Kind()))}:");
                                }
                                else
                                {
                                    Console.WriteLine("No matches.");
                                }
                            }
                            catch (OperationCanceledException) when (ct.IsCancellationRequested)
                            {
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("Error:");
                                Console.WriteLine(e);
                            }

                            Console.WriteLine($"Last selector: {selector}");
                            Console.Write($">> {selector}");

                            var key = Console.ReadKey();

                            if (key.Key is ConsoleKey.Escape)
                            {
                                lifetime.StopApplication();
                            }

                            if (key.Key is ConsoleKey.Enter)
                            {
                                break;
                            }

                            if (key.Key is ConsoleKey.Backspace)
                            {
                                selector = selector[..Math.Max(0,selector.Length-1)];
                                continue;
                            }

                            selector += key.KeyChar;
                        }
                    }
                    catch (OperationCanceledException) when (ct.IsCancellationRequested)
                    {
                        return;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error:");
                        Console.WriteLine(e);
                        Console.WriteLine("Press any key to continue.");
                        Console.ReadKey();
                    }
                }
            }
        }
        """,
        "InteractiveService")]
    public void QueriesExpectedSourceText(
        String sourceText,
        String expected,
        String selectorPath)
    {
        // Arrange
        TestContext.Current.TestOutputHelper?.WriteLine($"Source text:\n{sourceText}");
        TestContext.Current.TestOutputHelper?.WriteLine($"Selector:\n{selectorPath}");
        TestContext.Current.TestOutputHelper?.WriteLine($"Expected:\n{expected}");
        var root = CSharpSyntaxTree.ParseText(sourceText, cancellationToken: TestContext.Current.CancellationToken)
            .GetCompilationUnitRoot(cancellationToken: TestContext.Current.CancellationToken);
        var selector = new ServiceCollection()
            .AddMarkdownTemplating()
            .BuildServiceProvider()
            .GetRequiredService<CSharpSyntaxNodeSelector>();

        // Act
        var selected = selector.Select(root, selectorPath, TestContext.Current.CancellationToken);

        // Assert
        var actual = String.Join("\n", selected);
        TestContext.Current.TestOutputHelper?.WriteLine($"Actual:\n{actual}");
        Assert.Equal(expected, actual);
    }
}

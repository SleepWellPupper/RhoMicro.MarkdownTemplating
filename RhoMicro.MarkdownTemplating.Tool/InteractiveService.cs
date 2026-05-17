// SPDX-License-Identifier: MPL-2.0

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
                    Console.WriteLine("Welcome to the interactive mode.");
                    Console.WriteLine("Press ESC to exit the application.");
                    Console.WriteLine($"Source file: {path}");
                    Console.WriteLine("Press ENTER to select a different source file.");
                    Console.WriteLine();

                    try
                    {
                        var selectedNodes = compilation.Select(selector, ct);

                        if (selectedNodes is not [])
                        {
                            for (var i = 0; i < selectedNodes.Length; i++)
                            {
                                ct.ThrowIfCancellationRequested();

                                if (i is not 0)
                                {
                                    await Console.Out.WriteLineAsync(ReadOnlyMemory<Char>.Empty, ct);
                                }

                                var selectedNode = selectedNodes[i];

                                await context.Formatter.Format(Console.Out, selectedNode, ct);
                            }

                            Console.WriteLine();
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
                        selector = selector[..^1];
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

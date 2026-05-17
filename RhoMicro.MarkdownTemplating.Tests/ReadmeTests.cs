// SPDX-License-Identifier: MPL-2.0

namespace RhoMicro.MarkdownTemplating.Tests;

using Microsoft.Extensions.DependencyInjection;

public class ReadmeTests
{
    [Fact]
    public async Task SampleCompilesAndRuns()
    {
        var services = new ServiceCollection();
        var ct = TestContext.Current.CancellationToken;
        ExtensionSample(services);
        
        var serviceProvider = services
            .AddSingleton<IMarkdownRewriterFileProvider>(new MarkdownRewriterMemoryFileProvider(
                new Dictionary<String, String> { ["README.md"] = String.Empty }))
            .BuildServiceProvider();
        
        await ResolutionSample(serviceProvider);
        
        void ExtensionSample(IServiceCollection services)
        {
            services.AddMarkdownTemplating();
        }

        async Task ResolutionSample(IServiceProvider services)
        {
            var service = services.GetRequiredService<MarkdownRewriterService>();
            await service.Rewrite("README.md", ct);
        }
    }
}

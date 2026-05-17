// SPDX-License-Identifier: MPL-2.0

namespace RhoMicro.MarkdownTemplating;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

/// <summary>
/// Provides integration of Markdown templating services into DI containers.
/// </summary>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds Markdown templating services to the service collection.
        /// </summary>
        public IServiceCollection AddMarkdownTemplating()
        {
            services.TryAddTransient<MarkdownRewriterService>();
            services.TryAddTransient<MarkdownRewriterContext>();
            services.TryAddSingleton(CSharpParseOptions.Default);
            services.TryAddSingleton<IMarkdownRewriterFileProvider, MarkdownRewriterSystemFileProvider>();
            services.TryAddSingleton<ISyntaxNodeFormatter>(NormalizedSyntaxNodeFormatter.Default);

            return services;
        }
    }
}

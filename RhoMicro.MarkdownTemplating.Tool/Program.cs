// SPDX-License-Identifier: MPL-2.0

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RhoMicro.MarkdownTemplating;
using HostOptions = RhoMicro.MarkdownTemplating.HostOptions;

await Host
    .CreateDefaultBuilder(args)
    .ConfigureLogging((ctx, l) =>
    {
        var hostOptions = ctx.Configuration.Get<HostOptions>() ?? new();

        if (hostOptions.Mode is HostMode.Interactive)
        {
            l.ClearProviders();
        }
    })
    .ConfigureServices((ctx, s) =>
    {
        s.AddMarkdownTemplating();

        var hostOptions = ctx.Configuration.Get<HostOptions>() ?? new();

        switch (hostOptions.Mode)
        {
            case HostMode.Main:
                s.AddHostedService<MainService>()
                    .AddOptions<MainServiceOptions>()
                    .BindConfiguration(configSectionPath: String.Empty);
                break;
            case HostMode.Interactive:
                s.AddHostedService<InteractiveService>();
                break;
        }
    })
    .Build()
    .RunAsync();

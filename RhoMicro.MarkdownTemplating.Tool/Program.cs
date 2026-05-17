// SPDX-License-Identifier: MPL-2.0

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RhoMicro.MarkdownTemplating;
using HostOptions = RhoMicro.MarkdownTemplating.HostOptions;

if (args is [])
{
    Console.WriteLine("use the -h flag for a list of options");
}

if (args is ["-h", ..])
{
    Console.WriteLine(
        $"""
         Always available:
         Option:
         --{nameof(HostOptions.Mode)}=[{String.Join("|", Enum.GetNames<HostMode>())}]
         Description:
         Sets the execution mode. When set to `Rewrite`, target files will be rewritten according
         to the options below. When set to `Interactive`, an interactive session is started that
         allows for exploration of the C# syntax query language.

         Available when in `Rewrite` mode:
         Option:
         --{nameof(MainServiceOptions.WorkingDirectory)}=*
         Description:
         Target files will be searched relative to this directory.
         Option:
         --{nameof(MainServiceOptions.SearchPattern)}=*
         Description:
         Target files matching this pattern will be rewritten.
         Option:
         --{nameof(MainServiceOptions.Recurse)}=[true|false]
         Description:
         Subdirectories will be searched if this is set to `true`.
         Option:
         --{nameof(MainServiceOptions.Watch)}=[true|false]
         Description:
         Runs continuously until shut down, rewriting found files on change.
         """);

    return;
}

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
            case HostMode.Rewrite:
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

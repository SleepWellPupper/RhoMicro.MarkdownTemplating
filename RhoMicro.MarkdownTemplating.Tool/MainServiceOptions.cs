// SPDX-License-Identifier: MPL-2.0

namespace RhoMicro.MarkdownTemplating;

internal sealed class MainServiceOptions
{
    public String WorkingDirectory { get; set; } = Environment.CurrentDirectory;
    public String SearchPattern { get; set; } = "*.md";
    public Boolean Recurse { get; set; }
    public Boolean Watch { get; set; }
}

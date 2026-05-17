# RhoMicro.MarkdownTemplating

This is a set of tools and libraries for authoring Markdown files from C#,
as well as supporting Markdown documentation of C# projects.

## Licensing

This work is licensed to you under the [MPL-2.0](https://spdx.org/licenses/MPL-2.0.html) license.

## Features

- populate sample C# code in Markdown documents with source code from C# files
- rewrite markdown files to include external C# source code

## Installation

### Class Library:

CLI:

```
dotnet add package RhoMicro.MarkdownTemplating
```

*Installing the templating library*

### Cli Tool

CLI:

```
dotnet tool install --global RhoMicro.MarkdownTemplating.Tool
```

*Installing the cli tool*

> **_Note_** the cli tool is primarily intended for use in CICD pipelines. As such,
> it does not ship with a convenient shorthand tool name.

## How To Use

### CLI tool

The `RhoMicro.MarkdownTemplating.Tool` cli application allows for rewriting of Markdown files
with source texts from external files.

It may be invoked in one of two modes: `Rewrite` or `Interactive`:
```
Always available:
Option:
--Mode=[Rewrite|Interactive]
Description:
Sets the execution mode. When set to `Rewrite`, target files will be rewritten according
to the options below. When set to `Interactive`, an interactive session is started that
allows for exploration of the C# syntax query language.

Available when in `Rewrite` mode:
Option:
--WorkingDirectory=*
Description:
Target files will be searched relative to this directory.
Option:
--SearchPattern=*
Description:
Target files matching this pattern will be rewritten.
Option:
--Recurse=[true|false]
Description:
Subdirectories will be searched if this is set to `true`.
Option:
--Watch=[true|false]
Description:
Runs continuously until shut down, rewriting found files on change.
```

The tool detects code sections that have a special reference link colocated below them:

````md
 ```cs
 text here will be replaced
 ```

[//]:(rmmdt://pathto/file.cs@MyClass.MyMethod)
````

The cli tool will pick up on annotated code sections and execute the following steps:

- locate the file at `pathto/file.cs` (relative to the Markdown file being rewritten)
- read and parse the code within using roslyn
- execute the `MyClass.MyMethod` query to select the output code
- replace the code section with the selected syntax nodes

The sample code below makes use of the cli tool.

#### Notes and restrictions:

The code section start ```` ```cs ```` must be the only text on that line.
Any text within the code section will be replaced.
The code section end ```` ``` ```` must be the only text on that line.

There may be any number of empty lines between the code section end and the link.

The link must be the only text on that line.

The link must follow the syntax exactly (no whitespace/newlines allowed):
`[//]:(rmmdt://{0}@{1})` or `[//]:(rmmdt://{0})`. `{0}` is the file path to import.
It is not optional. `{1}` is the query selector identifying the syntax node[s] to
import. It is optional. When provided, it must be separated from the path using
the `@` character.

Currently, only C# source text imports are supported.

### Library

Add templating services to your DI container:

```cs
services.AddMarkdownTemplating();
```

[//]:(rmmdt://RhoMicro.MarkdownTemplating.Tests/ReadmeTests.cs@ExtensionSample.3.*)

Resolve the rewriting façade to rewrite Markdown files.

```cs
var service = services.GetRequiredService<MarkdownRewriterService>();
await service.Rewrite("README.md", ct);
```

[//]:(rmmdt://RhoMicro.MarkdownTemplating.Tests/ReadmeTests.cs@ReadmeTests.ResolutionSample.3.*)

### C# Syntax Query

When rewriting C# code sections, the specific syntax nodes to output may be specified using a query selector syntax.

Selectors are separated with `.` characters.

Selectors are iteratively evaluated against all matched nodes of the previous selector.
The initial node is the `CompilationUnitSyntax` of the source file.

#### Self Selector

Empty selectors will match the current node (they are basically no-ops).

```
Class.cs@
Class.cs
```

*When omitting the selector from the link, a self selector is implicitly used, matching the
root `CompilationUnitSyntax` (essentially the entire file).*

```
Foo..Bar
Foo.Bar
```

*These queries are equivalent.*

#### Index Selector

Selectors that can be parsed to `Int32` will match the nth child of the current node.

Index selectors are zero-based.

```
Foo.2
```
*This query selects the third child of any node matching the `Foo` selector.*

#### Wildcard Selector

Wildcard selectors (`*`) will match all children of the current node.

```
Class.Method.3.*
```
*This query can be used to extract all statements from the `Method` method.*

#### Name Selector

Name selectors will match all descendants of the current node starting with the selector value.

```
Foo.B
```
*This query matches all nodes whose name starts with `B` from all nodes whose name starts with `Foo`.*

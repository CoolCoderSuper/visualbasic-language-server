# Description
This is a hacky Roslyn-based LSP server for Visual Basic, based on
[csharp-ls](https://github.com/razzmatazz/csharp-language-server).

`vb-ls` requires .NET 8 SDK to be installed. However it has been reported
to work with projects using older versions of dotnet SDK, including .NET Core 3,
.NET Framework 4.8 and possibly older ones too as it uses the standard
Roslyn/MSBuild libs that Visual Studio & omnisharp does.

# Acknowledgements
- vb-ls is not affiliated with Microsoft Corp;
- vb-ls uses LSP interface from [Ionide.LanguageServerProtocol](https://github.com/ionide/LanguageServerProtocol);
- vb-ls uses [Roslyn](https://github.com/dotnet/roslyn) to parse and update code; Roslyn maps really nicely to LSP w/relatively little impedance mismatch;
- vb-ls uses [ILSpy/ICSharpCode.Decompiler](https://github.com/icsharpcode/ILSpy) to decompile types in assemblies to C# source.

# Installation
TODO: Coming soon
`dotnet tool install --global vb-ls`

See [vb-ls nuget page](https://www.nuget.org/packages/vb-ls/)

# Settings

- `vb.solution` - solution to load, optional
- `vb.applyFormattingOptions` - use formatting options as supplied by the client (may override `.editorconfig` values), defaults to `false`

# Clients

`vb-ls` implements the standard LSP protocol to interact with your editor.
However there are some features that need a non-standard implementation and this
is where editor-specific plugins can be helpful.

# Outdated

## Visual Studio Code
### vytautassurvila/vscode-csharp-ls
- Supports code decompilation from metadata

See [csharp-ls](https://marketplace.visualstudio.com/items?itemName=vytautassurvila.csharp-ls) and [vscode-csharp-ls @ github](https://github.com/vytautassurvila/vscode-csharp-ls).

### statiolake/vscode-csharp-ls
See [vscode-csharp-ls](https://marketplace.visualstudio.com/items?itemName=statiolake.vscode-csharp-ls).

# Building

## On Linux/macOS

```
$ dotnet build
```

# FAQ

## decompile for your editor , with the example of neovim

### api

The api is "csharp/metadata", in neovim ,you can request it like

```lua
  local result, err = client.request_sync("csharp/metadata", params, 10000)
```

#### sender
You need to send a uri, it is like

**csharp:/metadata/projects/trainning2/assemblies/System.Console/symbols/System.Console.cs**

In neovim, it will be result(s) from vim.lsp.handles["textDocument/definition"]

and the key of uri is the key,

The key to send is like

```lua
local params = {
	timeout = 5000,
	textDocument = {
		uri = uri,
	}
}
```

The key of textDocument is needed. And timeout is just for neovim. It is the same if is expressed by json.

### receiver

The object received is like

```lua
{
	projectName = "csharp-test",
	assemblyName = "System.Runtime",
	symbolName = "System.String",
	source = "using System.Buffers;\n ...."
}
```

And In neovim, You receive the "result" above, you can get the decompile source from

```lua

local result, err = client.request_sync("csharp/metadata", params, 10000)
local source
if not err then
	source = result.result.source
end
```

And there is a plugin of neovim for you to decompile it.

[csharpls-extended-lsp.nvim](https://github.com/chen244/csharpls-extended-lsp.nvim)

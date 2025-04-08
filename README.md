# Description
This is a hacky Roslyn-based LSP server for Visual Basic, based on
[csharp-ls](https://github.com/razzmatazz/csharp-language-server).

`vb-ls` requires .NET 8 SDK to be installed. However it has been reported
to work with projects using older versions of dotnet SDK, including .NET Core 3,
.NET Framework 4.8 and possibly older ones too as it uses the standard
Roslyn/MSBuild libs that Visual Studio & omnisharp does.

See [CHANGELOG.md](CHANGELOG.md) for the list of recent improvements/fixes.

# Acknowledgements
- vb-ls is not affiliated with Microsoft Corp;
- vb-ls is based on the work [razzmatazz](https://github.com/razzmatazz) in [csharp-ls](https://github.com/razzmatazz/csharp-language-server)
- vb-ls uses LSP interface from [Ionide.LanguageServerProtocol](https://github.com/ionide/LanguageServerProtocol);
- vb-ls uses [Roslyn](https://github.com/dotnet/roslyn) to parse and update code; Roslyn maps really nicely to LSP w/relatively little impedance mismatch;
- vb-ls uses [ILSpy/ICSharpCode.Decompiler](https://github.com/icsharpcode/ILSpy) to decompile types in assemblies to C# source.

# Installation
`dotnet tool install --global vb-ls`

See [vb-ls nuget page](https://www.nuget.org/packages/vb-ls/)

# Settings

- `vb.solution` - solution to load, optional
- `vb.applyFormattingOptions` - use formatting options as supplied by the client (may override `.editorconfig` values), defaults to `false`

# Clients

`vb-ls` implements the standard LSP protocol to interact with your editor.
However there are some features that need a non-standard implementation and this
is where editor-specific plugins can be helpful.

## NeoVim

NeoVim 0.11+ can easily be configured using the `vim.lsp.config` api.
```lua
vim.lsp.config['vb_ls'] = {
    cmd = { 'vb-ls' },
    root_markers = { '*.sln', '*.vbproj' },
    filetypes = { 'vbnet' },
    init_options = {
      AutomaticWorkspaceInit = true,
    },
}

vim.lsp.enable('vb_ls')
```
You will also have to install the [vbnet.nvim](https://github.com/CoolCoderSuper/vbnet.nvim) plugin to register the
vbnet filetype and improved syntax highlighting.

## Visual Studio Code
See [vscode-vb-ls](https://marketplace.visualstudio.com/items?itemName=CoolCoderSuper.vscode-vb-ls).

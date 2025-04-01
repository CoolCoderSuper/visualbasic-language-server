namespace VisualBasicLanguageServer.Handlers

open Ionide.LanguageServerProtocol.Types
open Ionide.LanguageServerProtocol.JsonRpc

open VisualBasicLanguageServer.State

[<RequireQualifiedAccess>]
module InlineValue =
    let provider (clientCapabilities: ClientCapabilities) : InlineValueOptions option = None

    let registration (clientCapabilities: ClientCapabilities) : Registration option = None

    let handle (context: ServerRequestContext) (p: InlineValueParams) : AsyncLspResult<InlineValue[] option> =
        LspResult.notImplemented<InlineValue[] option> |> async.Return

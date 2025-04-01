namespace VisualBasicLanguageServer.Handlers

open Ionide.LanguageServerProtocol.Types
open Ionide.LanguageServerProtocol.JsonRpc

open VisualBasicLanguageServer.State

[<RequireQualifiedAccess>]
module Color =
    let provider (clientCapabilities: ClientCapabilities)  = None

    let registration (clientCapabilities: ClientCapabilities) : Registration option = None

    let handle (context: ServerRequestContext) (p: DocumentColorParams) : AsyncLspResult<ColorInformation[]> =
        LspResult.notImplemented<ColorInformation[]> |> async.Return

    let present (context: ServerRequestContext) (p: ColorPresentationParams) : AsyncLspResult<ColorPresentation[]> =
        LspResult.notImplemented<ColorPresentation[]> |> async.Return

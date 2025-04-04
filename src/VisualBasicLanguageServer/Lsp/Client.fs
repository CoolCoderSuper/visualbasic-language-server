namespace VisualBasicLanguageServer.Lsp

open Ionide.LanguageServerProtocol
open Ionide.LanguageServerProtocol.Server
open Ionide.LanguageServerProtocol.Types

type VisualBasicLspClient(sendServerNotification: ClientNotificationSender, sendServerRequest: ClientRequestSender) =
    inherit LspClient()

    override __.WindowShowMessage(p) =
        sendServerNotification "window/showMessage" (box p) |> Async.Ignore

    // TODO: Send notifications / requests to client only if client support it

    override __.WindowShowMessageRequest(p) =
        sendServerRequest.Send "window/showMessageRequest" (box p)

    override __.WindowLogMessage(p) =
        sendServerNotification "window/logMessage" (box p) |> Async.Ignore

    override __.TelemetryEvent(p) =
        sendServerNotification "telemetry/event" (box p) |> Async.Ignore

    override __.ClientRegisterCapability(p) =
        sendServerRequest.Send "client/registerCapability" (box p)

    override __.ClientUnregisterCapability(p) =
        sendServerRequest.Send "client/unregisterCapability" (box p)

    override __.WorkspaceWorkspaceFolders() =
        sendServerRequest.Send "workspace/workspaceFolders" ()

    override __.WorkspaceConfiguration(p) =
        sendServerRequest.Send "workspace/configuration" (box p)

    override __.WorkspaceApplyEdit(p) =
        sendServerRequest.Send "workspace/applyEdit" (box p)

    override __.WorkspaceSemanticTokensRefresh() =
        sendServerNotification "workspace/semanticTokens/refresh" () |> Async.Ignore

    override __.TextDocumentPublishDiagnostics(p) =
        sendServerNotification "textDocument/publishDiagnostics" (box p) |> Async.Ignore

    override __.WorkDoneProgressCreate(token) =
        let param: WorkDoneProgressCreateParams = { Token = token }
        sendServerRequest.Send "window/workDoneProgress/create" (box param)

    override __.Progress(token, data) =
        let jtokenFromObject (obj: 'a) =
            Newtonsoft.Json.Linq.JToken.FromObject(obj, Ionide.LanguageServerProtocol.Server.jsonRpcFormatter.JsonSerializer)

        let progress: ProgressParams =
            { Token = token
              Value = jtokenFromObject data }

        sendServerNotification "$/progress" (box progress) |> Async.Ignore

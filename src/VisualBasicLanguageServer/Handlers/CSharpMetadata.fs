namespace VisualBasicLanguageServer.Handlers

open Ionide.LanguageServerProtocol.Types
open Ionide.LanguageServerProtocol.JsonRpc

open VisualBasicLanguageServer.Types
open VisualBasicLanguageServer.State

[<RequireQualifiedAccess>]
module CSharpMetadata =
    let handle (context: ServerRequestContext) (metadataParams: CSharpMetadataParams): AsyncLspResult<CSharpMetadataResponse option> = async {
        let uri = metadataParams.TextDocument.Uri

        let metadataMaybe =
            context.DecompiledMetadata
            |> Map.tryFind uri
            |> Option.map (fun x -> x.Metadata)

        return metadataMaybe |> LspResult.success
    }

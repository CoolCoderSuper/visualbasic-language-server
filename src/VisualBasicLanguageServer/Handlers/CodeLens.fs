namespace VisualBasicLanguageServer.Handlers

open System

open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.VisualBasic
open Microsoft.CodeAnalysis.VisualBasic.Syntax
open Microsoft.CodeAnalysis.Text
open Ionide.LanguageServerProtocol.Server
open Ionide.LanguageServerProtocol.Types
open Ionide.LanguageServerProtocol.JsonRpc

open VisualBasicLanguageServer.State
open VisualBasicLanguageServer.Conversions
open VisualBasicLanguageServer.Types

type private DocumentSymbolCollectorForCodeLens(semanticModel: SemanticModel) =
    inherit VisualBasicSyntaxWalker(SyntaxWalkerDepth.Token)

    let mutable collectedSymbols = []

    let collect (node: SyntaxNode) (nameSpan: TextSpan) =
        match semanticModel.GetDeclaredSymbol(node) |> Option.ofObj with
        | Some symbol ->
            collectedSymbols <- (symbol, nameSpan) :: collectedSymbols
        | _ -> ()

    member __.GetSymbols() =
        collectedSymbols |> List.rev |> Array.ofList

    override __.VisitEnumStatement(node) =
        collect node node.Identifier.Span
        base.VisitEnumStatement(node)

    override __.VisitEnumMemberDeclaration(node) =
        collect node node.Identifier.Span

    override __.VisitClassStatement(node) =
        collect node node.Identifier.Span
        base.VisitClassStatement(node)

    override __.VisitStructureStatement(node) =
        collect node node.Identifier.Span
        base.VisitStructureStatement(node)

    override __.VisitInterfaceStatement(node) =
        collect node node.Identifier.Span
        base.VisitInterfaceStatement(node)

    override __.VisitDelegateStatement(node) =
        collect node node.Identifier.Span

    override __.VisitConstructorBlock(node) =
        collect node node.SubNewStatement.Span

    override __.VisitOperatorStatement(node) =
        collect node node.OperatorToken.Span

    override __.VisitMethodStatement(node) =
        collect node node.Identifier.Span

    override __.VisitPropertyStatement(node) =
        collect node node.Identifier.Span

    override __.VisitVariableDeclarator(node) =
        //let grandparent =
        //    node.Parent
        //   |> Option.ofObj
        //    |> Option.bind (fun node -> node.Parent |> Option.ofObj)
        // Only show field variables and ignore local variables
        //if grandparent.IsSome && grandparent.Value :? FieldDeclarationSyntax then
        //    collect node node.Identifier.Span
        ()//todo: shit

    override __.VisitEventStatement(node) =
        collect node node.Identifier.Span

[<RequireQualifiedAccess>]
module CodeLens =
    type CodeLensData =
        { DocumentUri: string
          Position: Position }
        static member Default =
            { DocumentUri = ""
              Position = { Line = 0u; Character = 0u } }

    let private dynamicRegistration (clientCapabilities: ClientCapabilities) =
        clientCapabilities.TextDocument
        |> Option.bind (fun x -> x.CodeLens)
        |> Option.bind (fun x -> x.DynamicRegistration)
        |> Option.defaultValue false

    let provider (clientCapabilities: ClientCapabilities) : CodeLensOptions option =
        match dynamicRegistration clientCapabilities with
        | true -> None
        | false -> Some { ResolveProvider = Some true
                          WorkDoneProgress = None }

    let registration (clientCapabilities: ClientCapabilities) : Registration option =
        match dynamicRegistration clientCapabilities with
        | false -> None
        | true ->
            let registerOptions: CodeLensRegistrationOptions =
                { ResolveProvider = Some true
                  WorkDoneProgress = None
                  DocumentSelector = Some defaultDocumentSelector }

            Some
                { Id = Guid.NewGuid().ToString()
                  Method = "textDocument/codeLens"
                  RegisterOptions = registerOptions |> serialize |> Some }

    let handle (context: ServerRequestContext) (p: CodeLensParams): AsyncLspResult<CodeLens[] option> = async {
        let docMaybe = context.GetDocument p.TextDocument.Uri
        match docMaybe with
        | None ->
            return None |> LspResult.success
        | Some doc ->
            let! ct = Async.CancellationToken
            let! semanticModel = doc.GetSemanticModelAsync(ct) |> Async.AwaitTask
            let! syntaxTree = doc.GetSyntaxTreeAsync(ct) |> Async.AwaitTask
            let! docText = doc.GetTextAsync(ct) |> Async.AwaitTask

            let collector = DocumentSymbolCollectorForCodeLens(semanticModel)
            let! root = syntaxTree.GetRootAsync(ct) |> Async.AwaitTask
            collector.Visit(root)

            let makeCodeLens (_symbol: ISymbol, nameSpan: TextSpan) : CodeLens =
                let start = nameSpan.Start |> docText.Lines.GetLinePosition

                let lensData: CodeLensData =
                    { DocumentUri = p.TextDocument.Uri
                      Position = start |> Position.fromLinePosition }

                { Range = nameSpan |> Range.fromTextSpan docText.Lines
                  Command = None
                  Data = lensData |> serialize |> Some }

            let codeLens = collector.GetSymbols() |> Seq.map makeCodeLens

            return codeLens |> Array.ofSeq |> Some |> LspResult.success
    }

    let resolve (context: ServerRequestContext)
                (p: CodeLens)
            : AsyncLspResult<CodeLens> = async {

        let lensData: CodeLensData =
            p.Data
            |> Option.map _.ToObject<CodeLensData>()
            |> Option.bind Option.ofObj
            |> Option.defaultValue CodeLensData.Default

        match! context.FindSymbol lensData.DocumentUri lensData.Position with
        | None ->
            return p |> LspResult.success
        | Some symbol ->
            let! locations = context.FindReferences symbol false
            // FIXME: refNum is wrong. There are lots of false positive even if we distinct locations by
            // (l.SourceTree.FilePath, l.SourceSpan)
            let refNum =
                locations
                |> Seq.distinctBy (fun l -> (l.GetMappedLineSpan().Path, l.SourceSpan))
                |> Seq.length

            let title = sprintf "%d Reference(s)" refNum

            let arg: ReferenceParams =
                { TextDocument = { Uri = lensData.DocumentUri }
                  Position = lensData.Position
                  WorkDoneToken = None
                  PartialResultToken = None
                  Context = { IncludeDeclaration = true } }
            let command =
                { Title = title
                  Command = "textDocument/references"
                  Arguments = Some [| arg |> serialize |] }

            return { p with Command = Some command } |> LspResult.success
    }

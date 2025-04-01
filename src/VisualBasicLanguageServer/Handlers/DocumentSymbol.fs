namespace VisualBasicLanguageServer.Handlers

open System

open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.Text
open Microsoft.CodeAnalysis.VisualBasic
open Microsoft.CodeAnalysis.VisualBasic.Syntax
open Ionide.LanguageServerProtocol.Server
open Ionide.LanguageServerProtocol.Types
open Ionide.LanguageServerProtocol.JsonRpc

open VisualBasicLanguageServer.State
open VisualBasicLanguageServer.Conversions
open VisualBasicLanguageServer.Types

[<RequireQualifiedAccess>]
module DocumentSymbol =
    open VisualBasicLanguageServer.Util
    let private formatSymbol (sym: ISymbol)
                     showAttributes
                     (semanticModelMaybe: SemanticModel option)
                     (posMaybe: int option) =
        match showAttributes, semanticModelMaybe, posMaybe with
        | true, Some semanticModel, Some pos -> sym.ToMinimalDisplayString(semanticModel, pos)
        | true, _, _ -> sym.ToDisplayString()
        | false, _, _ -> sym.Name

    let private getSymbolNameAndKind
            (semanticModel: SemanticModel option)
            (pos: int option)
            (symbol: ISymbol) =
        let showAttributes = true

        match symbol with
        | :? ILocalSymbol as ls ->
            (formatSymbol ls showAttributes semanticModel pos,
                SymbolKind.Variable)

        | :? IFieldSymbol as fs ->
            (formatSymbol fs showAttributes semanticModel pos,
                SymbolKind.Field)

        | :? IPropertySymbol as ps ->
            (formatSymbol ps showAttributes semanticModel pos,
                SymbolKind.Property)

        | :? IMethodSymbol as ms ->
            (formatSymbol ms showAttributes semanticModel pos,
                match ms.MethodKind with
                | MethodKind.Constructor -> SymbolKind.Constructor
                | MethodKind.StaticConstructor -> SymbolKind.Constructor
                | MethodKind.BuiltinOperator -> SymbolKind.Operator
                | MethodKind.UserDefinedOperator -> SymbolKind.Operator
                | MethodKind.Conversion -> SymbolKind.Operator
                | _ -> SymbolKind.Method)

        | :? ITypeSymbol as ts ->
            (formatSymbol ts showAttributes semanticModel pos,
                match ts.TypeKind with
                | TypeKind.Class -> SymbolKind.Class
                | TypeKind.Enum -> SymbolKind.Enum
                | TypeKind.Struct -> SymbolKind.Struct
                | TypeKind.Interface -> SymbolKind.Interface
                | TypeKind.Delegate -> SymbolKind.Class
                | TypeKind.Array -> SymbolKind.Array
                | TypeKind.TypeParameter -> SymbolKind.TypeParameter
                | _ -> SymbolKind.Class)

        | :? IEventSymbol as es ->
            (formatSymbol es showAttributes semanticModel pos,
                SymbolKind.Event)

        | :? INamespaceSymbol as ns ->

            (formatSymbol ns showAttributes semanticModel pos,
                SymbolKind.Namespace)

        | _ ->
            (symbol.ToString(), SymbolKind.File)

    let rec private flattenDocumentSymbol (node: DocumentSymbol) =
        let nodeWithNoChildren =
            { node with Children = None }

        let flattenedChildren =
            match node.Children with
            | None -> []
            | Some xs -> xs |> Seq.map flattenDocumentSymbol |> Seq.concat |> List.ofSeq

        nodeWithNoChildren :: flattenedChildren

    type private DocumentSymbolCollector (docText: SourceText, semanticModel: SemanticModel) =
        inherit VisualBasicSyntaxWalker(SyntaxWalkerDepth.Token)

        let mutable symbolStack = []

        let push (node: SyntaxNode) (nameSpan: TextSpan) =
            let symbol = semanticModel.GetDeclaredSymbol(node) |> nonNull "semanticModel.GetDeclaredSymbol(node)"

            let (fullSymbolName, symbolKind) =
                getSymbolNameAndKind (Some semanticModel)
                                     (Some nameSpan.Start)
                                     symbol

            let lspRange = Range.fromTextSpan docText.Lines node.FullSpan

            let selectionLspRange = Range.fromTextSpan docText.Lines nameSpan

            let symbolDetail =
                match symbolKind with
                | SymbolKind.Class -> None
                | SymbolKind.Struct -> None
                | _ -> Some fullSymbolName

            let displayStyle = SymbolDisplayFormat(
                typeQualificationStyle = SymbolDisplayTypeQualificationStyle.NameOnly,
                genericsOptions = SymbolDisplayGenericsOptions.IncludeTypeParameters,
                memberOptions = (SymbolDisplayMemberOptions.IncludeParameters ||| SymbolDisplayMemberOptions.IncludeExplicitInterface),
                parameterOptions = (SymbolDisplayParameterOptions.IncludeParamsRefOut ||| SymbolDisplayParameterOptions.IncludeExtensionThis ||| SymbolDisplayParameterOptions.IncludeType ||| SymbolDisplayParameterOptions.IncludeName ||| SymbolDisplayParameterOptions.IncludeDefaultValue),
                miscellaneousOptions = SymbolDisplayMiscellaneousOptions.UseSpecialTypes)

            let docSymbol = {
                Name           = SymbolName.fromSymbol displayStyle symbol
                Detail         = symbolDetail
                Kind           = symbolKind
                Range          = lspRange
                SelectionRange = selectionLspRange
                Children       = None
                Tags           = None
                Deprecated     = None
            }

            symbolStack <- docSymbol :: symbolStack

        let pop (_node: SyntaxNode) =
            let symbolStack' =
                match symbolStack with
                | [] -> failwith "symbolStack is empty"
                | [_] -> []
                | top :: restPastTop ->
                    match restPastTop with
                    | [] -> failwith "restPastTop is empty"
                    | parent :: restPastParent ->
                        let parentWithTopAsChild =
                            let newChildren =
                                parent.Children
                                |> Option.defaultValue Array.empty
                                |> List.ofSeq
                                |> fun xs -> xs @ [top]
                                |> Array.ofSeq

                            { parent with Children = Some newChildren }

                        let poppedSymbolStack = parentWithTopAsChild :: restPastParent

                        poppedSymbolStack

            symbolStack <- symbolStack'

        member __.Init(moduleName: string) =
            let emptyRange = { Start={ Line=0u; Character=0u }
                               End={ Line=0u; Character=0u } }

            let root: DocumentSymbol = {
                Name           = moduleName
                Detail         = None
                Kind           = SymbolKind.File
                Range          = emptyRange
                SelectionRange = emptyRange
                Children       = None
                Tags           = None
                Deprecated     = None
            }

            symbolStack <- [root]

        member __.GetDocumentSymbols (clientSupportsDocSymbolHierarchy: bool) =
            let root =
                match symbolStack with
                | [root] -> root
                | _ -> Exception("symbolStack is not a single node") |> raise

            if clientSupportsDocSymbolHierarchy then
                [| root |]
            else
                root |> flattenDocumentSymbol |> Array.ofSeq

        override __.VisitNamespaceStatement(node) =
            push node node.Name.Span
            base.VisitNamespaceStatement(node)
            pop node

        override __.VisitEnumStatement(node) =
            push node node.Identifier.Span
            base.VisitEnumStatement(node)
            pop node

        override __.VisitEnumMemberDeclaration(node) =
            push node node.Identifier.Span
            base.VisitEnumMemberDeclaration(node)
            pop node

        override __.VisitClassStatement(node) =
            push node node.Identifier.Span
            base.VisitClassStatement(node)
            pop node

        override __.VisitStructureStatement(node) =
            push node node.Identifier.Span
            base.VisitStructureStatement(node)
            pop node

        override __.VisitInterfaceStatement(node) =
            push node node.Identifier.Span
            base.VisitInterfaceStatement(node)
            pop node

        override __.VisitDelegateStatement(node) =
            push node node.Identifier.Span
            base.VisitDelegateStatement(node)
            pop node

        override __.VisitConstructorBlock(node) =
            push node node.SubNewStatement.Span
            base.VisitConstructorBlock(node)
            pop node.SubNewStatement

        override __.VisitOperatorStatement(node) =
            push node node.OperatorToken.Span
            base.VisitOperatorStatement(node)
            pop node

        override __.VisitMethodStatement(node) =
            push node node.Identifier.Span
            base.VisitMethodStatement(node)
            pop node

        override __.VisitPropertyStatement(node) =
            push node node.Identifier.Span
            base.VisitPropertyStatement(node)
            pop node

        override __.VisitVariableDeclarator(node) =
            //let grandparent =
            //    node.Parent |> Option.ofObj
            //    |> Option.bind (fun node -> node.Parent |> Option.ofObj)
            // Only show field variables and ignore local variables
            //if grandparent.IsSome && grandparent.Value :? FieldDeclarationSyntax then
            //    push node node.Identifier.Span
            //    base.VisitVariableDeclarator(node)
            //    pop node
            //else
            //    base.VisitVariableDeclarator(node)
            base.VisitVariableDeclarator(node)//TODO: fix this

        override __.VisitEventStatement(node) =
            push node node.Identifier.Span
            base.VisitEventStatement(node)
            pop node

    let private dynamicRegistration (clientCapabilities: ClientCapabilities) =
        clientCapabilities.TextDocument
        |> Option.bind (fun x -> x.DocumentSymbol)
        |> Option.bind (fun x -> x.DynamicRegistration)
        |> Option.defaultValue false

    let provider (clientCapabilities: ClientCapabilities) : U2<bool, DocumentSymbolOptions> option =
        match dynamicRegistration clientCapabilities with
        | true -> None
        | false -> true |> U2.C1 |> Some

    let registration (clientCapabilities: ClientCapabilities) : Registration option =
        match dynamicRegistration clientCapabilities with
        | false -> None
        | true ->
            let registerOptions: DocumentSymbolRegistrationOptions =
                { Label = None
                  WorkDoneProgress = None
                  DocumentSelector = Some defaultDocumentSelector }

            Some
                { Id = Guid.NewGuid().ToString()
                  Method = "textDocument/documentSymbol"
                  RegisterOptions = registerOptions |> serialize |> Some }

    let handle (context: ServerRequestContext)
               (p: DocumentSymbolParams)
            : AsyncLspResult<U2<SymbolInformation[], DocumentSymbol[]> option> = async {
        let canEmitDocSymbolHierarchy =
            context.ClientCapabilities.TextDocument
            |> Option.bind (fun cc -> cc.DocumentSymbol)
            |> Option.bind (fun cc -> cc.HierarchicalDocumentSymbolSupport)
            |> Option.defaultValue false

        match context.GetDocument p.TextDocument.Uri with
        | None -> return None |> LspResult.success
        | Some doc ->
            let! ct = Async.CancellationToken
            let! semanticModel = doc.GetSemanticModelAsync(ct) |> Async.AwaitTask
            let! docText = doc.GetTextAsync(ct) |> Async.AwaitTask
            let! syntaxTree = doc.GetSyntaxTreeAsync(ct) |> Async.AwaitTask

            let collector = DocumentSymbolCollector(docText, semanticModel)
            collector.Init(doc.Name)

            let! root = syntaxTree.GetRootAsync(ct) |> Async.AwaitTask
            collector.Visit(root)

            return collector.GetDocumentSymbols(canEmitDocSymbolHierarchy)
                   |> U2.C2
                   |> Some
                   |> LspResult.success
    }

module VisualBasicLanguageServer.Tests.DiagnosticTests

open System.Threading

open NUnit.Framework
open Ionide.LanguageServerProtocol.Types

open VisualBasicLanguageServer.Tests.Tooling

[<TestCase>]
let testPushDiagnosticsWork () =
    use client = setupServerClient defaultClientProfile
                                   "TestData/testPushDiagnosticsWork"
    client.StartAndWaitForSolutionLoad()

    //
    // open Class.cs file and wait for diagnostics to be pushed
    //
    use classFile = client.Open("Project/Class.cs")

    Thread.Sleep(4000)

    let state = client.GetState()
    let version0, diagnosticList0 = state.PushDiagnostics |> Map.find classFile.Uri

    Assert.AreEqual(None, version0)

    Assert.AreEqual(3, diagnosticList0.Length)

    let diagnostic0 = diagnosticList0.[0]
    Assert.AreEqual("Identifier expected", diagnostic0.Message)
    Assert.AreEqual(Some DiagnosticSeverity.Error, diagnostic0.Severity)
    Assert.AreEqual(0, diagnostic0.Range.Start.Line)
    Assert.AreEqual(3, diagnostic0.Range.Start.Character)

    let diagnostic1 = diagnosticList0.[1]
    Assert.AreEqual("; expected", diagnostic1.Message)

    let diagnostic2 = diagnosticList0.[2]
    Assert.AreEqual(
      "The type or namespace name 'XXX' could not be found (are you missing a using directive or an assembly reference?)",
      diagnostic2.Message)

    //
    // now change the file to contain no content (and thus no diagnostics)
    //
    classFile.DidChange("")

    Thread.Sleep(4000)

    let state = client.GetState()
    let version1, diagnosticList1 = state.PushDiagnostics |> Map.find classFile.Uri

    Assert.AreEqual(None, version1)

    Assert.AreEqual(0, diagnosticList1.Length)
    ()


[<TestCase>]
let testPullDiagnosticsWork () =
    use client = setupServerClient defaultClientProfile
                                   "TestData/testPullDiagnosticsWork"
    client.StartAndWaitForSolutionLoad()

    //
    // open Class.cs file and pull diagnostics
    //
    use classFile = client.Open("Project/Class.cs")

    let diagnosticParams: DocumentDiagnosticParams =
        { WorkDoneToken = None
          PartialResultToken = None
          TextDocument = { Uri = classFile.Uri }
          Identifier = None
          PreviousResultId = None }

    let report0: DocumentDiagnosticReport option =
        client.Request("textDocument/diagnostic", diagnosticParams)

    match report0 with
    | Some (U2.C1 report) ->
        Assert.AreEqual("full", report.Kind)
        Assert.AreEqual(None, report.ResultId)
        Assert.AreEqual(3, report.Items.Length)

        let diagnostic0 = report.Items.[0]
        Assert.AreEqual(0, diagnostic0.Range.Start.Line)
        Assert.AreEqual(3, diagnostic0.Range.Start.Character)
        Assert.AreEqual(Some DiagnosticSeverity.Error, diagnostic0.Severity)
        Assert.AreEqual("Identifier expected", diagnostic0.Message)
        Assert.AreEqual(
            "https://msdn.microsoft.com/query/roslyn.query?appId=roslyn&k=k(CS1001)",
            diagnostic0.CodeDescription.Value.Href)

        let diagnostic1 = report.Items.[1]
        Assert.AreEqual("; expected", diagnostic1.Message)

        let diagnostic2 = report.Items.[2]
        Assert.AreEqual(
        "The type or namespace name 'XXX' could not be found (are you missing a using directive or an assembly reference?)",
        diagnostic2.Message)
    | _ -> failwith "U2.C1 is expected"

    //
    // now try to do the same but with file fixed to contain no content (and thus no diagnostics)
    //
    classFile.DidChange("")

    let report1: DocumentDiagnosticReport option =
        client.Request("textDocument/diagnostic", diagnosticParams)

    match report1 with
    | Some (U2.C1 report) ->
        Assert.AreEqual("full", report.Kind)
        Assert.AreEqual(None, report.ResultId)
        Assert.AreEqual(0, report.Items.Length)
    | _ -> failwith "U2.C1 is expected"
    ()

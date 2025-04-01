namespace VisualBasicLanguageServer.Handlers

open System
open System.IO
open System.Reflection

open Microsoft.Build.Locator
open Ionide.LanguageServerProtocol
open Ionide.LanguageServerProtocol.Types
open Ionide.LanguageServerProtocol.Server
open Ionide.LanguageServerProtocol.JsonRpc

open VisualBasicLanguageServer.State
open VisualBasicLanguageServer.State.ServerState
open VisualBasicLanguageServer.Types
open VisualBasicLanguageServer.Logging

[<RequireQualifiedAccess>]
module Initialization =
    let private logger = LogProvider.getLoggerByName "Initialization"

    let handleInitialize (lspClient: ILspClient)
                         (setupTimer: unit -> unit)
                         (serverCapabilities: ServerCapabilities)
                         (context: ServerRequestContext)
                         (p: InitializeParams)
            : Async<LspResult<InitializeResult>> = async {
        // context.State.LspClient has not been initialized yet thus context.WindowShowMessage will not work
        let windowShowMessage m = lspClient.WindowLogMessage({ Type = MessageType.Info; Message = m })

        context.Emit(ClientChange (Some lspClient))

        let serverName = "vb-ls"
        let serverVersion = Assembly.GetExecutingAssembly().GetName().Version |> string
        logger.info (
            Log.setMessage "initializing, {name} version {version}"
            >> Log.addContext "name" serverName
            >> Log.addContext "version" serverVersion
        )

        do! windowShowMessage(
            sprintf "vb-ls: initializing, version %s" serverVersion)

        logger.info (
            Log.setMessage "{name} is released under MIT license and is not affiliated with Microsoft Corp.; see https://github.com/CoolCoderSuper/visualbasic-language-server"
            >> Log.addContext "name" serverName
        )

        do! windowShowMessage(
            sprintf "vb-ls: %s is released under MIT license and is not affiliated with Microsoft Corp.; see https://github.com/CoolCoderSuper/visualbasic-language-server" serverName)

        let vsInstanceQueryOpt = VisualStudioInstanceQueryOptions.Default
        let vsInstanceList = MSBuildLocator.QueryVisualStudioInstances(vsInstanceQueryOpt)
        if Seq.isEmpty vsInstanceList then
            raise (InvalidOperationException("No instances of MSBuild could be detected." + Environment.NewLine + "Try calling RegisterInstance or RegisterMSBuildPath to manually register one."))

        // do! infoMessage "MSBuildLocator instances found:"
        //
        // for vsInstance in vsInstanceList do
        //     do! infoMessage (sprintf "- SDK=\"%s\", Version=%s, MSBuildPath=\"%s\", DiscoveryType=%s"
        //                              vsInstance.Name
        //                              (string vsInstance.Version)
        //                              vsInstance.MSBuildPath
        //                              (string vsInstance.DiscoveryType))

        let vsInstance = vsInstanceList |> Seq.head

        logger.info(
            Log.setMessage "MSBuildLocator: will register \"{vsInstanceName}\", Version={vsInstanceVersion} as default instance"
            >> Log.addContext "vsInstanceName" vsInstance.Name
            >> Log.addContext "vsInstanceVersion" (string vsInstance.Version)
        )

        MSBuildLocator.RegisterInstance(vsInstance)

(*
        logger.trace (
            Log.setMessage "handleInitialize: p.Capabilities={caps}"
            >> Log.addContext "caps" (serialize p.Capabilities)
        )
*)
        context.Emit(ClientCapabilityChange p.Capabilities)

        // TODO use p.RootUri
        let rootPath = Directory.GetCurrentDirectory()
        context.Emit(RootPathChange rootPath)

        // setup timer so actors get period ticks
        setupTimer()

        let initializeResult =
            { InitializeResult.Default with
                    Capabilities = serverCapabilities
                    ServerInfo =
                      Some
                        { Name = "vb-ls"
                          Version = Some (Assembly.GetExecutingAssembly().GetName().Version.ToString()) }}

        return initializeResult |> LspResult.success
    }

    let handleInitialized (lspClient: ILspClient)
                          (stateActor: MailboxProcessor<ServerStateEvent>)
                          (getRegistrations: ClientCapabilities -> Registration list)
                          (context: ServerRequestContext)
                          (_p: unit)
            : Async<LspResult<unit>> =
        async {
            logger.trace (
                Log.setMessage "handleInitialized: \"initialized\" notification received from client"
            )

            let registrationParams = { Registrations = getRegistrations context.ClientCapabilities |> List.toArray }

            // TODO: Retry on error?
            try
                match! lspClient.ClientRegisterCapability registrationParams with
                | Ok _ -> ()
                | Error error ->
                    logger.warn(
                        Log.setMessage "handleInitialized: didChangeWatchedFiles registration has failed with {error}"
                        >> Log.addContext "error" (string error)
                    )
            with
            | ex ->
                logger.warn(
                    Log.setMessage "handleInitialized: didChangeWatchedFiles registration has failed with {error}"
                    >> Log.addContext "error" (string ex)
                )

            //
            // retrieve vb settings
            //
            try
                let! workspaceCSharpConfig =
                    lspClient.WorkspaceConfiguration(
                        { Items=[| { Section=Some "vb"; ScopeUri=None } |] })

                let vbConfigTokensMaybe =
                    match workspaceCSharpConfig with
                    | Ok ts -> Some ts
                    | _ -> None

                let newSettingsMaybe =
                  match vbConfigTokensMaybe with
                  | Some [| t |] ->
                      let vbSettingsMaybe = t |> deserialize<ServerSettingsVisualBasicDto option>

                      match vbSettingsMaybe with
                      | Some vbSettings ->

                          match vbSettings.solution with
                          | Some solutionPath-> Some { context.State.Settings with SolutionPath = Some solutionPath }
                          | _ -> None

                      | _ -> None
                  | _ -> None

                // do! logMessage (sprintf "handleInitialized: newSettingsMaybe=%s" (string newSettingsMaybe))

                match newSettingsMaybe with
                | Some newSettings ->
                    context.Emit(SettingsChange newSettings)
                | _ -> ()
            with
            | ex ->
                logger.warn(
                    Log.setMessage "handleInitialized: could not retrieve `vb` workspace configuration section: {error}"
                    >> Log.addContext "error" (ex |> string)
                )

            //
            // start loading the solution
            //
            stateActor.Post(SolutionReloadRequest (TimeSpan.FromMilliseconds(100)))

            logger.trace(
                Log.setMessage "handleInitialized: OK")

            return Ok()
        }

    let handleShutdown (context: ServerRequestContext) (_: unit) : Async<LspResult<unit>> = async {
        context.Emit(ClientCapabilityChange emptyClientCapabilities)
        context.Emit(ClientChange None)
        return Ok()
    }

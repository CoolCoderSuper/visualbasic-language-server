import { ExtensionContext, workspace } from "vscode";
import {
  LanguageClient,
  LanguageClientOptions,
  ServerOptions
} from "vscode-languageclient/node";

let client: LanguageClient;

export function activate(context: ExtensionContext) {
  const config = workspace.getConfiguration("vb-ls");
  const serverPath = config.get<string>("server.path")!;
  const solutionPath = config.get<string>("solution.path");

  const args = [];
  if (solutionPath) {
    args.push("-s");
    args.push(solutionPath);
  }

  const serverOptions: ServerOptions = {
    command: serverPath,
    args
  };

  const clientOptions: LanguageClientOptions = {
    documentSelector: [{ scheme: "file", language: "vbnet" }]
  };

  client = new LanguageClient(
    "vb-ls",
    "Visual Basic Language Server",
    serverOptions,
    clientOptions
  );

  client.start();
}

export function deactivate(): Thenable<void> | undefined {
  return client?.stop();
}

"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.deactivate = exports.activate = void 0;
const vscode_1 = require("vscode");
const node_1 = require("vscode-languageclient/node");
let client;
function activate(context) {
    const config = vscode_1.workspace.getConfiguration("vb-ls");
    const serverPath = config.get("server.path");
    const solutionPath = config.get("solution.path");
    const args = [];
    if (solutionPath) {
        args.push("-s");
        args.push(solutionPath);
    }
    const serverOptions = {
        command: serverPath,
        args
    };
    const clientOptions = {
        documentSelector: [{ scheme: "file", language: "vbnet" }]
    };
    client = new node_1.LanguageClient("vb-ls", "Visual Basic Language Server", serverOptions, clientOptions);
    client.start();
}
exports.activate = activate;
function deactivate() {
    return client?.stop();
}
exports.deactivate = deactivate;
//# sourceMappingURL=extension.js.map
using System.CommandLine;

namespace RiptideEditor;

internal static class Application {
    private static async Task<int> Main(string[] args) {
        var cmd = SetupCommands();

        return await cmd.InvokeAsync(args);
    }

    private static RootCommand SetupCommands() {
        Argument<string> arg = new("Path", "Path of project to open.");
        var root = new RootCommand() {
            arg,
        };
        root.SetHandler(path => {
            if (!Directory.Exists(path)) {
                throw new DirectoryNotFoundException($"Project directory '{path}' does not exist.");
            }

            EditorApplication.Initialize(path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar));
        }, arg);

        return root;
    }
}
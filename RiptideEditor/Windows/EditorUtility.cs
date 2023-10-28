namespace RiptideEditor.Windows;

public static class EditorUtility {
    public static void ShowInExplorer(string path) {
        if (OperatingSystem.IsWindows()) {
            Process.Start("explorer.exe", path);
            return;
        }

        throw new NotImplementedException($"EditorUtility.ShowInExplorer is unimplemented for operating system '{RuntimeInformation.OSDescription}'.");
    }
}
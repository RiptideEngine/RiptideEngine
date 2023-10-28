using System.Reflection;
using System.Runtime.Loader;

namespace RiptidePlayer;

internal class GameAssemblyLoader : AssemblyLoadContext {
    private readonly AssemblyDependencyResolver _resolver;

    public GameAssemblyLoader(string pluginPath) {
        _resolver = new AssemblyDependencyResolver(pluginPath);
    }

    protected override Assembly? Load(AssemblyName assemblyName) {
        var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath != null) {
            return LoadFromAssemblyPath(assemblyPath);
        }

        return null;
    }

    protected override nint LoadUnmanagedDll(string unmanagedDllName) {
        var libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);

        if (libraryPath != null) {
            return LoadUnmanagedDllFromPath(libraryPath);
        }

        return nint.Zero;
    }
}
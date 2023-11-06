using RiptideRendering.Direct3D12;

namespace RiptideRendering;

public struct ContextOptions(RenderingAPI api, IWindow outputWindow) {
    public RenderingAPI Api = api;
    public IWindow OutputWindow = outputWindow;
}

public static class RenderingContext {
    private static BaseRenderingContext? _ctx;

    public static BaseRenderingContext? CreateContext(ContextOptions options) {
        if (!Environment.Is64BitOperatingSystem) throw new PlatformNotSupportedException("Current process is not a 64-bit process.");

        if (options.Api == RenderingAPI.None) throw new ArgumentException("Cannot create rendering context with API value of 'None'.", nameof(options));

        if (_ctx != null) throw new NotSupportedException("Creating multiple rendering context is not supported yet.");

        switch (options.Api) {
            case RenderingAPI.Direct3D12: {
                if (!ApiAvailability.IsDirect3D12Available()) throw new PlatformNotSupportedException("Direct3D12 API is not available.");

                _ctx = new D3D12RenderingContext(options);
                return _ctx;

                //Console.WriteLine("Loading rendering plugin for Direct3D12...");

                //var sw = Stopwatch.StartNew();
                //string pluginPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "RiptideRendering.Direct3D12.dll");

                //using var stream = new FileStream(pluginPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                //var loadContext = new PluginLoadContext(pluginPath);
                //var assembly = loadContext.LoadFromStream(stream);

                //var attribute = assembly.GetCustomAttribute(typeof(ContextTypePointerAttribute<>)) ?? throw new Exception("Failed to load Direct3D12 rendering context because the assembly is missing ContextTypePointerAttribute<T> attribute.");

                //var contextType = attribute.GetType().GetGenericArguments()[0];
                //if (!contextType.IsSubclassOf(typeof(BaseRenderingContext))) throw new Exception($"Failed to load Direct3D12 context because the context type '{contextType.FullName}' is not derived from {nameof(BaseRenderingContext)}.");

                //Console.WriteLine(string.Join('\n', loadContext.Assemblies.Select(x => x.FullName)));

                //var constructors = contextType.GetConstructors();

                //var ctx = (BaseRenderingContext)contextType.GetConstructors()[0].Invoke([options]);

                //sw.Stop();
                //Console.WriteLine($"Context created in {sw.Elapsed.TotalSeconds}sec.");

                //_ctx = ctx;
                //return ctx;
            }

            default:
                if (options.Api.IsDefined()) throw new NotImplementedException($"Context creation for rendering API '{options}' is not implemented yet.");

                throw new ArgumentException("Undefined rendering API.");
        }
    }
}
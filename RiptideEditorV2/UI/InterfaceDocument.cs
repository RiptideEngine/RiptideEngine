using RiptideFoundation.Text;

namespace RiptideEditorV2.UI;

public sealed partial class InterfaceDocument : IDisposable {
    public RootElement Root { get; }

    private RenderingContext _context;

    public Vector2 DisplaySize { get; set; }

    internal InterfaceRenderer Renderer { get; }

    private bool _disposed;
    
    public SignedDistanceFont DefaultFont { get; private set; }
    
    public InterfaceDocument(RenderingContext context) {
        Root = new(this) {
            Name = "Root",
        };

        _context = context;
        
        DefaultFont = SignedDistanceFont.CreateBuilder()
                                        .ImportFromFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Fonts", "arial.ttf"))
                                        .SetFontSize(64)
                                        .SetBitmapDimension(new(1024))
                                        .AddRasterizeCandidate(new IntegerRange<int>(32, 256))
                                        .Build();
        DefaultFont.Name = "Arial Font";

        Renderer = new(this, context);
    }

    public void Update() {
        // TODO: Recursive is relatively expensive so optimize it?

        Root.Layout.X = Root.Layout.Y = 0;
        Root.Layout.Width = DisplaySize.X;
        Root.Layout.Height = DisplaySize.Y;

        Root.ResolvedLayout.Rectangle = new(0, 0, DisplaySize);
        
        foreach (var child in Root) {
            CalculateLayout(child);
        }

        static void CalculateLayout(InterfaceElement node) {
            var layout = node.Layout;
            var resolved = node.ResolvedLayout;
            var parent = node.Parent;
            
            Debug.Assert(parent != null, "parent != null");

            resolved.Rectangle = new(layout.X, layout.Y, layout.Width, layout.Height);
            
            foreach (var child in node) {
                CalculateLayout(child);
            }
        }
    }

    public void Render(CommandList cmdList) {
        cmdList.SetViewport(new(0, 0, DisplaySize.X, DisplaySize.Y, 0, 1));
        cmdList.SetScissorRect(new(0, 0, (int)DisplaySize.X, (int)DisplaySize.Y));
        
        Renderer.RebuildDirtiedElements();
        Renderer.Render(cmdList, DisplaySize);
    }

    private void Dispose(bool disposed) {
        if (_disposed) return;

        DisposeRecursively(Root);

        DefaultFont.DecrementReference();
        Renderer.Dispose();
        
        _disposed = true;

        static void DisposeRecursively(InterfaceElement element) {
            foreach (var child in element) {
                DisposeRecursively(child);
            }
            
            element.Dispose();
        }
    }

    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~InterfaceDocument() {
        Dispose(false);
    }
}
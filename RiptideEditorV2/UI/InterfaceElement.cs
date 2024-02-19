namespace RiptideEditorV2.UI;

public abstract partial class InterfaceElement : IDisposable {
    public string Name { get; set; }
    
    public LayoutComponent Layout { get; private set; }
    public ResolvedLayoutComponent ResolvedLayout { get; private set; }

    protected InterfaceDocument? Document { get; set; }

    private bool _disposed;
    
    internal InterfaceElement() {
        Layout = new(this);
        ResolvedLayout = new();
        
        Name = string.Empty;
    }

    public virtual void Invalidate(InvalidationFlags flags = InvalidationFlags.All) {
        // Document.LayoutEngine.Invalidate(this);
    }
    
    protected virtual void DisposeImpl(bool disposing) { }

    public void Dispose() {
        if (_disposed) return;
        
        DisposeImpl(true);
        GC.SuppressFinalize(this);
        _disposed = true;
    }
    
    ~InterfaceElement() {
        DisposeImpl(false);
    }
}
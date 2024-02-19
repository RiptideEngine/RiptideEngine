using RiptideFoundation.Rendering;

namespace RiptideEditorV2.UI;

public abstract class UIBatcher : IDisposable {
    private bool _disposed;
    
    public abstract void BuildBatch(MaterialPipeline pipeline, InterfaceElement root, Matrix3x2 transformation, in BatchingOperation op);

    public void Dispose() {
        if (_disposed) return;
        
        Dispose(true);
        GC.SuppressFinalize(this);

        _disposed = true;
    }
    
    protected virtual void Dispose(bool disposing) { }
}
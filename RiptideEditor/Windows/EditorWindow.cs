namespace RiptideEditor.Windows;

public abstract class EditorWindow : IDisposable {
    private bool _isDisposed;

    public virtual void Initialize() { }
    public virtual bool Render() => false;

    protected virtual void OnDispose(bool disposeManaged) { }

    private void Dispose(bool disposing) {
        if (!_isDisposed) {
            OnDispose(disposing);

            _isDisposed = true;
        }
    }

    ~EditorWindow() {
        Dispose(disposing: false);
    }

    public void Dispose() {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
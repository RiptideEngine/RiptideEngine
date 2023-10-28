namespace RiptideDatabase;

public struct ResourceStreams(Stream dataStream, Stream? optionsStream = null) : IDisposable {
    private bool _disposed;

    public Stream ResourceStream { get; private set; } = dataStream;
    public Stream? OptionsStream { get; private set; } = optionsStream;

    private void Dispose(bool disposing) {
        if (!_disposed) {
            if (disposing) {
                ResourceStream.Dispose();
                OptionsStream?.Dispose();
            }

            _disposed = true;
        }
    }

    public void Dispose() {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
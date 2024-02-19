using Silk.NET.Assimp;

namespace RiptideFoundation;

public interface IAssimpLibrary : IRiptideService {
    Assimp Assimp { get; }
}

public sealed class AssimpLibraryService : IAssimpLibrary {
    public Assimp Assimp { get; } = Assimp.GetApi();
    private bool _disposed;

    private void Dispose(bool disposing) {
        if (!_disposed) {
            if (disposing) {
                Assimp.Dispose();
            }

            _disposed = true;
        }
    }

    public void Dispose() {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    ~AssimpLibraryService() {
        Dispose(false);
    }
}
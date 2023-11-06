using Silk.NET.Assimp;

namespace RiptideFoundation;

public interface IAssimpLibrary : IRiptideService {
    Assimp Assimp { get; }
}

public sealed class AssimpLibraryService : IAssimpLibrary {
    public Assimp Assimp { get; }
    private bool _disposed;

    public AssimpLibraryService() {
        Assimp = Assimp.GetApi();
    }

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
}
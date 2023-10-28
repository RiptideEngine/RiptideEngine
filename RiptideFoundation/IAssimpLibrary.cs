using Silk.NET.Assimp;

namespace RiptideFoundation;

public interface IAssimpLibrary : IRiptideService {
    Assimp Assimp { get; }
}
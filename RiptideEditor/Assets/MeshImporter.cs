using Silk.NET.Assimp;
using AssimpScene = Silk.NET.Assimp.Scene;
using AssimpMesh = Silk.NET.Assimp.Mesh;
using RiptideMesh = RiptideFoundation.Mesh;

namespace RiptideEditor.Assets;

internal sealed unsafe class MeshImporter : ResourceImporter {
    public override bool CanImport(ImportingLocation location, Type resourceType) {
        return resourceType == typeof(RiptideMesh);
    }

    public override void GetDependencies(ImportingContext context, ref object? userData) {
        
    }

    public override ImportingResult PartiallyLoadResource(object? userData) {
        var assimpService = EditorApplication.Services.GetService<IAssimpLibrary>();
        if (assimpService == null || assimpService.Assimp is not { } assimp) return ImportingResult.FromError(ImportingError.MissingImportingAPI);

        byte[] streamMemory = new byte[_streams.Length];
        _streams.ReadExactly(streamMemory);

        fixed (byte* pMemory = streamMemory) {
            AssimpScene* pScene = assimp.ImportFileFromMemory(pMemory, (uint)streamMemory.Length, (uint)PostProcessPreset.TargetRealTimeQuality, 0);

            if (pScene == null) return ImportingResult.FromError(ImportingError.CorruptedResourceData);

            try {
                return ProcessScene(pScene);
            } finally {
                assimp.FreeScene(pScene);
            }
        }
    }

    private ImportingResult ProcessScene(AssimpScene* scene) {
        (uint VertexStart, uint IndexStart)[] submeshes = new (uint, uint)[scene->MNumMeshes];


    }
}
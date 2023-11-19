using Silk.NET.Assimp;
using AssimpScene = Silk.NET.Assimp.Scene;
using AssimpMesh = Silk.NET.Assimp.Mesh;
using RiptideMesh = RiptideFoundation.Mesh;
using Silk.NET.Maths;

namespace RiptideEditor.Assets;

internal sealed unsafe class MeshImporter : ResourceImporter {
    public override bool CanImport(ImportingLocation location, Type resourceType) {
        return resourceType == typeof(RiptideMesh);
    }

    public override ImportingResult RawImport(ResourceStreams streams) {
        var assimpService = EditorApplication.Services.GetService<IAssimpLibrary>();
        if (assimpService == null || assimpService.Assimp is not { } assimp) return ImportingResult.FromError(ImportingError.MissingImportingAPI);

        byte[] stream = new byte[streams.ResourceStream.Length - streams.ResourceStream.Position];
        streams.ResourceStream.ReadExactly(stream);

        fixed (byte* pMemory = stream) {
            AssimpScene* pScene = assimp.ImportFileFromMemory(pMemory, (uint)stream.Length, (uint)PostProcessPreset.TargetRealTimeQuality, 0);

            if (pScene == null) return ImportingResult.FromError(ImportingError.CorruptedResourceData);

            return ImportingResult.FromResult((nint)pScene);
        }
    }

    public override ImportingResult ImportPartially(object rawObject) {
        Debug.Assert(rawObject is nint);

        var pScene = (AssimpScene*)Unsafe.Unbox<nint>(rawObject);

        try {
            return ProcessScene(pScene);
        } finally {
            EditorApplication.Services.GetRequiredService<IAssimpLibrary>().Assimp.FreeScene(pScene);
        }
    }

    private ImportingResult ProcessScene(AssimpScene* scene) {
        if (scene->MNumMeshes == 0) return ImportingResult.FromError(ImportingError.UnsupportedProperties);
        if (RuntimeFoundation.RenderingService is not { } renderingService) return ImportingResult.FromError(ImportingError.MissingAPI);

        var renderCtx = renderingService.Context;

        uint numVertices = 0, numFaces = 0;
        uint maxVertexCount = 0, maxFaceCount = 0;

        IndexFormat indexFormat = IndexFormat.UInt16;

        Vector3D<float> aabbMin = default, aabbMax = default;

        for (uint m = 0; m < scene->MNumMeshes; m++) {
            AssimpMesh* pMesh = scene->MMeshes[m];

            numVertices += pMesh->MNumVertices;
            numFaces += pMesh->MNumFaces;

            maxVertexCount = uint.Max(maxVertexCount, pMesh->MNumVertices);
            maxFaceCount = uint.Max(maxFaceCount, pMesh->MNumFaces);

            aabbMin = Vector3D.Min(aabbMin, pMesh->MAABB.Min);
            aabbMax = Vector3D.Max(aabbMax, pMesh->MAABB.Max);

            if (indexFormat == IndexFormat.UInt16) {
                for (uint f = 0; f < pMesh->MNumFaces; f++) {
                    Face face = pMesh->MFaces[f];

                    if (face.MIndices[0] > ushort.MaxValue || face.MIndices[1] > ushort.MaxValue || face.MIndices[2] > ushort.MaxValue) {
                        indexFormat = IndexFormat.UInt32;
                    }
                }
            }
        }

        RiptideMesh mesh = new();

        mesh.AllocateVertexBuffer(numVertices, [
            new(16, 0),
        ]);
        mesh.AllocateIndexBuffer(numFaces * 3, indexFormat);
        mesh.SetSubmeshes([ new(0, numFaces * 3, new(new Bound3D(aabbMin.X, aabbMin.Y, aabbMin.Z, aabbMax.X, aabbMax.Y, aabbMax.Z))) ]);

        var cmdList = renderCtx.Factory.CreateCommandList();

        cmdList.UpdateResource(mesh.GetVertexBuffer(0)!, UpdateVertexBuffer, (nint)scene);
        cmdList.UpdateResource(mesh.IndexBuffer!, UpdateIndexBuffer, ((nint)scene, indexFormat));
        
        cmdList.Close();
        renderCtx.ExecuteCommandList(cmdList);
        renderCtx.WaitForGpuIdle();
        cmdList.DecrementReference();

        return ImportingResult.FromResult(mesh);

        static void UpdateVertexBuffer(Span<byte> destination, uint row, nint scene) {
            AssimpScene* pScene = (AssimpScene*)scene;

            fixed (Vector4* pVertices = MemoryMarshal.Cast<byte, Vector4>(destination)) {
                Vector4* vertices = pVertices;

                for (uint m = 0; m < pScene->MNumMeshes; m++) {
                    AssimpMesh* pMesh = pScene->MMeshes[m];

                    for (uint v = 0, vc = pMesh->MNumVertices; v < vc; v++) {
                        vertices[v] = new Vector4(pMesh->MVertices[v], uint.MaxValue);
                    }

                    vertices += pMesh->MNumVertices;
                }
            }
        }
        static void UpdateIndexBuffer(Span<byte> destination, uint row, (nint Scene, IndexFormat Format) state) {
            AssimpScene* pScene = (AssimpScene*)state.Scene;

            switch (state.Format) {
                case IndexFormat.UInt16:
                    fixed (ushort* pIndices = MemoryMarshal.Cast<byte, ushort>(destination)) {
                        var indices = pIndices;

                        for (uint m = 0; m < pScene->MNumMeshes; m++) {
                            var pMesh = pScene->MMeshes[m];

                            for (uint f = 0; f < pMesh->MNumFaces; f++) {
                                var pFace = pMesh->MFaces + f;

                                indices[0] = (ushort)pFace->MIndices[0];
                                indices[1] = (ushort)pFace->MIndices[1];
                                indices[2] = (ushort)pFace->MIndices[2];

                                indices += 3;
                            }
                        }
                    }
                    break;

                case IndexFormat.UInt32:
                    fixed (uint* pIndices = MemoryMarshal.Cast<byte, uint>(destination)) {
                        var indices = pIndices;

                        for (uint m = 0; m < pScene->MNumMeshes; m++) {
                            var pMesh = pScene->MMeshes[m];

                            for (uint f = 0; f < pMesh->MNumFaces; f++) {
                                var pFace = pMesh->MFaces + f;

                                Unsafe.CopyBlock(indices, pFace->MIndices, 12);
                                indices += 3;
                            }
                        }
                    }
                    break;
            }
        }
    }
}
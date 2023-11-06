namespace RiptideEditor.Assets;

internal sealed class SceneImporter : ResourceImporter {
    private struct SceneDTO {
        public string Name;
        public List<Entity> Entities;

        public SceneDTO() {
            Name = null!;
            Entities = null!;
        }
    }

    public override bool CanImport(ImportingLocation location, Type resourceType) {
        return resourceType == typeof(Scene);
    }

    public override ImportingResult RawImport(ResourceStreams streams) {
        try {
            return ImportingResult.FromResult(JsonSerializer.Deserialize<SceneDTO>(streams.ResourceStream));
        } catch (JsonException) {
            return ImportingResult.FromError(ImportingError.CorruptedResourceData);
        } catch {
            return ImportingResult.FromError(ImportingError.Unknown);
        }
    }

    public override ImportingResult ImportPartially(object rawObject) {
        Debug.Assert(rawObject is SceneDTO);

        var dto = Unsafe.Unbox<SceneDTO>(rawObject);

        Scene scene = new() {
            Name = dto.Name,
        };
        RootEntities(scene) = dto.Entities;

        return ImportingResult.FromResult(scene);

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_rootEntities")]
        static extern ref List<Entity> RootEntities(Scene scene);
    }

    [AssetOpen(AssetFileExtensions.Scene, Order = 0)]
    private static void OpenSceneAsset(string path) {
        // EditorScene.CreateTemplatePreviewScene();
    }
}
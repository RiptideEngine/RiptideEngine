namespace RiptideEditor.Assets;

internal sealed class SceneImporter : ResourceImporter {
    public override bool CanImport(ImportingLocation location, Type resourceType) {
        return resourceType == typeof(Scene);
    }

    public override void GetDependencies(ImportingContext context, ref object? userData) {
        try {
            var scene = JsonSerializer.Deserialize<Scene>(_streams.ResourceStream, EditorScene.SceneSerializationOptions);
            userData = scene;
        } catch (JsonException) {
            userData = ImportingError.CorruptedResourceData;
        }

        // TODO: Read Template Entities.

    }

    public override ImportingResult PartiallyLoadResource(object? userData) {
        return userData switch {
            ImportingError error when userData is ImportingError => ImportingResult.FromError(error),
            Scene scene when userData is Scene => ImportingResult.FromResult(scene),
            _ => ImportingResult.FromError(ImportingError.Unknown),
        };
    }

    [AssetOpen(AssetFileExtensions.Scene, Order = 0)]
    private static void OpenSceneAsset(string path) {
        // EditorScene.CreateTemplatePreviewScene();
    }
}
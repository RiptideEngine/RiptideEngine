//namespace RiptideEditor.Windows;

//partial class InspectorWindow {
//    private readonly List<IResourceAsset> _inspectingAssets = [];
//    private bool _multipleResourceTypeConflict = false;

//    private void OnResourceSelectionChanged() {
//        _multipleResourceTypeConflict = false;
//        _inspectingAssets.Clear();

//        int firstHash = AssetImporters.CalculateExtensionHash(Path.GetExtension(ResourceDatabase.RIDToAssetPath(Selections.EnumerateSelectedGuids().First()).AsSpan()));

//        foreach (var path in Selections.EnumerateSelectedGuids().Skip(1).Select(ResourceDatabase.RIDToAssetPath)) {
//            int eh = AssetImporters.CalculateExtensionHash(Path.GetExtension(path.AsSpan()));

//            if (eh != firstHash) {
//                _multipleResourceTypeConflict = true;
//                break;
//            }
//        }

//        if (!_multipleResourceTypeConflict) {
//            _inspectingAssets.AddRange(Selections.EnumerateSelectedGuids().Select(ResourceDatabase.LoadResource));
//        }
//    }

//    private void DoResourceInspecting() {
//        if (_multipleResourceTypeConflict) {
//            ImGui.TextUnformatted("Cannot inspecting multiple asset types.");
//            return;
//        }


//    }
//}
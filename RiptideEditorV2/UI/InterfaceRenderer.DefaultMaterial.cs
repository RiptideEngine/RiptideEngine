using RiptideFoundation.Rendering;

namespace RiptideEditorV2.UI;

partial class InterfaceRenderer {
    private readonly Dictionary<Type, MaterialPipeline> _defaultPipeline = [];
    
    public void SetDefaultPipeline<T>(MaterialPipeline? material) where T : VisualElement {
        var type = typeof(T);
        if (material == null) {
            if (_defaultPipeline.Remove(type, out var removed)) {
                removed.DecrementReference();
            }
        } else {
            ref var mat = ref CollectionsMarshal.GetValueRefOrAddDefault(_defaultPipeline, type, out bool exists);

            if (exists) {
                mat!.DecrementReference();
            }
            
            mat = material;
            material.IncrementReference();
        }
    }

    public MaterialPipeline GetDefaultPipeline<T>() where T : InterfaceElement => GetDefaultPipeline(typeof(T));
    public MaterialPipeline GetDefaultPipeline(Type type) {
        return _defaultPipeline.TryGetValue(type, out var material) ? material : DefaultMaterialPipeline;
    }

    private void DisposeDefaultPipelines() {
        foreach ((_, var pipeline) in _defaultPipeline) {
            pipeline.DecrementReference();
        }
        _defaultPipeline.Clear();
    }
}
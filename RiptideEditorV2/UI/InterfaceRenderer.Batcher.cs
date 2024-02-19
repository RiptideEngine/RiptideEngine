using RiptideFoundation.Rendering;

namespace RiptideEditorV2.UI;

partial class InterfaceRenderer {
    private UIBatcher _defaultBatcher = new DefaultUIBatcher();
    
    private readonly Dictionary<MaterialPipeline, UIBatcher> _batchers = [];

    private UIBatcher GetPipelineBatcher(MaterialPipeline pipeline) => _batchers.TryGetValue(pipeline, out var batcher) ? batcher : _defaultBatcher;

    public void SetPipelineBatcher(MaterialPipeline pipeline, UIBatcher? batcher) {
        if (batcher == null) {
            if (_batchers.Remove(pipeline, out batcher)) {
                batcher.Dispose();
            }
        } else {
            ref var registered = ref CollectionsMarshal.GetValueRefOrAddDefault(_batchers, pipeline, out bool exists);
            if (exists) {
                registered!.Dispose();
            }

            registered = batcher;
        }
    }
    
    private void DisposePipelineBatchers() {
        foreach ((_, var batcher) in _batchers) {
            batcher.Dispose();
        }
        _batchers.Clear();
        
        _defaultBatcher.Dispose();
        _defaultBatcher = null!;
    }
}
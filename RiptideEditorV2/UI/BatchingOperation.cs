using RiptideFoundation.Rendering;

namespace RiptideEditorV2.UI;

public readonly unsafe struct BatchingOperation {
    private readonly IDictionary<int, BatchingCandidate> _batching;
    private readonly IReadOnlyDictionary<int, BatchedCandidate> _batched;
    private readonly MaterialPipeline _pipeline;
    
    internal BatchingOperation(IDictionary<int, BatchingCandidate> batching, IReadOnlyDictionary<int, BatchedCandidate> batched, MaterialPipeline pipeline) {
        _batching = batching;
        _batched = batched;
        _pipeline = pipeline;
    }

    public void AppendUniqueCandidate(VisualElement element, out MeshBuilder builder) {
        var hash = element.CalculateMaterialBatchingHash();
        
        if (!_batching.TryGetValue(hash, out var candidate)) {
            var properties = _batched.TryGetValue(hash, out var batchedCandidate) ? batchedCandidate.Properties : new(_pipeline.Reflection, _pipeline.ResourceSignature);
                    
            candidate = new(new MeshBuilder([new((uint)sizeof(Vertex), 0)]).SetVertexChannel(0), properties);
            _batching.Add(hash, candidate);
                    
            element.BindMaterialProperties(candidate.Properties);
        }

        builder = candidate.Builder;
    }
}
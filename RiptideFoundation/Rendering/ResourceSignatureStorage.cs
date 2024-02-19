using System.Collections.Concurrent;

namespace RiptideFoundation.Rendering;

internal sealed class ResourceSignatureStorage : IDisposable {
    private bool _disposed;
    private readonly Dictionary<int, ResourceSignature> _signatures = [];
    private readonly object _lock = new();

    public ResourceSignature Get(in ResourceSignatureDescription desc) {
        int hash = (int)desc.Flags;
        foreach (ref readonly var parameter in desc.Parameters.AsSpan()) {
            hash = HashCode.Combine(hash, GetResourceParameterHash(parameter));
        }
        foreach (ref readonly var sampler in desc.ImmutableSamplers.AsSpan()) {
            hash = HashCode.Combine(hash, GetImmutableSamplerHash(sampler));
        }

        lock (_lock) {
            if (_signatures.TryGetValue(hash, out var signature)) return signature;

            signature = Graphics.RenderingContext.Factory.CreateResourceSignature(desc);
            _signatures.Add(hash, signature);

            signature.Name = $"{nameof(ResourceSignatureStorage)} Signature {hash}";

            return signature;
        }
        
        static int GetResourceParameterHash(in ResourceParameter desc) {
            switch (desc.Type) {
                case ResourceParameterType.Constants: return HashCode.Combine(desc.Type, desc.Constants.NumConstants, desc.Constants.Register, desc.Constants.Space);
                case ResourceParameterType.Descriptors: return HashCode.Combine(desc.Type, desc.Descriptors.Type, desc.Descriptors.NumDescriptors, desc.Descriptors.BaseRegister, desc.Descriptors.Space);
                
                default: return 0;
            }
        }
        static int GetImmutableSamplerHash(in ImmutableSamplerDescription desc) {
            var a = HashCode.Combine(desc.Register, desc.Space);
            var b = HashCode.Combine(desc.Filter, desc.AddressU, desc.AddressV, desc.AddressW);
            var c = HashCode.Combine(desc.MinLod, desc.MaxLod, desc.MipLodBias);

            return HashCode.Combine(a, b, c, desc.ComparisonOp, desc.MaxAnisotropy);
        }
    }

    private void Dispose(bool disposing) {
        if (_disposed) return;
        
        lock (_lock) {
            foreach ((_, var signature) in _signatures) {
                signature.DecrementReference();
            }
            
            _signatures.Clear();
        }

        _disposed = true;
    }

    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~ResourceSignatureStorage() {
        Dispose(false);
    }
}
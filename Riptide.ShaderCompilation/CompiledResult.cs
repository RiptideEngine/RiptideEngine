namespace Riptide.ShaderCompilation;

/// <summary>
/// Storage of every compilation data that can be retrieved after a compilation process.
/// </summary>
public sealed class CompiledResult : RiptideRcObject {
    private readonly CompiledPayload?[] _payloads;
    
    /// <summary>
    /// Determine whether the result is valid, that is, the error payload contains no error, only warnings at best.
    /// </summary>
    public bool Status { get; private set; }

    internal CompiledResult(CompiledPayload? error, bool status, CompiledPayload? shader, CompiledPayload? reflection) {
        _payloads = new CompiledPayload?[PayloadTypeExtensions.EnumCount];
        _payloads[(int)PayloadType.Error] = error;
        _payloads[(int)PayloadType.Shader] = shader;
        _payloads[(int)PayloadType.Reflection] = reflection;
        Status = status;
        
        _refcount = 1;
    }

    /// <summary>
    /// Get the requesting payload, increment the reference counter if payload exists.
    /// </summary>
    /// <param name="type">Payload type to retrieve.</param>
    /// <returns>The payload data.</returns>
    public CompiledPayload? GetPayload(PayloadType type) {
        if (!type.IsDefined()) return null;
        
        var payload = _payloads[(int)type];
        payload?.IncrementReference();

        return payload;
    }

    protected override void Dispose() {
        foreach (var payload in _payloads.AsSpan()) {
            payload?.DecrementReference();
        }
        Array.Clear(_payloads);
    }
}
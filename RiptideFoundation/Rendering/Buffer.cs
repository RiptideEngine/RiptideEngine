namespace RiptideFoundation.Rendering;

public sealed class Buffer : RenderingObject {
    public GpuBuffer UnderlyingBuffer { get; private set; }
    public ShaderResourceView UnderlyingSrv { get; private set; }
    public UnorderedAccessView? UnderlyingUav { get; private set; }

    public override string? Name {
        get => base.Name;
        set {
            if (GetReferenceCount() == 0) return;

            base.Name = value;
            UnderlyingBuffer.Name = $"{value}.Buffer";
            UnderlyingSrv.Name = $"{value}.SRV";
            
            if (UnderlyingUav != null) {
                UnderlyingUav.Name = $"{value}.UAV";
            }
        }
    }

    public Buffer(BufferDescription bufferDesc, ShaderResourceViewDescription srvdesc, UnorderedAccessViewDescription uavdesc) {
        var factory = Graphics.RenderingContext.Factory;

        try {
            UnderlyingBuffer = factory.CreateBuffer(bufferDesc);
            UnderlyingSrv = factory.CreateShaderResourceView(UnderlyingBuffer, srvdesc);

            if (bufferDesc.Flags.HasFlag(BufferFlags.UnorderedAccess)) {
                UnderlyingUav = factory.CreateUnorderedAccessView(UnderlyingBuffer, uavdesc);
            }
        } catch {
            UnderlyingBuffer?.DecrementReference();
            UnderlyingSrv?.DecrementReference();
            UnderlyingUav?.DecrementReference();
            
            throw;
        }

        _refcount = 1;
    }

    protected override void Dispose() {
        UnderlyingBuffer.DecrementReference();
        UnderlyingBuffer = null!;
        
        UnderlyingSrv.DecrementReference();
        UnderlyingSrv = null!;
        
        UnderlyingUav?.DecrementReference();
        UnderlyingUav = null!;
    }
}
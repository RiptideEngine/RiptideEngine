namespace RiptideFoundation.Rendering;

public abstract class Texture : RenderingObject {
    public GpuTexture UnderlyingTexture { get; protected set; } = null!;
    public ShaderResourceView UnderlyingSrv { get; protected set; } = null!;
    
    public ShaderResourceViewDimension SrvDimension { get; protected set; }
}
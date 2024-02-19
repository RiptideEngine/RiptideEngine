namespace RiptideFoundation.Rendering;

public sealed class TextureCube : Texture {
    public override string? Name {
        get => base.Name;
        set {
            if (GetReferenceCount() == 0) return;
            
            base.Name = value;
            UnderlyingTexture.Name = $"{value}.Texture";
            UnderlyingSrv.Name = $"{value}.SRV";
        }
    }

    public TextureCube(uint width, ushort height, GraphicsFormat format) {
        var factory = Graphics.RenderingContext.Factory;

        try {
            UnderlyingTexture = factory.CreateTexture(new() {
                Width = width,
                Height = height,
                DepthOrArraySize = 6,
                Format = format,
                Dimension = TextureDimension.Texture2D,
                MipLevels = 1,
                Flags = TextureFlags.None,
            });
            UnderlyingSrv = factory.CreateShaderResourceView(UnderlyingTexture, new() {
                Dimension = ShaderResourceViewDimension.TextureCube,
                TextureCube = new() {
                    MipLevels = 1,
                    MostDetailedMip = 0,
                },
            });

            SrvDimension = ShaderResourceViewDimension.TextureCube;
        } catch {
            UnderlyingTexture?.DecrementReference();
            UnderlyingTexture = null!;
            
            throw;
        }

        _refcount = 1;
    }

    protected override void Dispose() {
        UnderlyingTexture.DecrementReference();
        UnderlyingTexture = null!;

        UnderlyingSrv.DecrementReference();
        UnderlyingSrv = null!;
    }
}
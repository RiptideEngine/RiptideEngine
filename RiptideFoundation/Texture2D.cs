namespace RiptideFoundation;

public sealed class Texture2D : RiptideRcObject {
    private GpuTexture _texture;
    private ShaderResourceView _view;

    public GpuTexture UnderlyingTexture => _texture;
    public ShaderResourceView UnderlyingView => _view;

    public override string? Name {
        get => base.Name;
        set {
            base.Name = value;

            if (_texture != null) {
                _texture.Name = $"{value}.Texture";
                _view.Name = $"{value}.View";
            }
        }
    }

    public Texture2D(ushort Width, ushort Height, GraphicsFormat Format) {
        var factory = Graphics.RenderingContext.Factory;

        try {
            _texture = factory.CreateTexture(new() {
                Dimension = TextureDimension.Texture2D,
                Width = Width,
                Height = Height,
                DepthOrArraySize = 1,
                Format = Format,
            });

            _view = factory.CreateShaderResourceView(_texture, new() {
                Dimension = ShaderResourceViewDimension.Texture2D,
                Format = Format,
                Texture2D = new() {
                    MostDetailedMip = 0,
                    MipLevels = 1,
                },
            });
        } catch {
            Dispose();
            throw;
        }

        _refcount = 1;
    }

    protected override void Dispose() {
        _texture?.DecrementReference(); _texture = null!;
        _view?.DecrementReference(); _view = null!;
    }
}
namespace RiptideFoundation;

public sealed class Texture2D : RiptideRcObject {
    private GpuResource _texture;
    private ResourceView _view;

    public GpuResource UnderlyingTexture => _texture;
    public ResourceView UnderlyingView => _view;

    public Texture2D(ushort Width, ushort Height, GraphicsFormat Format) {
        var factory = RuntimeFoundation.RenderingService.Context.Factory;

        try {
            _texture = factory.CreateResource(new() {
                Dimension = ResourceDimension.Texture2D,
                Width = Width,
                Height = Height,
                DepthOrArraySize = 1,
                TextureFormat = Format,
                Flags = ResourceFlags.None,
            });

            _view = factory.CreateResourceView(_texture, new ResourceViewDescriptor() {
                Dimension = ResourceViewDimension.Texture2D,
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
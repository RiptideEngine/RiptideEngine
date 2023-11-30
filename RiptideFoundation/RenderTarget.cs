namespace RiptideFoundation;

public sealed class RenderTarget : RiptideRcObject {
    private GpuTexture _colorTexture;
    private GpuTexture? _depthTexture;

    private RenderTargetView _rtv;
    private DepthStencilView? _dsv;

    public GpuResource UnderlyingTexture => _colorTexture;
    public GpuResource? UnderlyingDepthTexture => _depthTexture;
    
    public RenderTargetView UnderlyingView => _rtv;
    public DepthStencilView? UnderlyingDepthView => _dsv;

    public RenderTarget(TextureDimension dimension, uint width, ushort height, ushort depthOrArraySize, GraphicsFormat format, GraphicsFormat depthFormat) {
        if (dimension == TextureDimension.Unknown) throw new ArgumentException("Cannot create RenderTarget with unknown dimension.");
        if (!dimension.IsDefined()) throw new ArgumentException("Cannot create RenderTarget with undefined dimension.");

        var factory = Graphics.RenderingContext.Factory;

        try {
            RenderTargetViewDimension rtvDimension;
            DepthStencilViewDimension dsvDimension;

            switch (dimension) {
                case TextureDimension.Texture1D:
                    if (depthOrArraySize == 1) {
                        rtvDimension = RenderTargetViewDimension.Texture1D;
                        dsvDimension = DepthStencilViewDimension.Texture1D;
                    } else {
                        rtvDimension = RenderTargetViewDimension.Texture1DArray;
                        dsvDimension = DepthStencilViewDimension.Texture1DArray;
                    }
                    break;

                case TextureDimension.Texture2D:
                    if (depthOrArraySize == 1) {
                        rtvDimension = RenderTargetViewDimension.Texture2D;
                        dsvDimension = DepthStencilViewDimension.Texture2D;
                    } else {
                        rtvDimension = RenderTargetViewDimension.Texture2DArray;
                        dsvDimension = DepthStencilViewDimension.Texture2DArray;
                    }
                    break;

                case TextureDimension.Texture3D:
                    rtvDimension = RenderTargetViewDimension.Texture3D;
                    dsvDimension = DepthStencilViewDimension.Texture2DArray;
                    break;

                default: throw new UnreachableException();
            }

            _colorTexture = factory.CreateTexture(new() {
                Dimension = dimension,
                Width = width,
                Height = height,
                DepthOrArraySize = depthOrArraySize,
                Format = format,
                Flags = TextureFlags.RenderTarget,
            });
            _rtv = factory.CreateRenderTargetView(_colorTexture, new() {
                Dimension = rtvDimension,
                Format = format,
            });

            if (depthFormat != GraphicsFormat.Unknown) {
                if (!depthFormat.IsDepthFormat()) throw new ArgumentException($"Depth format must be {nameof(GraphicsFormat.D16UNorm)}, {nameof(GraphicsFormat.D24UNormS8UInt)}, {nameof(GraphicsFormat.D32Float)}, {nameof(GraphicsFormat.D32FloatS8X24UInt)}.");

                _depthTexture = factory.CreateTexture(new() {
                    Dimension = dimension,
                    Width = width,
                    Height = height,
                    DepthOrArraySize = depthOrArraySize,
                    Format = depthFormat,
                    Flags = TextureFlags.DepthStencil,
                });
                _dsv = factory.CreateDepthStencilView(_depthTexture, new() {
                    Dimension = dsvDimension,
                    Format = depthFormat,
                });
            }
        } catch {
            Dispose();
            throw;
        }

        _refcount = 1;
    }

    protected override void Dispose() {
        _colorTexture?.DecrementReference(); _colorTexture = null!;
        _depthTexture?.DecrementReference(); _depthTexture = null!;
        _rtv?.DecrementReference(); _rtv = null!;
        _dsv?.DecrementReference(); _dsv = null!;
    }
}
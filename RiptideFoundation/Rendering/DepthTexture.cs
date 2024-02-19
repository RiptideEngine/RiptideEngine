namespace RiptideFoundation.Rendering;

public sealed class DepthTexture : Texture {
    public DepthStencilView UnderlyingDsv { get; private set; }
    public ShaderResourceView? StencilSrv { get; private set; }

    public override string? Name {
        get => base.Name;
        set {
            if (GetReferenceCount() == 0) return;

            base.Name = value;
            UnderlyingTexture.Name = $"{value}.Texture";
            UnderlyingDsv.Name = $"{value}.DSV";
            
            if (UnderlyingSrv != null) UnderlyingSrv.Name = $"{value}.DepthSRV";
            if (StencilSrv != null) StencilSrv.Name = $"{value}.StencilSRV";
        }
    }

    public DepthTexture(TextureDimension dimension, uint width, ushort height, ushort depthOrArraySize, GraphicsFormat format, bool allowShaderResourceView = false) {
        GraphicsFormat textureFormat, dsvFormat;
        
        switch (format) {
            case GraphicsFormat.D16UNorm:
                textureFormat = GraphicsFormat.R16Typeless;
                dsvFormat = GraphicsFormat.D16UNorm;
                break;
            
            case GraphicsFormat.D24UNormS8UInt:
                textureFormat = GraphicsFormat.R24G8Typeless;
                dsvFormat = GraphicsFormat.D24UNormS8UInt;
                break;
            
            case GraphicsFormat.D32Float:
                textureFormat = GraphicsFormat.R32Typeless;
                dsvFormat = GraphicsFormat.D32Float;
                break;
            
            case GraphicsFormat.D32FloatS8X24UInt:
                textureFormat = GraphicsFormat.R32G8X24Typeless;
                dsvFormat = GraphicsFormat.D32FloatS8X24UInt;
                break;
            
            default: throw new ArgumentException("Format must be a valid depth format.");
        }
        
        var factory = Graphics.RenderingContext.Factory;
        
        try {
            DepthStencilViewDescription dsvdesc = CreateDSVDescription(dsvFormat, dimension, depthOrArraySize);

            UnderlyingTexture = factory.CreateTexture(new() {
                Dimension = dimension,
                Width = width,
                Height = height,
                DepthOrArraySize = depthOrArraySize,
                Format = textureFormat,
                Flags = TextureFlags.DepthStencil,
                MipLevels = 1,
            });
            UnderlyingDsv = factory.CreateDepthStencilView(UnderlyingTexture, dsvdesc);

            if (allowShaderResourceView) {
                switch (format) {
                    case GraphicsFormat.D16UNorm:
                        UnderlyingSrv = factory.CreateShaderResourceView(UnderlyingTexture, CreateSRVDescription(GraphicsFormat.R16UNorm, dimension, depthOrArraySize, 0));
                        break;
                    
                    case GraphicsFormat.D24UNormS8UInt:
                        UnderlyingSrv = factory.CreateShaderResourceView(UnderlyingTexture, CreateSRVDescription(GraphicsFormat.R24UNormX8Typeless, dimension, depthOrArraySize, 0));
                        StencilSrv = factory.CreateShaderResourceView(UnderlyingTexture, CreateSRVDescription(GraphicsFormat.X24TypelessG8UInt, dimension, depthOrArraySize, 1));
                        break;
                    
                    case GraphicsFormat.D32Float:
                        UnderlyingSrv = factory.CreateShaderResourceView(UnderlyingTexture, CreateSRVDescription(GraphicsFormat.R32Float, dimension, depthOrArraySize, 0));
                        break;
                    
                    case GraphicsFormat.D32FloatS8X24UInt:
                        UnderlyingSrv = factory.CreateShaderResourceView(UnderlyingTexture, CreateSRVDescription(GraphicsFormat.R32FloatX8X24Typeless, dimension, depthOrArraySize, 0));
                        StencilSrv = factory.CreateShaderResourceView(UnderlyingTexture, CreateSRVDescription(GraphicsFormat.X32TypelessG8X24Uint, dimension, depthOrArraySize, 1));
                        break;
                }
            }
        } catch {
            Dispose();
            throw;
        }

        _refcount = 1;

        static DepthStencilViewDescription CreateDSVDescription(GraphicsFormat format, TextureDimension dimension, uint depthOrArraySize) {
            DepthStencilViewDescription dsvdesc = new() {
                Format = format,
            };

            switch (dimension) {
                case TextureDimension.Texture1D:
                    if (depthOrArraySize == 1) {
                        dsvdesc.Dimension = DepthStencilViewDimension.Texture1D;
                        dsvdesc.Texture1D = new() {
                            MipSlice = 0,
                        };
                    } else {
                        dsvdesc.Dimension = DepthStencilViewDimension.Texture1DArray;
                        dsvdesc.Texture1DArray = new() {
                            MipSlice = 0,
                            ArraySize = depthOrArraySize,
                            FirstArraySlice = 0,
                        };
                    }
                    break;

                case TextureDimension.Texture2D:
                    if (depthOrArraySize == 1) {
                        dsvdesc.Dimension = DepthStencilViewDimension.Texture2D;
                        dsvdesc.Texture2D = new() {
                            MipSlice = 0,
                        };
                    } else {
                        dsvdesc.Dimension = DepthStencilViewDimension.Texture2DArray;
                        dsvdesc.Texture2DArray = new() {
                            MipSlice = 0,
                            ArraySize = depthOrArraySize,
                            FirstArraySlice = 0,
                        };
                    }
                    break;

                default: throw new ArgumentException($"Cannot create DepthTexture with '{dimension}' dimension.");
            }

            return dsvdesc;
        }
        static ShaderResourceViewDescription CreateSRVDescription(GraphicsFormat format, TextureDimension dimension, uint depthOrArraySize, uint planeSlice) {
            ShaderResourceViewDescription dsvdesc = new() {
                Format = format,
            };

            switch (dimension) {
                case TextureDimension.Texture1D:
                    if (depthOrArraySize == 1) {
                        dsvdesc.Dimension = ShaderResourceViewDimension.Texture1D;
                        dsvdesc.Texture1D = new() {
                            MipLevels = 1,
                            MostDetailedMip = 0,
                        };
                    } else {
                        dsvdesc.Dimension = ShaderResourceViewDimension.Texture1DArray;
                        dsvdesc.Texture1DArray = new() {
                            MipLevels = 1,
                            MostDetailedMip = 0,
                            ArraySize = depthOrArraySize,
                            FirstArraySlice = 0,
                        };
                    }
                    break;

                case TextureDimension.Texture2D:
                    if (depthOrArraySize == 1) {
                        dsvdesc.Dimension = ShaderResourceViewDimension.Texture2D;
                        dsvdesc.Texture2D = new() {
                            MipLevels = 1,
                            MostDetailedMip = 0,
                            PlaneSlice = planeSlice,
                        };
                    } else {
                        dsvdesc.Dimension = ShaderResourceViewDimension.Texture2DArray;
                        dsvdesc.Texture2DArray = new() {
                            MipLevels = 1,
                            MostDetailedMip = 0,
                            ArraySize = depthOrArraySize,
                            FirstArraySlice = 0,
                            PlaneSlice = planeSlice,
                        };
                    }
                    break;

                default: throw new ArgumentException($"Cannot create DepthTexture with '{dimension}' dimension.");
            }

            return dsvdesc;
        }
    }

    protected override void Dispose() {
        UnderlyingTexture?.DecrementReference(); UnderlyingTexture = null!;
        UnderlyingDsv?.DecrementReference(); UnderlyingDsv = null!;
        UnderlyingSrv?.DecrementReference(); UnderlyingSrv = null!;
        StencilSrv?.DecrementReference(); StencilSrv = null!;
    }
}
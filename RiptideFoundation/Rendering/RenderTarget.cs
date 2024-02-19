namespace RiptideFoundation.Rendering;

public sealed class RenderTarget : Texture {
    public RenderTargetView UnderlyingRtv { get; private set; }

    public override string? Name {
        get => base.Name;
        set {
            if (Interlocked.Read(ref _refcount) == 0) return;

            base.Name = value;
            UnderlyingTexture.Name = $"{value}.Texture";
            UnderlyingRtv.Name = $"{value}.RTV";
            UnderlyingSrv.Name = $"{value}.SRV";
        }
    }

    public RenderTarget(TextureDimension dimension, uint width, ushort height, ushort depthOrArraySize, GraphicsFormat format) {
        if (dimension == TextureDimension.Unknown) throw new ArgumentException("Cannot create RenderTarget with unknown dimension.");
        if (!dimension.IsDefined()) throw new ArgumentException("Cannot create RenderTarget with undefined dimension.");

        var factory = Graphics.RenderingContext.Factory;

        try {
            RenderTargetViewDescription rtvDesc = new() {
                Format = format,
            };
            ShaderResourceViewDescription srvdesc = new() {
                Format = format,
            };

            switch (dimension) {
                case TextureDimension.Texture1D:
                    if (depthOrArraySize == 1) {
                        rtvDesc.Dimension = RenderTargetViewDimension.Texture1D;
                        rtvDesc.Texture1D = new() {
                            MipSlice = 0,
                        };

                        srvdesc.Dimension = ShaderResourceViewDimension.Texture1D;
                        srvdesc.Texture1D = new() {
                            MipLevels = 1,
                            MostDetailedMip = 0,
                        };
                    } else {
                        rtvDesc.Dimension = RenderTargetViewDimension.Texture1DArray;
                        rtvDesc.Texture1DArray = new() {
                            MipSlice = 0,
                            FirstArraySlice = 0,
                            ArraySize = depthOrArraySize,
                        };
                        
                        srvdesc.Dimension = ShaderResourceViewDimension.Texture1DArray;
                        srvdesc.Texture1DArray = new() {
                            MipLevels = 1,
                            MostDetailedMip = 0,
                            ArraySize = depthOrArraySize,
                            FirstArraySlice = 0,
                        };
                    }
                    break;

                case TextureDimension.Texture2D:
                    if (depthOrArraySize == 1) {
                        rtvDesc.Dimension = RenderTargetViewDimension.Texture2D;
                        rtvDesc.Texture2D = new() {
                            MipSlice = 0,
                        };

                        srvdesc.Dimension = ShaderResourceViewDimension.Texture2D;
                        srvdesc.Texture2D = new() {
                            MipLevels = 1,
                            MostDetailedMip = 0,
                            PlaneSlice = 0,
                        };
                    } else {
                        rtvDesc.Dimension = RenderTargetViewDimension.Texture2DArray;
                        rtvDesc.Texture2DArray = new() {
                            MipSlice = 0,
                            FirstArraySlice = 0,
                            ArraySize = depthOrArraySize,
                        };

                        srvdesc.Dimension = ShaderResourceViewDimension.Texture2DArray;
                        srvdesc.Texture2DArray = new() {
                            MipLevels = 1,
                            MostDetailedMip = 0,
                            ArraySize = depthOrArraySize,
                            FirstArraySlice = 0,
                            PlaneSlice = 0,
                        };
                    }
                    break;

                case TextureDimension.Texture3D:
                    rtvDesc.Dimension = RenderTargetViewDimension.Texture3D;
                    rtvDesc.Texture3D = new() {
                        MipSlice = 0,
                        FirstWSlice = 0,
                        WSize = depthOrArraySize,
                    };

                    srvdesc.Dimension = ShaderResourceViewDimension.Texture3D;
                    srvdesc.Texture3D = new() {
                        MipLevels = 1,
                        MostDetailedMip = 0,
                    };
                    break;

                default: throw new UnreachableException();
            }

            UnderlyingTexture = factory.CreateTexture(new() {
                Dimension = dimension,
                Width = width,
                Height = height,
                DepthOrArraySize = depthOrArraySize,
                Format = format,
                Flags = TextureFlags.RenderTarget,
                MipLevels = 1,
            });
            UnderlyingRtv = factory.CreateRenderTargetView(UnderlyingTexture, rtvDesc);
            UnderlyingSrv = factory.CreateShaderResourceView(UnderlyingTexture, srvdesc);
        } catch {
            Dispose();
            throw;
        }

        _refcount = 1;
    }

    protected override void Dispose() {
        UnderlyingTexture?.DecrementReference(); UnderlyingTexture = null!;
        UnderlyingRtv?.DecrementReference(); UnderlyingRtv = null!;
        UnderlyingSrv?.DecrementReference(); UnderlyingSrv = null!;
    }
}
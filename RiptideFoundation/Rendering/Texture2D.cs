namespace RiptideFoundation.Rendering;

public sealed class Texture2D : Texture {
    private UnorderedAccessView[] _uavses;
    
    public UnorderedAccessView[] UnderlyingUavs => _uavses;

    public bool AllowUnorderedAccess => _uavses.Length != 0;

    public override string? Name {
        get => base.Name;
        set {
            if (GetReferenceCount() == 0) return;
            
            base.Name = value;
            UnderlyingTexture.Name = $"{value}.Texture";
            UnderlyingSrv.Name = $"{value}.SRV";

            for (int i = 0; i < _uavses.Length; i++) {
                _uavses[i].Name = $"{value}.UAV[{i}]";
            }
        }
    }

    public Vector2UInt Size {
        get {
            if (GetReferenceCount() == 0) return Vector2UInt.Zero;

            var desc = UnderlyingTexture.Description;
            return new(desc.Width, desc.Height);
        }
    }

    public Texture2D(uint width, ushort height, GraphicsFormat format, bool allowUnorderedAccess = false, uint mipLevels = 1) {
        var factory = Graphics.RenderingContext.Factory;

        try {
            UnderlyingTexture = factory.CreateTexture(new() {
                Dimension = TextureDimension.Texture2D,
                Width = width,
                Height = height,
                DepthOrArraySize = 1,
                Format = format,
                Flags = allowUnorderedAccess ? TextureFlags.UnorderedAccess : 0,
                MipLevels = mipLevels,
            });

            UnderlyingSrv = factory.CreateShaderResourceView(UnderlyingTexture, new() {
                Dimension = ShaderResourceViewDimension.Texture2D,
                Format = format,
                Texture2D = new() {
                    MostDetailedMip = 0,
                    MipLevels = unchecked((uint)-1),
                },
            });

            if (allowUnorderedAccess) {
                uint maxMips = uint.Log2(uint.Max(width, height)) + 1;
                uint mipCount = mipLevels == 0 ? maxMips : uint.Min(mipLevels, maxMips);

                _uavses = new UnorderedAccessView[mipCount];
                
                UnorderedAccessViewDescription desc = new() {
                    Dimension = UnorderedAccessViewDimension.Texture2D,
                    Format = format,
                };
                
                for (uint i = 0; i < _uavses.Length; i++) {
                    _uavses[i] = factory.CreateUnorderedAccessView(UnderlyingTexture, desc with {
                        Texture2D = new() {
                            MipSlice = i,
                            PlaneSlice = 0,
                        },
                    });
                }
            } else {
                _uavses = [];
            }

            SrvDimension = ShaderResourceViewDimension.Texture2D;
        } catch {
            Dispose();
            throw;
        }

        _refcount = 1;
    }

    protected override void Dispose() {
        UnderlyingTexture?.DecrementReference(); UnderlyingTexture = null!;
        UnderlyingSrv?.DecrementReference(); UnderlyingSrv = null!;

        foreach (var uav in _uavses) {
            uav.DecrementReference();
        }
        _uavses = [];
    }
}
namespace RiptideFoundation.Rendering;

public sealed class Texture2DArray : Texture {
    private UnorderedAccessView[] _uavses;
    public UnorderedAccessView[] UnderlyingUavs => _uavses;

    public bool AllowUnorderedAccess => _uavses.Length != 0;
    
    public Vector2UInt Size {
        get {
            var dimension = Dimension;
            return Unsafe.As<Vector3UInt, Vector2UInt>(ref dimension);
        }
    }

    public uint ArraySize => Dimension.Z;

    public Vector3UInt Dimension {
        get {
            if (GetReferenceCount() == 0) return Vector3UInt.Zero;

            var desc = UnderlyingTexture.Description;
            return new(desc.Width, desc.Height, desc.DepthOrArraySize);
        }
    }
    
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

    public Texture2DArray(uint width, ushort height, ushort arraySize, GraphicsFormat format, bool allowUnorderedAccess = false, uint mipLevels = 1) {
        var factory = Graphics.RenderingContext.Factory;

        try {
            UnderlyingTexture = factory.CreateTexture(new() {
                Dimension = TextureDimension.Texture2D,
                Width = width,
                Height = height,
                DepthOrArraySize = arraySize,
                Format = format,
                Flags = allowUnorderedAccess ? TextureFlags.UnorderedAccess : 0,
                MipLevels = mipLevels,
            });

            UnderlyingSrv = factory.CreateShaderResourceView(UnderlyingTexture, new() {
                Dimension = ShaderResourceViewDimension.Texture2DArray,
                Format = format,
                Texture2DArray = new() {
                    MostDetailedMip = 0,
                    MipLevels = unchecked((uint)-1),
                    ArraySize = arraySize,
                    FirstArraySlice = 0,
                    PlaneSlice = 0,
                },
            });

            if (allowUnorderedAccess) {
                uint maxMips = uint.Log2(uint.Max(width, height)) + 1;
                uint mipCount = mipLevels == 0 ? maxMips : uint.Min(mipLevels, maxMips);

                _uavses = new UnorderedAccessView[mipCount];
                
                UnorderedAccessViewDescription desc = new() {
                    Dimension = UnorderedAccessViewDimension.Texture2DArray,
                    Format = format,
                };
                
                for (uint i = 0; i < _uavses.Length; i++) {
                    _uavses[i] = factory.CreateUnorderedAccessView(UnderlyingTexture, desc with {
                        Texture2DArray = new() {
                            MipSlice = i,
                            ArraySize = arraySize,
                            FirstArraySlice = 0,
                            PlaneSlice = 0,
                        },
                    });
                }
            } else {
                _uavses = [];
            }

            SrvDimension = ShaderResourceViewDimension.Texture2DArray;
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
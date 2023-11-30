using Silk.NET.Direct3D12;

namespace RiptideRendering.Direct3D12;

internal static unsafe class Helper {
    public static CpuDescriptorHandle UnknownCpuHandle => new() {
        Ptr = nuint.MaxValue,
    };
    public static GpuDescriptorHandle UnknownGpuHandle => new() {
        Ptr = nuint.MaxValue,
    };
    
    private static Guid _nameGuid = new Guid(0x4cca5fd8, 0x921f, 0x42c8, 0x85, 0x66, 0x70, 0xca, 0xf2, 0xa9, 0xb7, 0x41);
    public const uint DefaultShader4ComponentMapping = 0 | 1 << 3 | 2 << 6 | 3 << 9 | 1 << 12;
    
    public static void SetName<T>(T* pObject, ReadOnlySpan<char> name) where T : unmanaged, IComVtbl<ID3D12Object> {
        fixed (char* pName = name) {
            ((ID3D12Object*)pObject)->SetPrivateData(ref _nameGuid, sizeof(char) * (uint)name.Length, pName);
        }
    }

    public static string? GetName<T>(T* pObject) where T : unmanaged, IComVtbl<ID3D12Object> {
        uint size = 0;
        HResult hr = ((ID3D12Object*)pObject)->GetPrivateData(ref _nameGuid, ref size, null);
        if (hr.IsError) return null;

        return string.Create((int)size, ((nint)pObject, size), (Span<char> span, (nint pObject, uint size) _) => {
            fixed (char* pString = span) {
                ((ID3D12Object*)pObject)->GetPrivateData(ref _nameGuid, ref size, pString);
            }
        });
    }
}
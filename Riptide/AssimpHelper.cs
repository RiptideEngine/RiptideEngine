using RiptideEngine.Core.Utils;
using Silk.NET.Assimp;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Riptide;

internal static unsafe class AssimpHelper {
    public static uint GetTextureCount(this Material material, TextureType type) {
        uint max = 0;
        for (uint i = 0; i < material.MNumProperties; i++) {
            MaterialProperty* prop = material.MProperties[i];

            if (UnsafeHelpers.StringCompare(prop->MKey.Data, (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference("$tex.file\0"u8))) == 0 && (TextureType)prop->MSemantic == type) {
                max = uint.Max(max, prop->MIndex + 1);
            }
        }
        return max;
    }

    public static Return GetProperty(this Material material, byte* pKey, uint type, uint index, MaterialProperty** pProperties) {
        Debug.Assert(pKey != null && pProperties != null);

        for (uint i = 0; i < material.MNumProperties; i++) {
            MaterialProperty* property = material.MProperties[i];

            if (UnsafeHelpers.StringCompare(property->MKey.Data, pKey) == 0 && (type == uint.MaxValue || property->MSemantic == type) && (index == uint.MaxValue || property->MIndex == index)) {
                *pProperties = property;
                return Return.Success;
            }
        }

        *pProperties = null;
        return Return.Failure;
    }

    public static Return GetString(this Material material, byte* pKey, uint type, uint index, AssimpString* pOutput) {
        Debug.Assert(pOutput != null);

        MaterialProperty* pProperty;
        var result = material.GetProperty(pKey, type, index, &pProperty);
        if (result != Return.Success) return result;

        Debug.Assert(pProperty != null);

        if (pProperty->MType == PropertyTypeInfo.String) {
            Debug.Assert(pProperty->MDataLength >= 5);

            pOutput->Length = *(uint*)pProperty->MData;

            Unsafe.CopyBlock(pOutput->Data, pProperty->MData + 4, pOutput->Length + 1);

            return Return.Success;
        }

        pOutput = null;
        return Return.Failure;
    }

    public static Return GetTexture(this Material material, TextureType type, uint index, AssimpString* pPath) {
        Debug.Assert(pPath != null);

        if (material.GetString((byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference("$tex.file\0"u8)), (uint)type, index, pPath) != Return.Success) {
            return Return.Failure;
        }

        return Return.Success;
    }

    public static ReadOnlySpan<byte> GetName(this Material material) {
        for (uint i = 0; i < material.MNumProperties; i++) {
            MaterialProperty* prop = material.MProperties[i];

            if (UnsafeHelpers.StringCompare(prop->MKey.Data, (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference("?mat.name\0"u8))) == 0) {
                AssimpString* pString = (AssimpString*)prop->MData;
                
                return new(pString->Data, (int)pString->Length);
            }
        }

        return ReadOnlySpan<byte>.Empty;
    }
}
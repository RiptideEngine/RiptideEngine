using Silk.NET.SPIRV.Cross;
using Silk.NET.SPIRV.Reflect;
using SpvcResult = Silk.NET.SPIRV.Cross.Result;

namespace Riptide.ShaderCompilation;

internal static class Helper {
    public static void CheckError(SpvcResult error) {
        switch (error) {
            case SpvcResult.ErrorInvalidSpirv: throw new ArgumentException("Invalid SPIR-V bytecode provided.");
            case SpvcResult.ErrorUnsupportedSpirv: throw new ArgumentException("Unsupported SPIR-V bytecode provided.");
            case SpvcResult.ErrorInvalidArgument: throw new ArgumentException("Invalid argument detected.");
            case SpvcResult.ErrorOutOfMemory: throw new OutOfMemoryException("Out of memory.");
        }
    }
}
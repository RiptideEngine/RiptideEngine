using Silk.NET.Direct3D12;

namespace RiptideRendering.Direct3D12;

partial class Converting {
    public static ResourceStates Convert(ResourceTranslateStates states) {
        ResourceStates output = ResourceStates.Common;

        output |= (ResourceStates)((int)(states & (ResourceTranslateStates.ConstantBuffer | ResourceTranslateStates.IndexBuffer | ResourceTranslateStates.RenderTarget | ResourceTranslateStates.UnorderedAccess | ResourceTranslateStates.DepthWrite | ResourceTranslateStates.DepthRead)) >> 1);
        output |= states.HasFlag(ResourceTranslateStates.ShaderResource) ? ResourceStates.AllShaderResource : 0;
        output |= (ResourceStates)((int)(states & (ResourceTranslateStates.CopyDestination | ResourceTranslateStates.CopySource)) << 1);
    
        return output;
    }
}
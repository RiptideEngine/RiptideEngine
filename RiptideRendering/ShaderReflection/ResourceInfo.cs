namespace RiptideRendering.ShaderReflection;

public readonly record struct ConstantBufferInfo(string Name, ResourceBindLocation Location, uint Size, bool IsRootConstants);
public readonly record struct ReadonlyResourceInfo(string Name, ResourceBindLocation Location, ResourceType Type);
public readonly record struct ReadWriteResourceInfo(string Name, ResourceBindLocation Location, ResourceType Type);
public readonly record struct SamplerInfo(string Name, ResourceBindLocation Location);
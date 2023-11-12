namespace RiptideRendering;

public readonly record struct ConstantBufferInfo(string Name, uint Register, uint Space, uint Size);
public readonly record struct ReadonlyResourceInfo(string Name, uint Register, uint Space, ResourceType Type);
public readonly record struct ReadWriteResourceInfo(string Name, uint Register, uint Space, ResourceType Type);
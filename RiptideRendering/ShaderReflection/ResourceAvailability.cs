namespace RiptideRendering.ShaderReflection;

[Flags]
public enum ResourceAvailability {
    Vertex = 1 << 0,
    Hull = 1 << 1,
    Domain = 1 << 2,
    Pixel = 1 << 3,
}
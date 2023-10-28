namespace RiptideRendering;

public abstract class Shader : RenderingObject {
    public abstract ShaderReflector Reflector { get; }
}
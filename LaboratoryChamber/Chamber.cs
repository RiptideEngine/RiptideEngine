using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace Riptide.Laboratory;

public abstract partial class Chamber {
    public IWindow MainWindow { get; init; } = null!;
    
    protected abstract void Initialize();
    protected abstract void Shutdown();

    protected abstract void Update(double deltaTime);
    protected abstract void Render(double deltaTime);

    protected virtual void Resize(Vector2D<int> size) { }
}
namespace RiptideFoundation;

public interface IRenderingService : IRiptideService {
    RenderingContext Context { get; }
}

public sealed class RenderingService : IRenderingService {
    public RenderingContext Context { get; }

    public RenderingService(ContextOptions options) {
        Context = RenderingContext.CreateContext(options) ?? throw new ArgumentException("Failed to create rendering option.");
    }

    public void Dispose() {
        Context.Dispose();
    }
}
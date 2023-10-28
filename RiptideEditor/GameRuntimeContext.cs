namespace RiptideEditor;

internal static class GameRuntimeContext {
    public static GameRuntimeWindowService WindowService { get; private set; } = null!;
    
    public static void Initialize(RiptideServices services) {
        WindowService = services.CreateService<IRuntimeWindowService, GameRuntimeWindowService>();
    }
}
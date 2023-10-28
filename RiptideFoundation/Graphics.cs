namespace RiptideFoundation;

public static class Graphics {
    internal static BaseRenderingContext RenderingContext => RuntimeFoundation.RenderingService.Context;
    internal static RenderingPipeline RenderingPipeline { get; set; } = null!;

    private static readonly List<CommandList> _commandLists;

    static Graphics() {
        _commandLists = new();
    }

    public static void AddCommandListExecutionBatch(CommandList commandList) {
        commandList.IncrementReference();
        _commandLists.Add(commandList);
    }

    public static void FlushCommandListExecutionBatch() {
        RenderingContext.ExecuteCommandLists(CollectionsMarshal.AsSpan(_commandLists));

        foreach (var cmdList in _commandLists) {
            cmdList.DecrementReference();
        }

        _commandLists.Clear();
    }
}
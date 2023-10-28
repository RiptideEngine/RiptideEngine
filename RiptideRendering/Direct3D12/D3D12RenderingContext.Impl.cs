namespace RiptideRendering.Direct3D12;

unsafe partial class D3D12RenderingContext {
    private readonly ulong[] _renderingLatencyFences = new ulong[Display.BufferCount];

    //public override void ExecuteCommandList(RiptideRendering.CommandContext list) {
    //    Debug.Assert(list is D3D12CommandContext, $"Invalid command list object. Expected D3D12CommandList, but provided type was '{list.GetType().FullName}'.");

    //    var list2 = Unsafe.As<D3D12CommandContext>(list);
    //    list2.CommandList->Close();

    //    RenderingQueue.ExecuteCommandList((ID3D12CommandList*)list2.CommandList);
    //    list2.Reset();
    //}

    public override void WaitForGpuIdle() {
        RenderingQueue.WaitForIdle();
    }

    public override void ExecuteCommandList(CommandList commandList) {
        Debug.Assert(commandList is D3D12CommandList, "Command list is not a Direct3D12's CommandList.");

        RenderingQueue.ExecuteCommandList((ID3D12CommandList*)Unsafe.As<D3D12CommandList>(commandList).CommandList);
    }

    public override void ExecuteCommandLists(ReadOnlySpan<CommandList> commandLists) {
        if (commandLists.IsEmpty) return;

        ID3D12CommandList** ppCommandLists = stackalloc ID3D12CommandList*[commandLists.Length];
        for (int i = 0; i < commandLists.Length; i++) {
            Debug.Assert(commandLists[i] is D3D12CommandList, "Command list index " + i + " is not a Direct3D12's CommandList.");

            ppCommandLists[i] = (ID3D12CommandList*)Unsafe.As<D3D12CommandList>(commandLists[i]).CommandList;
        }

        RenderingQueue.ExecuteCommandLists((uint)commandLists.Length, ppCommandLists);
    }

    protected override void ResizeSwapchainImpl(uint width, uint height) {
        RenderingQueue.WaitForIdle();

        Display.ResizeSwapchain(width, height);
    }

    public override void Present() {
        var queue = RenderingQueue;

        var oldSwapchainIndex = Display.CurrentSwapchainIndex;

        Display.Present();

        queue.WaitForFence(_renderingLatencyFences[oldSwapchainIndex]);

        _deferredDestructor.ReleaseResources(queue.CompletedValue);

        _renderingLatencyFences[oldSwapchainIndex] = queue.NextFenceValue - 1;
    }
}
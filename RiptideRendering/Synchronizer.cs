namespace RiptideRendering;

public abstract class Synchronizer {
    public abstract void WaitCpu(ulong fenceValue);
}
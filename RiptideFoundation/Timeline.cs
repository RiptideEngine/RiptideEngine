namespace RiptideFoundation;

public unsafe class Timeline {
    public float DeltaTime { get; private set; }
    public float ElapsedTime { get; private set; }

    public double DeltaTimeF64 { get; private set; }
    public double ElapsedTimeF64 { get; private set; }

    public ulong ElapsedFrames { get; private set; }

    public void Update(double deltaTime) {
        DeltaTimeF64 = deltaTime;
        ElapsedTimeF64 += deltaTime;

        DeltaTime = (float)DeltaTimeF64;
        ElapsedTime = (float)ElapsedTimeF64;

        ElapsedFrames++;
    }

    internal void Reset() {
        DeltaTime = 0;
        ElapsedTime = 0;
        ElapsedFrames = 0;
    }
}
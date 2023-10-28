namespace RiptideEditor;

public delegate void PlaymodePauseCallback(bool isPaused);

public static class PlaymodeProcedure {
    public static event Action? OnBeginPlaymode;
    public static event PlaymodePauseCallback? OnPause;
    public static event Action? OnStopPlaymode;

    public static bool IsInPlaymode { get; private set; }
    public static bool IsPaused { get; private set; }

    internal static void Begin() {
        if (IsInPlaymode) return;

        IsInPlaymode = true;
        OnBeginPlaymode?.Invoke();
    }

    internal static void Pause() {
        if (IsPaused) return;

        IsPaused = true;
        OnPause?.Invoke(true);
    }

    internal static void Unpause() {
        if (!IsPaused) return;

        IsPaused = false;
        OnPause?.Invoke(false);
    }

    internal static void Stop() {
        if (!IsInPlaymode) return;

        IsInPlaymode = false;
        OnStopPlaymode?.Invoke();
    }
}
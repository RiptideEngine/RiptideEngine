namespace RiptideEngine.Audio;

internal sealed unsafe class StreamingCallback {
    private readonly record struct CallbackEntry(uint Source, Action<int> StreamCallback, Action StopCallback);
    private static readonly List<CallbackEntry> _callbackEntries;

    static StreamingCallback() {
        _callbackEntries = new();
    }

    public static void InjectCallbackEvent(AL al) {
        int* eventTypes = stackalloc int[] { 0x19A4, 0x19A5, };
        ((delegate* unmanaged[Cdecl]<int, int*, byte, void>)al.GetProcAddress("alEventControlSOFT"))(2, eventTypes, 1);

        ((delegate* unmanaged[Cdecl]<delegate* unmanaged[Cdecl]<uint, uint, uint, int, byte*, void*, void>, void*, void>)al.GetProcAddress("alEventCallbackSOFT"))(&Callback, null);
    }

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvCdecl) })]
    private static void Callback(uint type, uint obj, uint param, int length, byte* pMessage, void* userParam) {
        switch (type) {
            case 0x19A4: // AL_EVENT_TYPE_BUFFER_COMPLETED_SOFT
                foreach (ref readonly var entry in CollectionsMarshal.AsSpan(_callbackEntries)) {
                    if (entry.Source != obj) continue;

                    entry.StreamCallback((int)param);
                    break;
                }
                break;

            case 0x19A5: // AL_EVENT_TYPE_SOURCE_STATE_CHANGED_SOFT
                if ((SourceState)param == SourceState.Playing) break;

                int idx = 0;
                foreach (ref readonly var entry in CollectionsMarshal.AsSpan(_callbackEntries)) {
                    if (entry.Source != obj) {
                        idx++;
                        continue;
                    }

                    entry.StopCallback();
                    _callbackEntries.RemoveAt(idx);
                    break;
                }
                break;
        }
    }

    public static void CleanAllCallbacks() => _callbackEntries.Clear();
    public static void RegisterCallback(uint source, Action<int> streamCallback, Action stopStreamCallback) {
        _callbackEntries.Add(new(source, streamCallback, stopStreamCallback));
        Console.WriteLine("Registered");
    }
}
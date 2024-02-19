namespace RiptideEngine.Audio;

// TODO: Convert to context based to support multiple APIs.

public static unsafe class AudioEngine {
    internal const int StreamingBufferCount = 3;
    internal const int StreamingBufferSize = 65536;

    internal static AL? AL { get; private set; }
    internal static ALContext? ALC { get; private set; }

    private static Device* pDevice;
    private static Context* pContext;

    public static event SourceCompleteBufferCallback? OnBufferCompleted;
    public static event SourceStateChangedCallback? OnStateChanged;
    
    public static void Initialize() {
        Debug.Assert(AL == null);

        try {
            AL = AL.GetApi(true);
            ALC = ALContext.GetApi(true);
            
            pDevice = ALC.OpenDevice(null);
            if (pDevice == null) throw new($"Failed to open default OpenAL device.");

            int* attributes = stackalloc int[] {
                0,
            };

            pContext = ALC.CreateContext(pDevice, attributes);
            if (pContext == null) throw new($"Failed to create OpenAL context ({ALC.GetError(pDevice)}).");
            
            if (!ALC.MakeContextCurrent(pContext)) throw new($"Failed to make created context a current context.");

            foreach (string extension in new[] { "AL_SOFTX_map_buffer", "AL_SOFT_events", "AL_SOFT_buffer_length_query" }) {
                if (!AL.IsExtensionPresent(extension)) {
                    throw new PlatformNotSupportedException($"The required extension '{extension}' is not present.");
                }
            }

            AudioCapability.DoCapabilityCheck();

            OALBufferMapping.GetFunctionPointers(AL);
        } catch {
            Shutdown();
            throw;
        }
        
        int* eventTypes = stackalloc int[] { (int)EventType.BufferCompleted, (int)EventType.SourceStateChanged };
        ((delegate* unmanaged[Cdecl]<int, int*, byte, void>)AL.GetProcAddress("alEventControlSOFT"))(2, eventTypes, 1);
        ((delegate* unmanaged[Cdecl]<delegate* unmanaged[Cdecl]<uint, uint, uint, int, byte*, void*, void>, void*, void>)AL.GetProcAddress("alEventCallbackSOFT"))(&Callback, null);
    }

    public static void Shutdown() {
        if (ALC != null) {
            ALC.DestroyContext(pContext); pContext = null;
            ALC.CloseDevice(pDevice); pDevice = null;
        }

        DisposeAPIObjects();
    }

    private static void DisposeAPIObjects() {
        ALC?.Dispose(); ALC = null;
        AL?.Dispose(); AL = null;
    }

    internal static void EnsureInitialized() {
        if (pDevice == null)
            throw new InvalidOperationException(ExceptionMessages.UninitializedEngine);
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void Callback(uint type, uint obj, uint param, int length, byte* pMessage, void* userParam) {
        switch ((EventType)type) {
            case EventType.BufferCompleted:
                OnBufferCompleted?.Invoke(obj, param);
                break;

            case EventType.SourceStateChanged:
                OnStateChanged?.Invoke(obj, (SourceState)param);
                break;
            
            case EventType.Disconnected:
                // Unused.
                break;
        }
    }

    internal static bool IsInitialized() => pDevice != null;
}

internal enum EventType {
    BufferCompleted = 0x19A4,
    SourceStateChanged = 0x19A5,
    Disconnected = 0x19A6,
}

public delegate void SourceCompleteBufferCallback(uint Source, uint NumBuffers);
public delegate void SourceStateChangedCallback(uint Source, SourceState NewState);
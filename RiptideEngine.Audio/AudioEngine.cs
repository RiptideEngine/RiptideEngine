namespace RiptideEngine.Audio;

public static unsafe class AudioEngine {
    internal const int StreamingBufferCount = 3;
    internal const int StreamingBufferSize = 65_536;

    internal static AL? AL { get; private set; }
    internal static ALContext? ALC { get; private set; }

    private static Device* pDevice;
    private static Context* pContext;

    public static void Initialize() {
        Debug.Assert(AL == null);

        try {
            AL = AL.GetApi(true);
            ALC = ALContext.GetApi(true);
            
            pDevice = ALC.OpenDevice(null);
            if (pDevice == null) throw new Exception($"Failed to open default OpenAL device.");

            pContext = ALC.CreateContext(pDevice, null);
            if (pContext == null) throw new Exception($"Failed to create OpenAL context ({ALC.GetError(pDevice)}).");

            if (!ALC.MakeContextCurrent(pContext)) throw new Exception($"Failed to make created context a current context.");

            string[] requireExtensions = new string[] {
                "AL_SOFTX_map_buffer",
                "AL_SOFT_events",
                "AL_SOFT_buffer_length_query"
            };

            foreach (string extension in requireExtensions) {
                if (!AL.IsExtensionPresent(extension)) {
                    throw new PlatformNotSupportedException($"{extension} extension cannot be found.");
                }
            }

            AudioCapability.DoCapabilityCheck();

            OALBufferMapping.GetFunctionPointers(AL);
            StreamingCallback.InjectCallbackEvent(AL);
        } catch {
            Shutdown();
            throw;
        }
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

    [Conditional("DEBUG"), StackTraceHidden]
    internal static void AssertNoError() {
        var error = AL!.GetError();
        Debug.Assert(error == AudioError.NoError, $"OpenAL error: {error}.");
    }

    internal static bool IsInitialized() => pDevice != null;
}
namespace RiptideRendering.Direct3D12;

internal unsafe class Debugger : IDisposable {
    private static readonly StringBuilder _messageBuilder = new(256);

    private ComPtr<ID3D12InfoQueue1> pQueue;
    private ComPtr<ID3D12DebugDevice2> pDebugDevice;
    private uint _callbackCookie;

    public bool IsValid => pQueue.Handle != null;

    public Debugger(ID3D12Device* pDevice) {
        if (pDevice->QueryInterface(SilkMarshal.GuidPtrOf<ID3D12InfoQueue1>(), (void**)pQueue.GetAddressOf()) >= 0) {
            pDevice->QueryInterface(SilkMarshal.GuidPtrOf<ID3D12DebugDevice2>(), (void**)pDebugDevice.GetAddressOf());

            MessageID* denyIDs = stackalloc MessageID[] {
                MessageID.ClearrendertargetviewMismatchingclearvalue,
                MessageID.CleardepthstencilviewMismatchingclearvalue,
            };

            var filter = new D3D12InfoQueueFilter() {
                DenyList = new() {
                    NumIDs = 2,
                    PIDList = denyIDs,
                },
            };

            pQueue.AddStorageFilterEntries(&filter);

            pQueue.RegisterMessageCallback(new(&MessageCallback), MessageCallbackFlags.FlagNone, null, ref _callbackCookie);
        } else {
            Console.WriteLine("Failed to initialize debugger.");
        }
    }

    public void ReportLiveD3D12Objects() {
        if (pDebugDevice.Handle == null) return;

        pDebugDevice.ReportLiveDeviceObjects(RldoFlags.Summary | RldoFlags.Detail | RldoFlags.IgnoreInternal);
    }

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvCdecl) })]
    private static void MessageCallback(MessageCategory category, MessageSeverity severity, MessageID id, byte* pMessage, void* pContext) {
        var oldColor = Console.ForegroundColor;

        switch (severity) {
            case MessageSeverity.Corruption:
            case MessageSeverity.Error:
                Console.ForegroundColor = ConsoleColor.Red;
                break;

            case MessageSeverity.Warning:
                Console.ForegroundColor = ConsoleColor.Yellow;
                break;

            case MessageSeverity.Message:
            case MessageSeverity.Info:
                Console.ForegroundColor = ConsoleColor.White;
                break;
        }

        _messageBuilder.Clear();

        var categoryMessage = typeof(MessageCategory).GetField(category.ToString())!.GetCustomAttributes<NativeNameAttribute>().First(x => x.Category == "Name").Name;
        var idMessage = typeof(MessageID).GetField(id.ToString())!.GetCustomAttributes<NativeNameAttribute>().First(x => x.Category == "Name").Name;

        _messageBuilder.Append('[').Append(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")).Append(']').Append(' ');
        _messageBuilder.Append(categoryMessage).Append(" - ").Append(idMessage).Append(": ").Append(Marshal.PtrToStringAnsi((nint)pMessage));

        Console.WriteLine(_messageBuilder.ToString());

        Console.ForegroundColor = oldColor;
    }

    public void Dispose() {
        if (pQueue.Handle == null) return;

        pQueue.UnregisterMessageCallback(_callbackCookie); _callbackCookie = 0;

        pQueue.Dispose(); pQueue = default;
        pDebugDevice.Dispose(); pDebugDevice = default;

        GC.SuppressFinalize(this);
    }

    ~Debugger() {
        Dispose();
    }
}
namespace RiptideRendering.Direct3D12;

internal unsafe class Debugger : IDisposable {
    private static readonly StringBuilder _messageBuilder = new(256);

    private ComPtr<ID3D12InfoQueue1> pQueue;
    private ComPtr<ID3D12DebugDevice2> pDebugDevice;
    private uint _callbackCookie;

    public bool IsValid => pQueue.Handle != null;

    private D3D12RenderingContext _context;

    private MessageFunc _errorDelegate;

    public Debugger(D3D12RenderingContext context) {
        if (context.Device->QueryInterface(SilkMarshal.GuidPtrOf<ID3D12InfoQueue1>(), (void**)pQueue.GetAddressOf()) >= 0) {
            context.Device->QueryInterface(SilkMarshal.GuidPtrOf<ID3D12DebugDevice2>(), (void**)pDebugDevice.GetAddressOf());

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

            _errorDelegate = ErrorCallback;
            pQueue.RegisterMessageCallback(_errorDelegate, MessageCallbackFlags.FlagNone, null, ref _callbackCookie);

            _context = context;

        } else {
            Console.WriteLine("Failed to initialize debugger.");

            _context = null!;
            _errorDelegate = null!;
        }
    }

    public void ReportLiveD3D12Objects() {
        if (pDebugDevice.Handle == null) return;

        pDebugDevice.ReportLiveDeviceObjects(RldoFlags.Summary | RldoFlags.Detail | RldoFlags.IgnoreInternal);
    }

    private void ErrorCallback(MessageCategory category, MessageSeverity severity, MessageID id, byte* pMessage, void* pContext) {
        if (_context.Logger is not { } logger) return;

        var type = severity switch {
            MessageSeverity.Corruption or MessageSeverity.Error => LoggingType.Error,
            MessageSeverity.Warning => LoggingType.Warning,
            _ => LoggingType.Info,
        };

        _messageBuilder.Clear();
        
        var categoryString = typeof(MessageCategory).GetField(category.ToString())!.GetCustomAttributes<NativeNameAttribute>().First(x => x.Category == "Name").Name;
        var idString = typeof(MessageID).GetField(id.ToString())!.GetCustomAttributes<NativeNameAttribute>().First(x => x.Category == "Name").Name;
        
        _messageBuilder.Append(categoryString).Append(" - ").Append(idString).Append(": ").Append(Marshal.PtrToStringAnsi((nint)pMessage));

        logger.Log(type, _messageBuilder.ToString());
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
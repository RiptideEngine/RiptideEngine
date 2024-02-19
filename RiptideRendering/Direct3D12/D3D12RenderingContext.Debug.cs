using Silk.NET.Direct3D12;

namespace RiptideRendering.Direct3D12;

unsafe partial class D3D12RenderingContext {
    private static readonly StringBuilder _messageBuilder = new(256);
    
    private ComPtr<ID3D12InfoQueue1> pQueue;
    private ComPtr<ID3D12DebugDevice2> pDebugDevice;
    private uint _callbackCookie;
    
    private MessageFunc _callback = null!;

    private void InitializeDebugLayer() {
        using ComPtr<ID3D12Debug> pDebug = default;
        if (D3D12.GetDebugInterface(SilkMarshal.GuidPtrOf<ID3D12Debug>(), (void**)&pDebug) >= 0) {
            pDebug.EnableDebugLayer();

            using ComPtr<ID3D12Debug1> pDebug1 = default;
            if (pDebug.QueryInterface(SilkMarshal.GuidPtrOf<ID3D12Debug1>(), (void**)&pDebug1) >= 0) {
                pDebug1.SetEnableGPUBasedValidation(true);
            }
            
            Logger?.Log(LoggingType.Info, "Direct3D12: Debug layer enabled.");
        } else {
            Logger?.Log(LoggingType.Info, "Direct3D12: Debug layer disabled.");
        }
    }
    
    private void InitializeDebugMessageCallback() {
        if (pDevice.QueryInterface(SilkMarshal.GuidPtrOf<ID3D12InfoQueue1>(), (void**)pQueue.GetAddressOf()) >= 0) {
            pDevice.QueryInterface(SilkMarshal.GuidPtrOf<ID3D12DebugDevice2>(), (void**)pDebugDevice.GetAddressOf());

            MessageID* denyIDs = stackalloc MessageID[] {
                MessageID.ClearrendertargetviewMismatchingclearvalue,
                MessageID.CleardepthstencilviewMismatchingclearvalue,
            };

            var filter = new InfoQueueFilter {
                DenyList = new() {
                    NumIDs = 2,
                    PIDList = denyIDs,
                },
            };

            pQueue.AddStorageFilterEntries(&filter);

            _callback = ErrorCallback;
            pQueue.RegisterMessageCallback(_callback, MessageCallbackFlags.FlagNone, null, ref _callbackCookie);
            
            Logger?.Log(LoggingType.Info, "D3D12RenderingContext: Debug Layer initialized.");
        } else {
            Logger?.Log(LoggingType.Error, "D3D12RenderingContext: Failed to initialize debug layer.");
        }
    }

    private void ShutdownDebugLayer() {
        if (pQueue.Handle == null) return;

        pDebugDevice.Handle->ReportLiveDeviceObjects(RldoFlags.Detail | RldoFlags.IgnoreInternal | RldoFlags.Summary);

        pQueue.UnregisterMessageCallback(_callbackCookie);
        
        pQueue.Dispose();
        pDebugDevice.Dispose();
    }
    
    private void ErrorCallback(MessageCategory category, MessageSeverity severity, MessageID id, byte* pMessage, void* pContext) {
        if (Logger is not { } logger) return;

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
}
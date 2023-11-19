﻿using RiptideRendering.Shadering;
using Silk.NET.Input;

namespace RiptideEditor;

internal unsafe class ImGuiController : IDisposable {
    private nint pContext;

    private GpuResource _vertexBuffer = null!;
    private ResourceView _vertexBufferView = null!;
    private GpuResource _indexBuffer = null!;

    private ResourceSignature _resourceSignature = null!;

    private Texture2D _fontTexture = null!;
    private GraphicalShader _shader = null!;
    private PipelineState _pipelineState = null!;

    private BaseRenderingContext _context;

    private readonly IInputService _input;

    private ImFontPtr _fontNormal, _fontBold;

    public ImFontPtr FontNormal => _fontNormal;
    public ImFontPtr FontTitle1 => _fontBold;

    public ImGuiController(RiptideServices services, Vector2 displaySize) {
        _context = services.GetRequiredService<IRenderingService>().Context;
        _input = services.GetRequiredService<IInputService>();

        _input.KeyChar += KeyCharacterInput;
        _input.KeyUp += KeyEventUp;
        _input.KeyDown += KeyEventDown;

        _input.MouseScroll += MouseScroll;

        pContext = ImGui.CreateContext();
        ImGui.SetCurrentContext(pContext);

        var io = ImGui.GetIO();
        io.DisplaySize = displaySize;

        io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;
        io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard | ImGuiConfigFlags.DockingEnable;
        io.Fonts.Flags |= ImFontAtlasFlags.NoBakedLines;

        CreateResources();
    }

    private readonly record struct Vertex(Vector2 Position, Vector2 TexCoord, uint Color);

    private void CreateResources() {
        var factory = _context.Factory;

        // Create mesh resources
        _vertexBuffer = factory.CreateResource(new() {
            Dimension = ResourceDimension.Buffer,
            Width = 10000 * (uint)sizeof(Vertex),
            DepthOrArraySize = 1,
            Height = 1,
        });
        _vertexBuffer.Name = "ImGui Vertex Buffer";

        _vertexBufferView = factory.CreateResourceView(_vertexBuffer, new() {
            Dimension = ResourceViewDimension.Buffer,
            Format = GraphicsFormat.Unknown,
            Buffer = new() {
                FirstElement = 0,
                NumElements = 10000,
                StructureSize = (uint)sizeof(Vertex),
                IsRawBuffer = false,
            },
        });
        _vertexBufferView.Name = "ImGui Vertex Buffer View";

        _indexBuffer = factory.CreateResource(new() {
            Dimension = ResourceDimension.Buffer,
            Width = 2500 * sizeof(ushort),
            DepthOrArraySize = 1,
            Height = 1,
        });
        _indexBuffer.Name = "ImGui Index Buffer";

        // Create font resources
        {
            var io = ImGui.GetIO();

            _fontNormal = io.Fonts.AddFontFromFileTTF(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "font.ttf"), 18);
            if (_fontNormal.NativePtr == null) throw new Exception("Failed to load font.");
            
            _fontBold = io.Fonts.AddFontFromFileTTF(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "font_bold.ttf"), 36);
            if (_fontBold.NativePtr == null) throw new Exception("Failed to load font.");

            io.Fonts.GetTexDataAsRGBA32(out byte* pFontPixels, out int width, out int height);

            _fontTexture = new((ushort)width, (ushort)height, GraphicsFormat.R8G8B8A8UNorm) {
                Name = "ImGui Font Texture"
            };
            CommandList initializerList = factory.CreateCommandList();

            io.Fonts.SetTexID((nint)_fontTexture.UnderlyingView.NativeView.Handle);

            ResourceTransitionDescriptor barrier = new(_fontTexture.UnderlyingTexture, ResourceStates.Common, ResourceStates.CopyDestination);
            initializerList.TranslateResourceStates([
                new(_fontTexture.UnderlyingTexture, ResourceStates.Common, ResourceStates.CopyDestination),
            ]);

            initializerList.UpdateResource(_fontTexture.UnderlyingTexture, new(pFontPixels, width * height * 4));

            initializerList.TranslateResourceStates([
                new(_fontTexture.UnderlyingTexture, ResourceStates.CopyDestination, ResourceStates.ShaderResource),
                new(_vertexBuffer, ResourceStates.Common, ResourceStates.CopyDestination),
                new(_indexBuffer, ResourceStates.Common, ResourceStates.CopyDestination),
            ]);

            initializerList.Close();
            _context.ExecuteCommandList(initializerList);
            _context.WaitForGpuIdle();

            initializerList.DecrementReference();

            io.Fonts.ClearTexData();
        }

        // Create shader
        {
            using ComPtr<IDxcBlob> pVSBlob = default;
            using ComPtr<IDxcBlob> pPSBlob = default;

            var source = File.ReadAllBytes(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "ImGuiShader.hlsl"));

            ShaderCompileUtils.CompileShader(source, "vs_6_0\0", "vsmain\0", "-O3", default, pVSBlob.GetAddressOf());
            ShaderCompileUtils.CompileShader(source, "ps_6_0\0", "psmain\0", "-O3", default, pPSBlob.GetAddressOf());

            var rdesc = new RasterizationConfig() {
                FillMode = FillingMode.Solid,
                CullMode = CullingMode.None,
                Conservative = false,
            };
            var depth = new DepthStencilConfig() {
                EnableDepth = false,
                EnableStencil = false,
                DepthComparison = ComparisonOperator.Always,
                FrontFaceOperation = new() {
                    FailOp = StencilOperation.Keep,
                    DepthFailOp = StencilOperation.Keep,
                    PassOp = StencilOperation.Keep,
                    CompareOp = ComparisonOperator.Always,
                },
                BackfaceOperation = new() {
                    FailOp = StencilOperation.Keep,
                    DepthFailOp = StencilOperation.Keep,
                    PassOp = StencilOperation.Keep,
                    CompareOp = ComparisonOperator.Always,
                },
            };
            var blend = BlendingConfig.CreateNonIndependent(new() {
                EnableBlend = true,

                Source = BlendFactor.SourceAlpha,
                Dest = BlendFactor.InvertSourceAlpha,
                Operator = BlendOperator.Add,
                SourceAlpha = BlendFactor.One,
                DestAlpha = BlendFactor.InvertSourceAlpha,
                AlphaOperator = BlendOperator.Add,
                WriteMask = RenderTargetWriteMask.All,
            });

            _resourceSignature = factory.CreateResourceSignature(new() {
                Parameters = [
                    new() {
                        Type = ResourceParameterType.Constants,
                        Constants = new() {
                            NumConstants = 17,
                            Register = 0,
                            Space = 0,
                        },
                    },
                    // VertexBuffer
                    new() {
                        Type = ResourceParameterType.Table,
                        Table = new() {
                            Ranges = [
                                new() {
                                    Type = ResourceRangeType.ResourceView,
                                    NumResources = 1,
                                    BaseRegister = 0,
                                    Space = 0,
                                },
                            ],
                        },
                    },
                    // Texture
                    new() {
                        Type = ResourceParameterType.Table,
                        Table = new() {
                            Ranges = [
                                new() {
                                    Type = ResourceRangeType.ResourceView,
                                    NumResources = 1,
                                    BaseRegister = 1,
                                    Space = 0,
                                },
                            ],
                        },
                    },
                ],

                ImmutableSamplers = [
                    new() {
                        AddressU = TextureAddressingMode.Wrap,
                        AddressV = TextureAddressingMode.Wrap,
                        AddressW = TextureAddressingMode.Wrap,
                        Filter = SamplerFilter.Linear,
                        ComparisonOp = ComparisonOperator.Always,
                        MipLodBias = 0,
                        MaxAnisotropy = 0,
                        MinLod = 0,
                        MaxLod = 0,
                        Register = 0,
                        Space = 0,
                    }
                ],
            });

            _shader = factory.CreateGraphicalShader(pVSBlob.AsSpan(), pPSBlob.AsSpan(), default, default);

            RenderTargetFormats formats = default;
            formats[0] = GraphicsFormat.R8G8B8A8UNorm;
            _pipelineState = factory.CreatePipelineState(_shader, _resourceSignature, new() {
                Rasterization = rdesc,
                Blending = blend,
                DepthStencil = depth,
                RenderTargetFormats = new() {
                    NumRenderTargets = 1,
                    Formats = formats,
                },
                DepthFormat = GraphicsFormat.Unknown,
                PrimitiveTopology = PipelinePrimitiveTopology.Triangle,
            });
        }
    }

    private void KeyCharacterInput(char character) {
        ImGui.GetIO().AddInputCharacterUTF16(character);
    }

    private void KeyEventUp(Key key) {
        if (TryMapKey(key, out var imguiKey)) {
            ImGui.GetIO().AddKeyEvent(imguiKey, false);
        }
    }
    private void KeyEventDown(Key key) {
        if (TryMapKey(key, out var imguiKey)) {
            ImGui.GetIO().AddKeyEvent(imguiKey, true);
        }
    }
    private void MouseScroll(ScrollWheel scroll) {
        ImGui.GetIO().AddMouseWheelEvent(scroll.X, scroll.Y);
    }

    public void Update(float deltaTime) {
        var io = ImGui.GetIO();

        io.DisplayFramebufferScale = Vector2.One;
        io.DeltaTime = deltaTime;
        io.DisplaySize = new Vector2(EditorApplication.MainWindow.Size.X, EditorApplication.MainWindow.Size.Y);
        
        // Update input events
        var mpos = _input.MousePosition;
        io.AddMousePosEvent(mpos.X, mpos.Y);

        io.AddMouseButtonEvent(0, _input.IsHolding(MouseButton.Left));
        io.AddMouseButtonEvent(1, _input.IsHolding(MouseButton.Right));
        io.AddMouseButtonEvent(2, _input.IsHolding(MouseButton.Middle));
        //io.AddMouseButtonEvent(3, _input.IsMouseHolding(MouseButton.Button4));
        //io.AddMouseButtonEvent(4, _input.IsMouseHolding(MouseButton.Button5));

        io.AddKeyEvent(ImGuiKey.ModShift, _input.IsHolding(Key.ShiftLeft) || _input.IsHolding(Key.ShiftRight));
        io.AddKeyEvent(ImGuiKey.ModAlt, _input.IsHolding(Key.AltLeft) || _input.IsHolding(Key.AltRight));
        io.AddKeyEvent(ImGuiKey.ModCtrl, _input.IsHolding(Key.ControlLeft) || _input.IsHolding(Key.ControlRight));
        io.AddKeyEvent(ImGuiKey.ModSuper, _input.IsHolding(Key.SuperLeft) || _input.IsHolding(Key.SuperRight));

        // New frame
        ImGui.NewFrame();
    }

    private static bool TryMapKey(Key key, out ImGuiKey outputKey) {
        switch (key) {
            case Key.Menu: outputKey = ImGuiKey.Menu; return true;
            case Key.ShiftLeft: outputKey = ImGuiKey.LeftShift; return true;
            case Key.ShiftRight: outputKey = ImGuiKey.RightShift; return true;
            case Key.ControlLeft: outputKey = ImGuiKey.LeftCtrl; return true;
            case Key.ControlRight: outputKey = ImGuiKey.RightCtrl; return true;
            case Key.AltLeft: outputKey = ImGuiKey.LeftAlt; return true;
            case Key.AltRight: outputKey = ImGuiKey.RightAlt; return true;
            case Key.SuperLeft: outputKey = ImGuiKey.LeftSuper; return true;
            case Key.SuperRight: outputKey = ImGuiKey.RightSuper; return true;
            case Key.Up: outputKey = ImGuiKey.UpArrow; return true;
            case Key.Down: outputKey = ImGuiKey.DownArrow; return true;
            case Key.Left: outputKey = ImGuiKey.LeftArrow; return true;
            case Key.Right: outputKey = ImGuiKey.RightArrow; return true;
            case Key.Enter: outputKey = ImGuiKey.Enter; return true;
            case Key.Escape: outputKey = ImGuiKey.Escape; return true;
            case Key.Space: outputKey = ImGuiKey.Space; return true;
            case Key.Tab: outputKey = ImGuiKey.Tab; return true;
            case Key.Backspace: outputKey = ImGuiKey.Backspace; return true;
            case Key.Insert: outputKey = ImGuiKey.Insert; return true;
            case Key.Delete: outputKey = ImGuiKey.Delete; return true;
            case Key.PageUp: outputKey = ImGuiKey.PageUp; return true;
            case Key.PageDown: outputKey = ImGuiKey.PageDown; return true;
            case Key.Home: outputKey = ImGuiKey.Home; return true;
            case Key.End: outputKey = ImGuiKey.End; return true;
            case Key.CapsLock: outputKey = ImGuiKey.CapsLock; return true;
            case Key.ScrollLock: outputKey = ImGuiKey.ScrollLock; return true;
            case Key.PrintScreen: outputKey = ImGuiKey.PrintScreen; return true;
            case Key.Pause: outputKey = ImGuiKey.Pause; return true;
            case Key.NumLock: outputKey = ImGuiKey.NumLock; return true;
            case Key.KeypadDivide: outputKey = ImGuiKey.KeypadDivide; return true;
            case Key.KeypadMultiply: outputKey = ImGuiKey.KeypadMultiply; return true;
            case Key.KeypadSubtract: outputKey = ImGuiKey.KeypadSubtract; return true;
            case Key.KeypadAdd: outputKey = ImGuiKey.KeypadAdd; return true;
            case Key.KeypadDecimal: outputKey = ImGuiKey.KeypadDecimal; return true;
            case Key.KeypadEnter: outputKey = ImGuiKey.KeypadEnter; return true;
            case Key.GraveAccent: outputKey = ImGuiKey.GraveAccent; return true;
            case Key.Minus: outputKey = ImGuiKey.Minus; return true;
            case Key.Equal: outputKey = ImGuiKey.Equal; return true;
            case Key.LeftBracket: outputKey = ImGuiKey.LeftBracket; return true;
            case Key.RightBracket: outputKey = ImGuiKey.RightBracket; return true;
            case Key.Semicolon: outputKey = ImGuiKey.Semicolon; return true;
            case Key.Apostrophe: outputKey = ImGuiKey.Apostrophe; return true;
            case Key.Comma: outputKey = ImGuiKey.Comma; return true;
            case Key.Period: outputKey = ImGuiKey.Period; return true;
            case Key.Slash: outputKey = ImGuiKey.Slash; return true;
            case Key.BackSlash: outputKey = ImGuiKey.Backslash; return true;

            case Key.F1: outputKey = ImGuiKey.F1; return true;
            case Key.F2: outputKey = ImGuiKey.F2; return true;
            case Key.F3: outputKey = ImGuiKey.F3; return true;
            case Key.F4: outputKey = ImGuiKey.F4; return true;
            case Key.F5: outputKey = ImGuiKey.F5; return true;
            case Key.F6: outputKey = ImGuiKey.F6; return true;
            case Key.F7: outputKey = ImGuiKey.F7; return true;
            case Key.F8: outputKey = ImGuiKey.F8; return true;
            case Key.F9: outputKey = ImGuiKey.F9; return true;
            case Key.F10: outputKey = ImGuiKey.F10; return true;
            case Key.F11: outputKey = ImGuiKey.F11; return true;
            case Key.F12: outputKey = ImGuiKey.F12; return true;

            case Key.Keypad0: outputKey = ImGuiKey.Keypad0; return true;
            case Key.Keypad1: outputKey = ImGuiKey.Keypad1; return true;
            case Key.Keypad2: outputKey = ImGuiKey.Keypad2; return true;
            case Key.Keypad3: outputKey = ImGuiKey.Keypad3; return true;
            case Key.Keypad4: outputKey = ImGuiKey.Keypad4; return true;
            case Key.Keypad5: outputKey = ImGuiKey.Keypad5; return true;
            case Key.Keypad6: outputKey = ImGuiKey.Keypad6; return true;
            case Key.Keypad7: outputKey = ImGuiKey.Keypad7; return true;
            case Key.Keypad8: outputKey = ImGuiKey.Keypad8; return true;
            case Key.Keypad9: outputKey = ImGuiKey.Keypad9; return true;

            case Key.Number0: outputKey = ImGuiKey._0; return true;
            case Key.Number1: outputKey = ImGuiKey._1; return true;
            case Key.Number2: outputKey = ImGuiKey._2; return true;
            case Key.Number3: outputKey = ImGuiKey._3; return true;
            case Key.Number4: outputKey = ImGuiKey._4; return true;
            case Key.Number5: outputKey = ImGuiKey._5; return true;
            case Key.Number6: outputKey = ImGuiKey._6; return true;
            case Key.Number7: outputKey = ImGuiKey._7; return true;
            case Key.Number8: outputKey = ImGuiKey._8; return true;
            case Key.Number9: outputKey = ImGuiKey._9; return true;

            default:
                if (key >= Key.A && key <= Key.Z) {
                    outputKey = ImGuiKey.A + (key - Key.A);
                    return true;
                }
                break;
        }

        outputKey = ImGuiKey.None;
        return false;
    }

    private void SetupRenderState(ImDrawDataPtr data, CommandList cmdList) {
        var io = ImGui.GetIO();

        Matrix4x4 projection = Matrix4x4.CreateOrthographicOffCenter(0, io.DisplaySize.X, io.DisplaySize.Y, 0, 0, 1000);

        cmdList.SetPipelineState(_pipelineState);
        cmdList.SetGraphicsResourceSignature(_resourceSignature);
        cmdList.SetGraphicsConstants(0, MemoryMarshal.Cast<Matrix4x4, uint>(new(&projection, 1)), 0);
        cmdList.SetGraphicsResourceView(1, 0, _vertexBufferView);
        cmdList.SetPrimitiveTopology(RenderingPrimitiveTopology.TriangleList);
        cmdList.SetIndexBuffer(_indexBuffer, IndexFormat.UInt16, 0);
        cmdList.SetViewport(new(0, 0, io.DisplaySize.X, io.DisplaySize.Y));

        data.ScaleClipRects(io.DisplayFramebufferScale);
    }

    public void Render(CommandList cmdList) {
        ImGui.Render();
        var data = ImGui.GetDrawData();

        if (data.CmdListsCount == 0) return;

        var bufferDesc = _vertexBuffer.Descriptor;
        if ((ulong)data.TotalVtxCount * (ulong)sizeof(Vertex) > bufferDesc.Width) {
            _vertexBuffer.DecrementReference();
            _vertexBuffer = _context.Factory.CreateResource(bufferDesc with  {
                Width = (ulong)sizeof(Vertex) * (ulong)data.TotalVtxCount * 2,
            });
            _vertexBuffer.Name = "ImGui Vertex Buffer";
        }

        bufferDesc = _indexBuffer.Descriptor;
        if ((ulong)data.TotalIdxCount * sizeof(ushort) > bufferDesc.Width) {
            _indexBuffer.DecrementReference();
            _indexBuffer = _context.Factory.CreateResource(bufferDesc with {
                Width = sizeof(ushort) * (ulong)data.TotalIdxCount * 2,
            });
            _indexBuffer.Name = "ImGui Index Buffer";
        }

        uint vertexOffset = 0;
        uint indexOffset = 0;
        for (int i = 0; i < data.CmdListsCount; i++) {
            var cl = data.CmdLists[i];

            var vertexData = MemoryMarshal.AsBytes(new ReadOnlySpan<Vertex>((void*)cl.VtxBuffer.Data, cl.VtxBuffer.Size));
            var indexData = MemoryMarshal.AsBytes(new ReadOnlySpan<ushort>((void*)cl.IdxBuffer.Data, cl.IdxBuffer.Size));

            cmdList.UpdateBufferRegion(_vertexBuffer, vertexOffset * (uint)sizeof(Vertex), vertexData);
            cmdList.UpdateBufferRegion(_indexBuffer, indexOffset * sizeof(ushort), indexData);

            vertexOffset += (uint)cl.VtxBuffer.Size;
            indexOffset += (uint)cl.IdxBuffer.Size;
        }

        cmdList.TranslateResourceStates([
            new(_vertexBuffer, ResourceStates.CopyDestination, ResourceStates.ShaderResource),
            new(_indexBuffer, ResourceStates.CopyDestination, ResourceStates.IndexBuffer),
        ]);

        SetupRenderState(data, cmdList);

        vertexOffset = indexOffset = 0;
        var clipOffset = data.DisplayPos;

        for (int i = 0; i < data.CmdListsCount; i++) {
            ImDrawListPtr drawList = data.CmdLists[i];

            for (int j = 0, c = drawList.CmdBuffer.Size; j < c; j++) {
                var pcmd = drawList.CmdBuffer[j];

                if (pcmd.UserCallback != nint.Zero) {
                    if (pcmd.UserCallback == -1) {
                        SetupRenderState(data, cmdList);
                    } else {
                        if (ImGuiCallbackRegister.TryGetCallback(pcmd.UserCallback, out var entry)) {
                            entry.Callback.Invoke(entry.Parameter);
                        }
                    }
                } else {
                    Vector2 clipMin = new(pcmd.ClipRect.X - clipOffset.X, pcmd.ClipRect.Y - clipOffset.Y);
                    Vector2 clipMax = new(pcmd.ClipRect.Z - clipOffset.X, pcmd.ClipRect.W - clipOffset.Y);
                    if (clipMax.X <= clipMin.X || clipMax.Y <= clipMin.Y) continue;

                    cmdList.SetGraphicsResourceView(2, 0, new((ulong)pcmd.GetTexID()));
                    cmdList.SetScissorRect(new((int)clipMin.X, (int)clipMin.Y, (int)clipMax.X, (int)clipMax.Y));

                    uint vtxOffset = pcmd.VtxOffset + vertexOffset;

                    cmdList.SetGraphicsConstants(0, new(&vtxOffset, 1), 16);
                    cmdList.DrawIndexed(pcmd.ElemCount, 1, pcmd.IdxOffset + indexOffset, 0);
                }
            }

            vertexOffset += (uint)drawList.VtxBuffer.Size;
            indexOffset += (uint)drawList.IdxBuffer.Size;
        }

        cmdList.TranslateResourceStates([
            new(_vertexBuffer, ResourceStates.ShaderResource, ResourceStates.CopyDestination),
            new(_indexBuffer, ResourceStates.IndexBuffer, ResourceStates.CopyDestination),
        ]);
    }

    protected virtual void Dispose(bool disposing) {
        if (pContext == nint.Zero) return;

        if (disposing) { }

        _vertexBuffer.DecrementReference(); _vertexBuffer = null!;
        _indexBuffer.DecrementReference(); _indexBuffer = null!;
        _fontTexture.DecrementReference(); _fontTexture = null!;
        _pipelineState.DecrementReference(); _pipelineState = null!;

        _shader.DecrementReference(); _shader = null!;

        _input.MouseScroll -= MouseScroll;

        _input.KeyChar -= KeyCharacterInput;
        _input.KeyUp -= KeyEventUp;
        _input.KeyDown -= KeyEventDown;

        _context = null!;

        ImGui.DestroyContext(pContext); pContext = nint.Zero;
    }

    ~ImGuiController() {
        Dispose(disposing: false);
    }

    public void Dispose() {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
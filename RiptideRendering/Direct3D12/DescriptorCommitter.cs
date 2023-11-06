namespace RiptideRendering.Direct3D12;

internal sealed unsafe class DescriptorCommitter(D3D12RenderingContext context) {
    public readonly record struct CommitEntry(uint ParameterIndex, uint NumResourceDescriptors, CpuDescriptorHandle StartResourceHandle);
    private readonly struct CommitVersioning {
        public readonly List<CommitEntry> Committing;
        public readonly List<CommitEntry> Committed;

        public CommitVersioning() {
            Committing = new();
            Committed = new();
        }

        public void ClearAllEntries() {
            Committing.Clear();
            Committed.Clear();
        }
        public void ClearCommittingEntries() {
            Committing.Clear();
        }
        public void MergeCommittingHistory() {
            foreach (ref readonly var committing in CollectionsMarshal.AsSpan(Committing)) {
                bool found = false;

                foreach (ref var committed in CollectionsMarshal.AsSpan(Committed)) {
                    if (committing.ParameterIndex != committed.ParameterIndex) continue;

                    committed = committing;
                    found = true;
                    break;
                }

                if (!found) {
                    Committed.Add(committing);
                }
            }
        }
        public void GetNumCommittingDescriptors(out uint numResourceDescriptors, out uint numSamplerDescriptors) {
            numResourceDescriptors = 0;
            numSamplerDescriptors = 0;

            foreach (ref readonly var entry in CollectionsMarshal.AsSpan(Committing)) {
                numResourceDescriptors += entry.NumResourceDescriptors;
            }
        }
    }

    private readonly List<nint> _finishedStagingHeaps = new();
    private StagingDescriptorHeapLinearAllocator _resourceStagingAllocator;

    private readonly D3D12RenderingContext _context = context;
    private readonly DynamicRenderingDescriptorHeap _resourceRenderingHeap = new(context, DescriptorHeapType.CbvSrvUav);

    private readonly CommitVersioning _graphicsVersioning = new(), _computeVersioning = new();

    public void ResetGraphics() => _graphicsVersioning.ClearAllEntries();
    public void ResetCompute() => _computeVersioning.ClearAllEntries();

    public void FinalizeResources() {
        _finishedStagingHeaps.Clear();

        _resourceStagingAllocator.Finish(_context.StagingResourceDescHeapPool);
        _resourceStagingAllocator = new(_context.StagingResourceDescHeapPool);

        _resourceRenderingHeap.RetireCurrentHeap();
    }

    public void InitializeGraphicDescriptors(ReadOnlySpan<RootParameter> rootParameters, Shader shader) {
        D3D12Utils.CountTotalDescriptors((RootParameter*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(rootParameters)), (uint)rootParameters.Length, out uint numResourceDescs, out uint numSamplerDescs);
        _graphicsVersioning.ClearAllEntries();

        Debug.Assert(numSamplerDescs == 0, "Sampler binding is not supported yet.");

        if (numResourceDescs != 0) {
            var resourceStagingHandle = AllocStagingResourceDescriptor(numResourceDescs);
            Debug.Assert(resourceStagingHandle.Ptr != nuint.MaxValue);
            var incrementSize = _context.Constants.ResourceViewDescIncrementSize;

            for (int p = 0; p < rootParameters.Length; p++) {
                ref readonly var param = ref rootParameters[p];
                if (param.ParameterType != RootParameterType.TypeDescriptorTable) continue;

                ref readonly var table = ref param.DescriptorTable;

                D3D12Utils.CountTotalDescriptors(table.PDescriptorRanges, table.NumDescriptorRanges, out numResourceDescs, out numSamplerDescs);

                FillNullDescriptors(resourceStagingHandle, incrementSize, (ID3D12Device*)_context.Device, shader, table.PDescriptorRanges, table.NumDescriptorRanges);
                _graphicsVersioning.Committing.Add(new((uint)p, numResourceDescs, resourceStagingHandle));

                resourceStagingHandle.Offset(numResourceDescs, incrementSize);
            }
        }
    }

    public void InitializeComputeBindingSchematic(RootParameter* rootParameters, uint numParameters, Shader shader) {
        D3D12Utils.CountTotalDescriptors(rootParameters, numParameters, out uint numResourceDescs, out uint numSamplerDescs);
        _computeVersioning.ClearAllEntries();

        Debug.Assert(numSamplerDescs == 0, "Sampler binding is not supported yet.");

        if (numResourceDescs != 0) {
            var resourceStagingHandle = AllocStagingResourceDescriptor(numResourceDescs);
            Debug.Assert(resourceStagingHandle.Ptr != nuint.MaxValue);
            var incrementSize = _context.Constants.ResourceViewDescIncrementSize;

            for (uint p = 0; p < numParameters; p++) {
                ref readonly var param = ref rootParameters[p];
                if (param.ParameterType != RootParameterType.TypeDescriptorTable) continue;

                ref readonly var table = ref param.DescriptorTable;

                D3D12Utils.CountTotalDescriptors(table.PDescriptorRanges, table.NumDescriptorRanges, out numResourceDescs, out numSamplerDescs);

                FillNullDescriptors(resourceStagingHandle, incrementSize, (ID3D12Device*)_context.Device, shader, table.PDescriptorRanges, table.NumDescriptorRanges);
                _computeVersioning.Committing.Add(new(p, numResourceDescs, resourceStagingHandle));

                resourceStagingHandle.Offset(numResourceDescs, incrementSize);
            }
        }
    }

    private CpuDescriptorHandle AllocStagingResourceDescriptor(uint numDescriptors) {
        var incrementSize = _context.Constants.ResourceViewDescIncrementSize;
        if (_resourceStagingAllocator.TryAllocate(numDescriptors, incrementSize, out var handle)) {
            return handle;
        }

        _finishedStagingHeaps.Add((nint)_resourceStagingAllocator.DetachHeap());

        _resourceStagingAllocator = new(_context.StagingResourceDescHeapPool, numDescriptors);
        bool alloc = _resourceStagingAllocator.TryAllocate(numDescriptors, incrementSize, out handle);
        Debug.Assert(alloc, "Allocation from newly created heap pool failed.");

        return handle;
    }

    private bool TryGetResourceDescriptor(uint rootParameterIndex, uint descriptorOffset, List<CommitEntry> committingEntries, List<CommitEntry> committedEntries, out CpuDescriptorHandle outputHandle) {
        uint incrementSize = _context.Constants.ResourceViewDescIncrementSize;

        foreach (ref readonly var committing in CollectionsMarshal.AsSpan(committingEntries)) {
            if (committing.ParameterIndex != rootParameterIndex) continue;

            Debug.Assert(descriptorOffset < committing.NumResourceDescriptors);

            outputHandle = new() { Ptr = committing.StartResourceHandle.Ptr + descriptorOffset * incrementSize };
            return true;
        }

        foreach (ref readonly var committed in CollectionsMarshal.AsSpan(committedEntries)) {
            if (committed.ParameterIndex != rootParameterIndex) continue;

            Debug.Assert(descriptorOffset < committed.NumResourceDescriptors);

            var handle = AllocStagingResourceDescriptor(committed.NumResourceDescriptors);
            _context.Device->CopyDescriptorsSimple(committed.NumResourceDescriptors, handle, committed.StartResourceHandle, DescriptorHeapType.CbvSrvUav);

            committingEntries.Add(new(committed.ParameterIndex, committed.NumResourceDescriptors, handle));

            outputHandle = new() { Ptr = handle.Ptr + descriptorOffset * incrementSize };
            return true;
        }

        outputHandle = D3D12Helper.UnknownCpuHandle;
        return false;
    }

    public bool TryGetGraphicsResourceDescriptor(uint rootParameterIndex, uint descriptorOffset, out CpuDescriptorHandle outputHandle) => TryGetResourceDescriptor(rootParameterIndex, descriptorOffset, _graphicsVersioning.Committing, _graphicsVersioning.Committed, out outputHandle);
    public bool TryGetComputeResourceDescriptor(uint rootParameterIndex, uint descriptorOffset, out CpuDescriptorHandle outputHandle) => TryGetResourceDescriptor(rootParameterIndex, descriptorOffset, _computeVersioning.Committing, _computeVersioning.Committed, out outputHandle);

    private void Commit(ID3D12GraphicsCommandList* pCommandList, CommitVersioning versioning, delegate* unmanaged[Stdcall]<ID3D12GraphicsCommandList*, uint, GpuDescriptorHandle, void> descriptorTableSetter) {
        versioning.GetNumCommittingDescriptors(out uint numCommittingResourceDescs, out uint numCommittingSamplerDescs);
        Debug.Assert(numCommittingSamplerDescs == 0, "Sampler binding is not supported yet.");

        if (numCommittingResourceDescs != 0) {
            var incrementSize = _context.Constants.ResourceViewDescIncrementSize;
            var device = _context.Device;

            if (_resourceRenderingHeap.CurrentHeap == null) {
                var renderingHandle = _resourceRenderingHeap.Allocate(numCommittingResourceDescs);

                var heap = _resourceRenderingHeap.CurrentHeap;
                pCommandList->SetDescriptorHeaps(1, &heap);

                foreach (ref readonly var entry in CollectionsMarshal.AsSpan(versioning.Committing)) {
                    device->CopyDescriptorsSimple(entry.NumResourceDescriptors, renderingHandle.Cpu, entry.StartResourceHandle, DescriptorHeapType.CbvSrvUav);
                    descriptorTableSetter(pCommandList, entry.ParameterIndex, renderingHandle.Gpu);

                    var offset = incrementSize * entry.NumResourceDescriptors;
                    renderingHandle = new(renderingHandle.Cpu.Ptr + offset, renderingHandle.Gpu.Ptr + offset);
                }

                versioning.Committed.AddRange(versioning.Committing);
            } else {
                if (_resourceRenderingHeap.HasEnoughDescriptor(numCommittingResourceDescs)) {
                    var renderingHandle = _resourceRenderingHeap.Allocate(numCommittingResourceDescs);

                    var heap = _resourceRenderingHeap.CurrentHeap;
                    pCommandList->SetDescriptorHeaps(1, &heap);

                    foreach (ref readonly var entry in CollectionsMarshal.AsSpan(versioning.Committing)) {
                        device->CopyDescriptorsSimple(entry.NumResourceDescriptors, renderingHandle.Cpu, entry.StartResourceHandle, DescriptorHeapType.CbvSrvUav);
                        descriptorTableSetter(pCommandList, entry.ParameterIndex, renderingHandle.Gpu);

                        var offset = incrementSize * entry.NumResourceDescriptors;
                        renderingHandle = new(renderingHandle.Cpu.Ptr + offset, renderingHandle.Gpu.Ptr + offset);
                    }
                }

                versioning.MergeCommittingHistory();
            }

            versioning.ClearCommittingEntries();
        }
    }

    public void CommitGraphics(ID3D12GraphicsCommandList* pCommandList) {
        Commit(pCommandList, _graphicsVersioning, (delegate* unmanaged[Stdcall]<ID3D12GraphicsCommandList*, uint, GpuDescriptorHandle, void>)pCommandList->LpVtbl[32]);
    }

    public void CommitCompute(ID3D12GraphicsCommandList* pCommandList) {
        Commit(pCommandList, _computeVersioning, (delegate* unmanaged[Stdcall]<ID3D12GraphicsCommandList*, uint, GpuDescriptorHandle, void>)pCommandList->LpVtbl[31]);
    }

    private static void FillNullDescriptors(CpuDescriptorHandle startHandle, uint incrementSize, ID3D12Device* pDevice, Shader shader, DescriptorRange* pRanges, uint numRanges) {
        nuint handle = startHandle.Ptr;

        for (uint r = 0; r < numRanges; r++) {
            ref readonly var range = ref pRanges[r];

            switch (range.RangeType) {
                case DescriptorRangeType.Cbv:
                    for (uint d = 0; d < range.NumDescriptors; d++) {
                        pDevice->CreateConstantBufferView(null, Unsafe.BitCast<nuint, CpuDescriptorHandle>(handle));
                        handle += incrementSize;
                    }
                    break;
                case DescriptorRangeType.Srv: {
                    for (uint d = 0; d < range.NumDescriptors; d++) {
                        if (shader.TryGetReadonlyResourceInfo(range.BaseShaderRegister + d, range.RegisterSpace, out var info)) {
                            ShaderResourceViewDesc srvdesc = new() {
                                Shader4ComponentMapping = D3D12Helper.DefaultShader4ComponentMapping,
                            };

                            switch (info.Type) {
                                case ResourceType.TextureBuffer:
                                    srvdesc.ViewDimension = SrvDimension.Buffer;
                                    srvdesc.Format = Format.FormatR32Float;
                                    break;

                                case ResourceType.Buffer:
                                    srvdesc.ViewDimension = SrvDimension.Buffer;
                                    srvdesc.Format = Format.FormatR8Unorm;
                                    break;

                                case ResourceType.StructuredBuffer:
                                    srvdesc.ViewDimension = SrvDimension.Buffer;
                                    srvdesc.Format = Format.FormatUnknown;
                                    srvdesc.Buffer.StructureByteStride = 4;     // Arbitrary value so that GPU doesn't crash due to invalid creation.
                                    break;

                                case ResourceType.ByteAddressBuffer:
                                    srvdesc.ViewDimension = SrvDimension.Buffer;
                                    srvdesc.Format = Format.FormatR32Uint;
                                    srvdesc.Buffer = new() {
                                        FirstElement = 0,
                                        NumElements = 0,
                                        Flags = BufferSrvFlags.Raw,
                                    };
                                    break;

                                case ResourceType.Texture1D:
                                    srvdesc.ViewDimension = SrvDimension.Texture1D;
                                    srvdesc.Format = Format.FormatR8Unorm;
                                    break;

                                case ResourceType.Texture1DArray:
                                    srvdesc.ViewDimension = SrvDimension.Texture1Darray;
                                    srvdesc.Format = Format.FormatR8Unorm;
                                    break;

                                case ResourceType.Texture2D:
                                    srvdesc.ViewDimension = SrvDimension.Texture2D;
                                    srvdesc.Format = Format.FormatR8Unorm;
                                    break;

                                case ResourceType.Texture2DArray:
                                    srvdesc.ViewDimension = SrvDimension.Texture2Darray;
                                    srvdesc.Format = Format.FormatR8Unorm;
                                    break;

                                case ResourceType.Texture2DMS:
                                    srvdesc.ViewDimension = SrvDimension.Texture2Dms;
                                    srvdesc.Format = Format.FormatR8Unorm;
                                    break;

                                case ResourceType.Texture2DMSArray:
                                    srvdesc.ViewDimension = SrvDimension.Texture2Dmsarray;
                                    srvdesc.Format = Format.FormatR8Unorm;
                                    break;

                                case ResourceType.Texture3D:
                                    srvdesc.ViewDimension = SrvDimension.Texture3D;
                                    srvdesc.Format = Format.FormatR8Unorm;
                                    break;

                                case ResourceType.TextureCube:
                                    srvdesc.ViewDimension = SrvDimension.Texturecube;
                                    srvdesc.Format = Format.FormatR8Unorm;
                                    break;

                                case ResourceType.TextureCubeArray:
                                    srvdesc.ViewDimension = SrvDimension.Texturecubearray;
                                    srvdesc.Format = Format.FormatR8Unorm;
                                    break;

                                default: throw new NotImplementedException($"Unimplemented case '{info.Type}'.");
                            }

                            pDevice->CreateShaderResourceView(null, &srvdesc, Unsafe.BitCast<nuint, CpuDescriptorHandle>(handle));
                        } else {
                            pDevice->CreateShaderResourceView(null, new ShaderResourceViewDesc() {
                                Shader4ComponentMapping = D3D12Helper.DefaultShader4ComponentMapping,
                                ViewDimension = SrvDimension.Buffer,
                                Format = Format.FormatR8Unorm,
                            }, Unsafe.BitCast<nuint, CpuDescriptorHandle>(handle));
                        }
                        handle += incrementSize;
                    }
                    break;
                }
                case DescriptorRangeType.Uav: {
                    for (uint d = 0; d < range.NumDescriptors; d++) {
                        var createHandle = Unsafe.BitCast<nuint, CpuDescriptorHandle>(handle);

                        if (shader.TryGetReadWriteResourceInfo(range.BaseShaderRegister + d, range.RegisterSpace, out var info)) {
                            UnorderedAccessViewDesc uavdesc = default;

                            switch (info.Type) {
                                case ResourceType.RWBuffer:
                                    uavdesc.ViewDimension = UavDimension.Buffer;
                                    uavdesc.Format = Format.FormatR8Unorm;
                                    break;

                                case ResourceType.RWStructuredBuffer or ResourceType.RWStructuredBufferWithCounter or ResourceType.AppendStructuredBuffer or ResourceType.ConsumeStructuredBuffer:
                                    uavdesc.ViewDimension = UavDimension.Buffer;
                                    uavdesc.Format = Format.FormatUnknown;
                                    break;

                                case ResourceType.RWByteAddressBuffer:
                                    uavdesc.ViewDimension = UavDimension.Buffer;
                                    uavdesc.Format = Format.FormatR32Uint;
                                    uavdesc.Buffer = new() { Flags = BufferUavFlags.Raw };
                                    break;

                                case ResourceType.RWTexture1D:
                                    uavdesc.ViewDimension = UavDimension.Texture1D;
                                    uavdesc.Format = Format.FormatR8Unorm;
                                    break;

                                case ResourceType.RWTexture1DArray:
                                    uavdesc.ViewDimension = UavDimension.Texture1Darray;
                                    uavdesc.Format = Format.FormatR8Unorm;
                                    break;

                                case ResourceType.RWTexture2D:
                                    uavdesc.ViewDimension = UavDimension.Texture2D;
                                    uavdesc.Format = Format.FormatR8Unorm;
                                    break;

                                case ResourceType.RWTexture2DArray:
                                    uavdesc.ViewDimension = UavDimension.Texture2Darray;
                                    uavdesc.Format = Format.FormatR8Unorm;
                                    break;

                                case ResourceType.RWTexture3D:
                                    uavdesc.ViewDimension = UavDimension.Texture3D;
                                    uavdesc.Format = Format.FormatR8Unorm;
                                    break;

                                default: throw new NotImplementedException($"Unimplemented case '{info.Type}'.");
                            }

                            pDevice->CreateUnorderedAccessView(null, null, &uavdesc, createHandle);
                        } else {
                            pDevice->CreateUnorderedAccessView(null, null, new UnorderedAccessViewDesc() {
                                ViewDimension = UavDimension.Buffer,
                                Format = Format.FormatR8Unorm,
                            }, createHandle);
                        }
                        handle += incrementSize;
                    }
                    break;
                }
            }
        }
    }

    public void CleanUp(ulong fenceValue) {
        _resourceRenderingHeap.CleanUp(fenceValue);
    }

    public void Dispose(ulong fenceValue) {
        _resourceStagingAllocator.Finish(_context.StagingResourceDescHeapPool);
        _resourceStagingAllocator = default;

        _resourceRenderingHeap.CleanUp(fenceValue);
    }
}
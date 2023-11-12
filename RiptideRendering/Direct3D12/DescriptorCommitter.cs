namespace RiptideRendering.Direct3D12;

internal sealed unsafe partial class DescriptorCommitter(D3D12RenderingContext context) {
    private readonly D3D12RenderingContext _context = context;
    private ID3D12DescriptorHeap* _boundResourceHeap, _boundSamplerHeap;

    private uint numResourceDescriptors, numSamplerDescriptors;

    private readonly List<CommitTable> _committedGraphicsTables = [], _committingGraphicsTables = [];

    public void Reset() {
        _committedGraphicsTables.Clear();
        _committingGraphicsTables.Clear();
    }

    public void InitializeGraphicDescriptors(D3D12ResourceSignature signature) {
        ReturnStagingHeaps();

        _committedGraphicsTables.Clear();
        _committingGraphicsTables.Clear();
        
        numResourceDescriptors = 0;
        numSamplerDescriptors = 0;

        foreach (var info in signature.TableInfos) {
            if ((info.Bitmap & (1 << 31)) == 0) {
                numResourceDescriptors += info.Bitmap;
            } else {
                numSamplerDescriptors += info.Bitmap & ~(1 << 31);
            }
        }

        if (numResourceDescriptors == 0 && numSamplerDescriptors == 0) return;

        var resourceHandle = AllocStagingResourceDescriptor(numResourceDescriptors);
        var samplerHandle = AllocStagingSamplerDescriptor(numSamplerDescriptors);

        var resourceIncrSize = _context.Constants.ResourceViewDescIncrementSize;
        var samplerIncrSize = _context.Constants.SamplerDescIncrementSize;

        foreach (var info in signature.TableInfos) {
            if ((info.Bitmap & (1 << 31)) == 0) {
                _committingGraphicsTables.Add(new(info.ParameterIndex, info.Bitmap, resourceHandle));
                resourceHandle.Offset(info.Bitmap, resourceIncrSize);
            } else {
                _committingGraphicsTables.Add(new(info.ParameterIndex, info.Bitmap, samplerHandle));
                samplerHandle.Offset(info.Bitmap & ~(1 << 31), samplerIncrSize);
            }
        }
    }

    private bool TryGetResourceDescriptor(uint rootParameterIndex, uint descriptorOffset, List<CommitTable> committingTables, List<CommitTable> committedTables, out CpuDescriptorHandle outputHandle) {
        uint incrementSize = _context.Constants.ResourceViewDescIncrementSize;

        foreach (ref readonly var committing in CollectionsMarshal.AsSpan(committingTables)) {
            if (committing.ParameterIndex != rootParameterIndex) continue;
            if (descriptorOffset >= (committing.Bitmap & ~(1 << 31))) continue;

            outputHandle = new() { Ptr = committing.StartHandle.Ptr + descriptorOffset * incrementSize };
            return true;
        }

        foreach (ref readonly var committed in CollectionsMarshal.AsSpan(committedTables)) {
            if (committed.ParameterIndex != rootParameterIndex) continue;

            uint numDesc = committed.Bitmap & ~(1 << 31);
            if (descriptorOffset >= numDesc) continue;

            CpuDescriptorHandle handle;

            if ((committed.Bitmap & (1 << 31)) == 0) {
                handle = AllocStagingResourceDescriptor(numDesc);
                _context.Device->CopyDescriptorsSimple(numDesc, handle, committed.StartHandle, DescriptorHeapType.CbvSrvUav);
            } else {
                handle = AllocStagingSamplerDescriptor(numDesc);
                _context.Device->CopyDescriptorsSimple(numDesc, handle, committed.StartHandle, DescriptorHeapType.Sampler);
            }

            outputHandle = new() { Ptr = handle.Ptr + descriptorOffset * incrementSize };
            committingTables.Add(new(committed.ParameterIndex, committed.Bitmap, handle));

            return true;
        }

        outputHandle = D3D12Helper.UnknownCpuHandle;
        return false;
    }

    public bool TryGetGraphicsResourceDescriptor(uint rootParameterIndex, uint descriptorOffset, out CpuDescriptorHandle outputHandle) => TryGetResourceDescriptor(rootParameterIndex, descriptorOffset, _committingGraphicsTables, _committedGraphicsTables, out outputHandle);

    private void Commit(ID3D12GraphicsCommandList* pCommandList, List<CommitTable> committingTables, List<CommitTable> committedTables, delegate* unmanaged[Stdcall]<ID3D12GraphicsCommandList*, uint, GpuDescriptorHandle, void> setOperation) {
        if (numResourceDescriptors == 0 && numSamplerDescriptors == 0) return;

        var device = _context.Device;

        var resourceIncrementSize = _context.Constants.ResourceViewDescIncrementSize;
        var samplerIncrementSize = _context.Constants.SamplerDescIncrementSize;

        CpuDescriptorHandle cpuResourceHandle, cpuSamplerHandle;
        GpuDescriptorHandle gpuResourceHandle, gpuSamplerHandle;

        ID3D12DescriptorHeap** ppHeaps = stackalloc ID3D12DescriptorHeap*[2];
        uint numHeaps = 0;

        if (committedTables.Count == 0) {
            // First time committing.
            lock (_context.GpuDescriptorSuballocationLock) {
                if (numResourceDescriptors != 0) {
                    var heap = _context.CurrentResourceGpuDescHeap;

                    if (!heap.TryAllocate(numResourceDescriptors, out cpuResourceHandle, out gpuResourceHandle)) {
                        var pool = _context.GpuResourceDescHeapPool;

                        heap.Retire(pool, _context.RenderingQueue.NextFenceValue - 1);
                        heap.Request(pool, numResourceDescriptors);

                        bool suballocate = heap.TryAllocate(numResourceDescriptors, out cpuResourceHandle, out gpuResourceHandle);
                        Debug.Assert(suballocate);
                    }

                    ppHeaps[numHeaps++] = _boundResourceHeap = heap.Heap;
                } else {
                    cpuResourceHandle = D3D12Helper.UnknownCpuHandle;
                    gpuResourceHandle = D3D12Helper.UnknownGpuHandle;
                }

                if (numSamplerDescriptors != 0) {
                    var heap = _context.CurrentSamplerGpuDescHeap;

                    if (!heap.TryAllocate(numSamplerDescriptors, out cpuSamplerHandle, out gpuSamplerHandle)) {
                        var pool = _context.GpuSamplerDescHeapPool;

                        heap.Retire(pool, _context.RenderingQueue.NextFenceValue - 1);
                        heap.Request(pool, numSamplerDescriptors);

                        bool suballocate = heap.TryAllocate(numSamplerDescriptors, out cpuSamplerHandle, out gpuSamplerHandle);
                        Debug.Assert(suballocate);
                    }

                    ppHeaps[numHeaps++] = _boundSamplerHeap = heap.Heap;
                } else {
                    cpuSamplerHandle = D3D12Helper.UnknownCpuHandle;
                    gpuSamplerHandle = D3D12Helper.UnknownGpuHandle;
                }
            }

            pCommandList->SetDescriptorHeaps(numHeaps, ppHeaps);

            foreach (var committing in committingTables) {
                uint numDescriptors = committing.Bitmap & ~(1U << 31);

                if ((committing.Bitmap & (1 << 31)) == 0) {
                    device->CopyDescriptorsSimple(numDescriptors, cpuResourceHandle, committing.StartHandle, DescriptorHeapType.CbvSrvUav);
                    setOperation(pCommandList, committing.ParameterIndex, gpuResourceHandle);

                    cpuResourceHandle.Offset(numDescriptors, resourceIncrementSize);
                    gpuResourceHandle.Offset(numDescriptors, resourceIncrementSize);
                } else {
                    device->CopyDescriptorsSimple(numDescriptors, cpuSamplerHandle, committing.StartHandle, DescriptorHeapType.Sampler);
                    setOperation(pCommandList, committing.ParameterIndex, gpuSamplerHandle);

                    cpuSamplerHandle.Offset(numDescriptors, resourceIncrementSize);
                    gpuSamplerHandle.Offset(numDescriptors, resourceIncrementSize);
                }
            }

            committedTables.AddRange(committingTables);
            committingTables.Clear();
        } else {
            // Separate different cases for easier implementation and prevent me going crazy.

            if (numResourceDescriptors != 0 && numSamplerDescriptors == 0) {
                uint numCommittingDescs = 0;
                foreach (var table in committingTables) {
                    numCommittingDescs += table.Bitmap;
                }

                if (numCommittingDescs == 0) return;

                bool rebindTable = false;

                lock (_context.GpuDescriptorSuballocationLock) {
                    var heap = _context.CurrentResourceGpuDescHeap;

                    if (!heap.TryAllocate(numCommittingDescs, out cpuResourceHandle, out gpuResourceHandle)) {
                        var pool = _context.GpuResourceDescHeapPool;

                        heap.Retire(pool, _context.RenderingQueue.NextFenceValue - 1);
                        heap.Request(pool, numResourceDescriptors);

                        bool suballocate = heap.TryAllocate(numResourceDescriptors, out cpuResourceHandle, out gpuResourceHandle);
                        Debug.Assert(suballocate);
                    }

                    if (_boundResourceHeap != heap.Heap) {
                        rebindTable = true;

                        var pHeap = heap.Heap;
                        pCommandList->SetDescriptorHeaps(1, &pHeap);

                        _boundResourceHeap = heap.Heap;
                    }
                }

                if (rebindTable) {
                    foreach (var committed in committedTables) {
                        uint numDescriptor = committed.Bitmap;

                        bool foundCommitting = false;
                        foreach (var committing in committingTables) {
                            if (committing.ParameterIndex != committed.ParameterIndex) continue;

                            Debug.Assert(numDescriptor == committing.Bitmap);

                            device->CopyDescriptorsSimple(numDescriptor, cpuResourceHandle, committing.StartHandle, DescriptorHeapType.CbvSrvUav);
                            setOperation(pCommandList, committed.ParameterIndex, gpuResourceHandle);

                            foundCommitting = true;
                            break;
                        }

                        if (!foundCommitting) {
                            device->CopyDescriptorsSimple(numDescriptor, cpuResourceHandle, committed.StartHandle, DescriptorHeapType.CbvSrvUav);
                            setOperation(pCommandList, committed.ParameterIndex, gpuResourceHandle);
                        }

                        cpuResourceHandle.Offset(numDescriptor, resourceIncrementSize);
                        gpuResourceHandle.Offset(numDescriptor, resourceIncrementSize);
                    }
                } else {
                    foreach (var committing in committingTables) {
                        uint numDescriptor = committing.Bitmap;

                        Debug.Assert(numDescriptor != 0);

                        device->CopyDescriptorsSimple(numDescriptor, cpuResourceHandle, committing.StartHandle, DescriptorHeapType.CbvSrvUav);
                        setOperation(pCommandList, committing.ParameterIndex, gpuResourceHandle);

                        cpuResourceHandle.Offset(numDescriptor, resourceIncrementSize);
                        gpuResourceHandle.Offset(numDescriptor, resourceIncrementSize);
                    }
                }
            } else if (numResourceDescriptors == 0 && numSamplerDescriptors != 0) {
                throw new NotImplementedException("TODO");
            } else {
                throw new NotImplementedException("TODO");
            }

            MergeCommittingHistory(committingTables, committedTables);

            //uint numCommittingResourceDescs = 0, numCommittingSamplerDescs = 0;
            //foreach (var entry in committingTables) {
            //    numCommittingResourceDescs += entry.NumResourceDescriptors;
            //    numCommittingSamplerDescs += entry.NumSamplerDescriptors;
            //}

            //lock (_context.GpuDescriptorSuballocationLock) {
            //    bool reallocResource = false;
            //    bool reallocSampler = false;

            //    if (numCommittingResourceDescs != 0) {
            //        var heap = _context.CurrentResourceGpuDescHeap;

            //        if (!heap.TryAllocate(numCommittingResourceDescs, out cpuResourceHandle, out gpuResourceHandle)) {
            //            var pool = _context.GpuResourceDescHeapPool;

            //            heap.Retire(pool, _context.RenderingQueue.NextFenceValue - 1);
            //            heap.Request(pool, numCommittingResourceDescs);

            //            bool suballocate = heap.TryAllocate(numResourceDescriptors, out cpuResourceHandle, out gpuResourceHandle);
            //            Debug.Assert(suballocate);

            //            reallocResource = true;
            //        }
            //    } else {
            //        cpuResourceHandle = D3D12Helper.UnknownCpuHandle;
            //        gpuResourceHandle = D3D12Helper.UnknownGpuHandle;
            //    }

            //    if (numCommittingSamplerDescs != 0) {
            //        var heap = _context.CurrentResourceGpuDescHeap;

            //        if (!heap.TryAllocate(numCommittingSamplerDescs, out cpuSamplerHandle, out gpuSamplerHandle)) {
            //            var pool = _context.GpuResourceDescHeapPool;

            //            heap.Retire(pool, _context.RenderingQueue.NextFenceValue - 1);
            //            heap.Request(pool, numCommittingSamplerDescs);

            //            bool suballocate = heap.TryAllocate(numSamplerDescriptors, out cpuSamplerHandle, out gpuSamplerHandle);
            //            Debug.Assert(suballocate);

            //            reallocSampler = true;
            //        }
            //    } else {
            //        cpuSamplerHandle = D3D12Helper.UnknownCpuHandle;
            //        gpuSamplerHandle = D3D12Helper.UnknownGpuHandle;
            //    }

            //    if (reallocResource) {
            //        CpuDescriptorHandle handle = cpuResourceHandle;

            //        foreach (var committed in committedTables) {
            //            if (committed.NumResourceDescriptors == 0) continue;
                        
            //            device->CopyDescriptorsSimple(committed.NumResourceDescriptors, handle, committed.StartResourceHandle, DescriptorHeapType.CbvSrvUav);
            //            handle.Offset(committed.NumResourceDescriptors, resourceIncrementSize);
            //        }
            //    }

            //    if (reallocSampler) {
            //        CpuDescriptorHandle handle = cpuSamplerHandle;

            //        foreach (var committed in committedTables) {
            //            if (committed.NumSamplerDescriptors == 0) continue;

            //            device->CopyDescriptorsSimple(committed.NumSamplerDescriptors, handle, committed.StartSamplerHandle, DescriptorHeapType.Sampler);
            //            handle.Offset(committed.NumSamplerDescriptors, resourceIncrementSize);
            //        }
            //    }

            //    if (reallocResource || reallocSampler) {

            //    }
            //}

            //Console.WriteLine(committingTables[0].ParameterIndex);
        }

        //lock (_context.GpuDescriptorSuballocationLock) {
        //    CpuDescriptorHandle cpuResourceHandle, cpuSamplerHandle;
        //    GpuDescriptorHandle gpuResourceHandle, gpuSamplerHandle;

        //    if (numCommittingResourceDescs != 0) {
        //        var heap = _context.CurrentResourceGpuDescHeap;

        //        if (heap.TryAllocate(numCommittingResourceDescs, out cpuResourceHandle, out gpuResourceHandle)) {
        //            foreach (var table in committingTables) {
        //                device->CopyDescriptorsSimple(table.NumResourceDescriptors, cpuResourceHandle, table.StartResourceHandle, DescriptorHeapType.CbvSrvUav);
        //                cpuResourceHandle.Offset(table.NumResourceDescriptors, resourceIncrementSize);
        //            }
        //        } else {
        //            var pool = _context.GpuResourceDescHeapPool;

        //            heap.Retire(pool, _context.RenderingQueue.NextFenceValue - 1);
        //            heap.Request(pool, numResourceDescriptors);

        //            bool suballocate = heap.TryAllocate(numCommittingResourceDescs, out cpuResourceHandle, out gpuResourceHandle);
        //            Debug.Assert(suballocate);
        //        }
        //    }

        //    //if (numCommittingSamplerDescs != 0) {
        //    //    var heap = _context.CurrentSamplerGpuDescHeap;

        //    //    if (!heap.TryAllocate(numCommittingResourceDescs, out cpuSamplerHandle, out gpuSamplerHandle)) {
        //    //        var pool = _context.GpuSamplerDescHeapPool;

        //    //        heap.Retire(pool, _context.RenderingQueue.NextFenceValue - 1);
        //    //        heap.Request(pool, numResourceDescriptors);

        //    //        bool suballocate = heap.TryAllocate(numCommittingResourceDescs, out cpuSamplerHandle, out gpuSamplerHandle);
        //    //        Debug.Assert(suballocate);
        //    //    }
        //    //}

        //    //ID3D12DescriptorHeap** ppHeaps = stackalloc ID3D12DescriptorHeap*[2];
        //    //uint numHeaps = 0;
        //    //bool resourceHeapChanged = false, samplerHeapChanged = false;

        //    //if (numResourceDescriptors != 0 && _boundResourceHeap != _context.CurrentResourceGpuDescHeap.Heap) {
        //    //    ppHeaps[numHeaps++] = _context.CurrentResourceGpuDescHeap.Heap;
        //    //    resourceHeapChanged = true;
        //    //}

        //    //if (numSamplerDescriptors != 0 && _boundSamplerHeap != _context.CurrentSamplerGpuDescHeap.Heap) {
        //    //    ppHeaps[numHeaps++] = _context.CurrentSamplerGpuDescHeap.Heap;
        //    //    samplerHeapChanged = true;
        //    //}

        //    //if (numHeaps != 0) {
        //    //    pCommandList->SetDescriptorHeaps(numHeaps, ppHeaps);

        //    //    _boundResourceHeap = _context.CurrentResourceGpuDescHeap.Heap;
        //    //    _boundSamplerHeap = _context.CurrentSamplerGpuDescHeap.Heap;
        //    //}

        //    //if (resourceHeapChanged) {

        //    //} else {
        //    //    foreach (var table in committingTables) {

        //    //    }
        //    //}
        //}
    }

    public void CommitGraphics(ID3D12GraphicsCommandList* pCommandList) {
        Commit(pCommandList, _committingGraphicsTables, _committedGraphicsTables, (delegate* unmanaged[Stdcall]<ID3D12GraphicsCommandList*, uint, GpuDescriptorHandle, void>)pCommandList->LpVtbl[32]);
    }

    //public void CommitCompute(ID3D12GraphicsCommandList* pCommandList) {
    //    Commit(pCommandList, _computeVersioning, (delegate* unmanaged[Stdcall]<ID3D12GraphicsCommandList*, uint, GpuDescriptorHandle, void>)pCommandList->LpVtbl[31]);
    //}

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
    }

    public void Dispose(ulong fenceValue) {
        ReturnStagingHeaps();
    }

    private static void MergeCommittingHistory(List<CommitTable> committingEntries, List<CommitTable> committedEntries) {
        foreach (ref readonly var committingEntry in CollectionsMarshal.AsSpan(committingEntries)) {
            bool found = false;

            foreach (ref var committedEntry in CollectionsMarshal.AsSpan(committedEntries)) {
                if (committingEntry.ParameterIndex != committedEntry.ParameterIndex) continue;

                committedEntry = committingEntry;
                found = true;
                break;
            }

            if (!found) {
                committedEntries.Add(committingEntry);
            }
        }
    }

    private readonly record struct CommitTable(uint ParameterIndex, uint Bitmap, CpuDescriptorHandle StartHandle);
}
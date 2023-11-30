using RiptideRendering.Direct3D12.Allocators;
using Silk.NET.Direct3D12;

namespace RiptideRendering.Direct3D12;

internal sealed unsafe class DescriptorCommitter(D3D12RenderingContext context) : IDisposable {
    private ID3D12DescriptorHeap* _stagingDescriptorHeap;
    private CpuDescriptorHandle _stagingStartHandle;

    private ID3D12DescriptorHeap* _gpuHeap;
    private RetainedRingAllocator _gpuHeapAllocator = new();

    private readonly List<nint> _retiredHeaps = [];

    private uint _numResourceDescriptors;

    private readonly List<TableInformation> _tableInfos = [];
    private readonly List<CommittingTable> _committing = [];

    public void InitializeSignature(D3D12ResourceSignature signature) {
        _committing.Clear();
        _tableInfos.Clear();

        if (_stagingDescriptorHeap != null) {
            context.StagingResourceHeapPool.Return(_stagingDescriptorHeap);
            _stagingDescriptorHeap = null!;
        }
        _stagingStartHandle = Helper.UnknownCpuHandle;
        
        _numResourceDescriptors = 0;

        foreach (var info in signature.TableInfos) {
            if ((info.Bitfield & 1 << 31) != 0) continue;
            
            _numResourceDescriptors += info.Bitfield;
        }

        if (_numResourceDescriptors == 0) return;

        _stagingDescriptorHeap = context.StagingResourceHeapPool.Request(_numResourceDescriptors);
        _stagingStartHandle = _stagingDescriptorHeap->GetCPUDescriptorHandleForHeapStart();
        
        var resourceHandle = _stagingStartHandle;
        var incrementSize = context.Device->GetDescriptorHandleIncrementSize(DescriptorHeapType.CbvSrvUav);
        
        foreach (var info in signature.TableInfos) {
            if ((info.Bitfield & 1 << 31) != 0) continue;
            
            _tableInfos.Add(new(info.ParameterIndex, info.Bitfield));
            _committing.Add(new(info.ParameterIndex, info.Bitfield, resourceHandle));
            resourceHandle.Ptr += incrementSize * info.Bitfield;
        }
    }

    public bool TryGetGraphicsResourceDescriptor(uint rootParameterIndex, uint descriptorOffset, out CpuDescriptorHandle outputHandle) {
        uint incrementSize = context.Device->GetDescriptorHandleIncrementSize(DescriptorHeapType.CbvSrvUav);

        foreach (ref readonly var committing in CollectionsMarshal.AsSpan(_committing)) {
            if (committing.ParameterIndex != rootParameterIndex) continue;
            if (descriptorOffset >= committing.NumDescriptors) continue;

            outputHandle = new() { Ptr = committing.Handle.Ptr + descriptorOffset * incrementSize };
            return true;
        }

        uint offset = 0;
        foreach (ref readonly var info in CollectionsMarshal.AsSpan(_tableInfos)) {
            if (info.ParameterIndex != rootParameterIndex) goto fail;
            if (descriptorOffset >= info.NumDescriptors) goto fail;
            
            CpuDescriptorHandle committingHandle = new() { Ptr = _stagingStartHandle.Ptr + incrementSize * offset };
            _committing.Add(new(info.ParameterIndex, info.NumDescriptors, committingHandle));
            
            outputHandle = new() { Ptr = committingHandle.Ptr + incrementSize * descriptorOffset };
            return true;
            
            fail:
            offset += info.NumDescriptors;
        }

        outputHandle = Helper.UnknownCpuHandle;
        return false;
    }

    public void Commit(ID3D12GraphicsCommandList* pCommandList) {
        uint numCommittingDescriptors = 0;
        foreach (var table in CollectionsMarshal.AsSpan(_committing)) {
            numCommittingDescriptors += table.NumDescriptors;
        }

        if (numCommittingDescriptors == 0) return;

        var queue = context.Queues.GraphicQueue;

        uint incrementSize = context.Device->GetDescriptorHandleIncrementSize(DescriptorHeapType.CbvSrvUav);

        if (_gpuHeap == null) {
            _gpuHeap = context.GraphicsResourceDescHeapPool.Request(_numResourceDescriptors, queue.LastCompletedFenceValue);
            _gpuHeapAllocator.Reset(_gpuHeap->GetDesc().NumDescriptors);
            
            pCommandList->SetDescriptorHeaps(1, _gpuHeap);

            CpuDescriptorHandle cpuDescriptorHandle = _gpuHeap->GetCPUDescriptorHandleForHeapStart();
            GpuDescriptorHandle gpuDescriptorHandle = _gpuHeap->GetGPUDescriptorHandleForHeapStart();

            CpuDescriptorHandle stagingHandle = _stagingStartHandle;
            
            foreach (var table in CollectionsMarshal.AsSpan(_tableInfos)) {
                context.Device->CopyDescriptorsSimple(table.NumDescriptors, cpuDescriptorHandle, stagingHandle, DescriptorHeapType.CbvSrvUav);
                pCommandList->SetGraphicsRootDescriptorTable(table.ParameterIndex, gpuDescriptorHandle);
                
                cpuDescriptorHandle.Ptr += incrementSize * table.NumDescriptors;
                stagingHandle.Ptr += incrementSize * table.NumDescriptors;
                gpuDescriptorHandle.Ptr += incrementSize * table.NumDescriptors;
            }
        } else if (!_gpuHeapAllocator.TryAllocate(numCommittingDescriptors, out var offset)) {
            context.GraphicsResourceDescHeapPool.Return(_gpuHeap, queue.NextFenceValue - 1);
            
            _gpuHeap = context.GraphicsResourceDescHeapPool.Request(_numResourceDescriptors, queue.LastCompletedFenceValue);
            _gpuHeapAllocator.Reset(_gpuHeap->GetDesc().NumDescriptors);
            
            pCommandList->SetDescriptorHeaps(1, _gpuHeap);
            
            CpuDescriptorHandle cpuDescriptorHandle = _gpuHeap->GetCPUDescriptorHandleForHeapStart();
            GpuDescriptorHandle gpuDescriptorHandle = _gpuHeap->GetGPUDescriptorHandleForHeapStart();

            CpuDescriptorHandle stagingHandle = _stagingStartHandle;
            
            foreach (var table in CollectionsMarshal.AsSpan(_tableInfos)) {
                context.Device->CopyDescriptorsSimple(table.NumDescriptors, cpuDescriptorHandle, stagingHandle, DescriptorHeapType.CbvSrvUav);
                pCommandList->SetGraphicsRootDescriptorTable(table.ParameterIndex, gpuDescriptorHandle);
                
                cpuDescriptorHandle.Ptr += incrementSize * table.NumDescriptors;
                stagingHandle.Ptr += incrementSize * table.NumDescriptors;
                gpuDescriptorHandle.Ptr += incrementSize * table.NumDescriptors;
            }
        } else {
            
        }
    }

    public void Dispose() {
        if (_stagingDescriptorHeap != null) {
            context.StagingResourceHeapPool.Return(_stagingDescriptorHeap);
            _stagingDescriptorHeap = null!;
        }

        if (_gpuHeap != null) {
            context.GraphicsResourceDescHeapPool.Return(_gpuHeap, context.Queues.GraphicQueue.NextFenceValue - 1);
            _gpuHeap = null;
        }
        
        _stagingStartHandle = Helper.UnknownCpuHandle;
    }

    private readonly record struct TableInformation(uint ParameterIndex, uint NumDescriptors);
    private readonly record struct CommittingTable(uint ParameterIndex, uint NumDescriptors, CpuDescriptorHandle Handle);
}
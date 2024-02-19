using RiptideRendering.Direct3D12.Allocators;
using Silk.NET.Direct3D12;

namespace RiptideRendering.Direct3D12;

// TODO: Sampler implementation.

internal sealed unsafe class GraphicsDescriptorCommitter : IDisposable {
    private RetainedRingAllocator _resourceHeapAllocator = new();
    
    private readonly List<nint> _retiredHeaps = [];
    private readonly D3D12RenderingContext _context;

    private BindingDescriptorHeaps _binding;
    private readonly PipelineCommitter _graphicCommitter, _computeCommitter;

    public GraphicsDescriptorCommitter(D3D12RenderingContext context) {
        _context = context;

        _graphicCommitter = new(this);
        _computeCommitter = new(this);
    }

    public void SetGraphicsResourceSignature(D3D12ResourceSignature signature) {
        _graphicCommitter.SwapSignature(signature);
    }

    public void SetComputeResourceSignature(D3D12ResourceSignature signature) {
        _computeCommitter.SwapSignature(signature);
    }

    public bool TryGetGraphicsResourceDescriptor(uint rootParameterIndex, uint descriptorOffset, out CpuDescriptorHandle outputHandle) {
        return _graphicCommitter.TryGetResourceDescriptor(rootParameterIndex, descriptorOffset, out outputHandle);
    }
    
    public bool TryGetComputeResourceDescriptor(uint rootParameterIndex, uint descriptorOffset, out CpuDescriptorHandle outputHandle) {
        return _computeCommitter.TryGetResourceDescriptor(rootParameterIndex, descriptorOffset, out outputHandle);
    }

    public void CommitGraphics(ID3D12GraphicsCommandList* pList) {
        var numCommitting = _graphicCommitter.NumCommittingResourceDescriptors;

        if (numCommitting == 0) return;

        var setter = (delegate*<ID3D12GraphicsCommandList*, uint, GpuDescriptorHandle, void>)pList->LpVtbl[32];

        CommitPipeline(pList, _graphicCommitter, setter);
    }
    
    public void CommitCompute(ID3D12GraphicsCommandList* pList) {
        var numCommitting = _computeCommitter.NumCommittingResourceDescriptors;

        if (numCommitting == 0) return;
        
        var setter = (delegate*<ID3D12GraphicsCommandList*, uint, GpuDescriptorHandle, void>)pList->LpVtbl[31];
        
        CommitPipeline(pList, _computeCommitter, setter);
    }
    
    private void CommitPipeline(ID3D12GraphicsCommandList* pList, PipelineCommitter committer, delegate*<ID3D12GraphicsCommandList*, uint, GpuDescriptorHandle, void> setter) {
        var numCommitting = committer.NumCommittingResourceDescriptors;

        if (numCommitting == 0) return;

        if (_binding[0] == nint.Zero) {
            var heap = _context.ResourceDescriptorHeapPool.Request(numCommitting, _context.Queues.GraphicsQueue.LastCompletedFenceValue);
            _resourceHeapAllocator.Reset(heap->GetDesc().NumDescriptors);

            _resourceHeapAllocator.TryAllocate(numCommitting, out _);
            pList->SetDescriptorHeaps(1, &heap);
            
            committer.Commit(pList, setter, heap->GetCPUDescriptorHandleForHeapStart(), heap->GetGPUDescriptorHandleForHeapStart());

            _binding[0] = (nint)heap;
        } else if (_resourceHeapAllocator.TryAllocate(numCommitting, out var offset)) {
            var cpuHandle = ((ID3D12DescriptorHeap*)_binding[0])->GetCPUDescriptorHandleForHeapStart();
            var gpuHandle = ((ID3D12DescriptorHeap*)_binding[0])->GetGPUDescriptorHandleForHeapStart();
            
            var incrementSize = _context.Device->GetDescriptorHandleIncrementSize(DescriptorHeapType.CbvSrvUav);
            var incrementByte = incrementSize * offset;
            
            committer.Commit(pList, setter, new() {
                Ptr = cpuHandle.Ptr + (nuint)incrementByte,
            }, new() {
                Ptr = gpuHandle.Ptr + incrementByte,
            });
        } else {
            _retiredHeaps.Add(_binding[0]);
            var heap = _context.ResourceDescriptorHeapPool.Request(numCommitting, _context.Queues.GraphicsQueue.LastCompletedFenceValue);
            _resourceHeapAllocator.Reset(heap->GetDesc().NumDescriptors);
            
            pList->SetDescriptorHeaps(1, &heap);
            
            committer.AddUncommittedParameters();
            committer.Commit(pList, setter, heap->GetCPUDescriptorHandleForHeapStart(), heap->GetGPUDescriptorHandleForHeapStart());

            _resourceHeapAllocator.TryAllocate(committer.NumResourceDescriptors, out _);
            
            _binding[0] = (nint)heap;
        }
    }

    public void Dispose() {
        var returnFence = _context.Queues.GraphicsQueue.NextFenceValue - 1;
        
        foreach (var ptr in _retiredHeaps) {
            _context.ResourceDescriptorHeapPool.Return((ID3D12DescriptorHeap*)ptr, returnFence);
        }

        if (_binding[0] != nint.Zero) {
            _context.ResourceDescriptorHeapPool.Return((ID3D12DescriptorHeap*)_binding[0], returnFence);
        }

        _graphicCommitter.Dispose();
        _computeCommitter.Dispose();
    }

    [InlineArray(1)]
    private struct BindingDescriptorHeaps {
        private nint _element;
    }
    
    private sealed class PipelineCommitter(GraphicsDescriptorCommitter committer) : IDisposable {
        private CpuDescriptorHandle _stagingResourceHandle;
        
        private readonly List<TableInformation> _tableInfos = [];
        private readonly List<CommittingTable> _committing = [];

        private uint _numResourceDescs, _numSamplers;

        public uint NumResourceDescriptors => _numResourceDescs;
        public uint NumCommittingResourceDescriptors {
            get {
                uint result = 0;
                foreach (ref readonly var table in CollectionsMarshal.AsSpan(_committing)) {
                    result += table.NumDescriptors;
                }

                return result;
            }
        }

        public void SwapSignature(D3D12ResourceSignature signature) {
            _committing.Clear();
            _tableInfos.Clear();
            
            committer._context.StagingResourceHeapPool.Deallocate(_stagingResourceHandle);
            _stagingResourceHandle = Helper.UnknownCpuHandle;

            _numResourceDescs = _numSamplers = 0;

            foreach (ref readonly var info in signature.Parameters) {
                if (info.Type != ResourceParameterType.Descriptors) continue;
                    
                ref readonly var descriptors = ref info.Descriptors;
                
                if (descriptors.Type == DescriptorTableType.Sampler) {
                    _numSamplers += descriptors.NumDescriptors;
                } else {
                    _numResourceDescs += descriptors.NumDescriptors;
                }
            }
            
            Debug.Assert(_numSamplers == 0, "Sampler descriptor is not implemented.");

            if (_numResourceDescs != 0) {
                _stagingResourceHandle = committer._context.StagingResourceHeapPool.Allocate(_numResourceDescs);
                
                var incrementSize = committer._context.Device->GetDescriptorHandleIncrementSize(DescriptorHeapType.CbvSrvUav);

                uint offset = 0;
                for (int i = 0, c = signature.Parameters.Length; i < c; i++) {
                    ref readonly var info = ref signature.Parameters[i];
                    if (info.Type != ResourceParameterType.Descriptors) continue;
                    
                    ref readonly var descriptors = ref info.Descriptors;
                    if (descriptors.Type == DescriptorTableType.Sampler) continue;
                    
                    _tableInfos.Add(new((uint)i, offset, descriptors.NumDescriptors));
                    _committing.Add(new((uint)i, offset, descriptors.NumDescriptors, new() {
                        Ptr = _stagingResourceHandle.Ptr + incrementSize * offset,
                    }));
                    
                    offset += descriptors.NumDescriptors;
                }
            }
        }
        
        public bool TryGetResourceDescriptor(uint rootParameterIndex, uint descriptorOffset, out CpuDescriptorHandle outputHandle) {
            var incrementSize = committer._context.Device->GetDescriptorHandleIncrementSize(DescriptorHeapType.CbvSrvUav);
            
            foreach (ref readonly var committing in CollectionsMarshal.AsSpan(_committing)) {
                if (committing.ParameterIndex != rootParameterIndex) continue;
                if (descriptorOffset >= committing.NumDescriptors) continue;
                
                outputHandle = new() { Ptr = committing.Handle.Ptr + descriptorOffset * incrementSize };
                return true;
            }
            
            foreach (ref readonly var info in CollectionsMarshal.AsSpan(_tableInfos)) {
                if (info.ParameterIndex != rootParameterIndex) continue;
                if (descriptorOffset >= info.NumDescriptors) continue;

                var offset = _committing.Count == 0 ? 0 : _committing[^1].DescriptorOffset + _committing[^1].NumDescriptors;
                CpuDescriptorHandle committingHandle = new() { Ptr = _stagingResourceHandle.Ptr + incrementSize * info.DescriptorOffset };
                _committing.Add(new(info.ParameterIndex, offset, info.NumDescriptors, committingHandle));
                
                outputHandle = new() { Ptr = committingHandle.Ptr + incrementSize * descriptorOffset };
                return true;
            }
            
            outputHandle = Helper.UnknownCpuHandle;
            return false;
        }

        public void AddUncommittedParameters() {
            var incrementSize = committer._context.Device->GetDescriptorHandleIncrementSize(DescriptorHeapType.CbvSrvUav);
            
            foreach (ref readonly var info in CollectionsMarshal.AsSpan(_tableInfos)) {
                foreach (ref readonly var committing in CollectionsMarshal.AsSpan(_committing)) {
                    if (committing.ParameterIndex == info.ParameterIndex) {
                        goto next;
                    }
                }
                
                CpuDescriptorHandle committingHandle = new() { Ptr = _stagingResourceHandle.Ptr + incrementSize * info.DescriptorOffset };
                _committing.Add(new(info.ParameterIndex, info.DescriptorOffset, info.NumDescriptors, committingHandle));
                
                next: ;
            }
        }

        public void Commit(ID3D12GraphicsCommandList* pCommandList, delegate*<ID3D12GraphicsCommandList*, uint, GpuDescriptorHandle, void> setter, CpuDescriptorHandle copyDestination, GpuDescriptorHandle commitStart) {
            var incrementSize = committer._context.Device->GetDescriptorHandleIncrementSize(DescriptorHeapType.CbvSrvUav);
            
            foreach (var committing in CollectionsMarshal.AsSpan(_committing)) {
                uint increment = incrementSize * committing.DescriptorOffset;
                
                var destCpuHandle = new CpuDescriptorHandle {
                    Ptr = copyDestination.Ptr + increment,
                };
                var destGpuHandle = new GpuDescriptorHandle {
                    Ptr = commitStart.Ptr + increment,
                };
                
                committer._context.Device->CopyDescriptorsSimple(committing.NumDescriptors, destCpuHandle, committing.Handle, DescriptorHeapType.CbvSrvUav);
                setter(pCommandList, committing.ParameterIndex, destGpuHandle);
            }
            
            _committing.Clear();
        }

        public void Dispose() {
            _committing.Clear();
            _tableInfos.Clear();
            
            committer._context.StagingResourceHeapPool.Deallocate(_stagingResourceHandle);
            _stagingResourceHandle = Helper.UnknownCpuHandle;
        }
    }

    private readonly record struct TableInformation(uint ParameterIndex, uint DescriptorOffset, uint NumDescriptors);
    private readonly record struct CommittingTable(uint ParameterIndex, uint DescriptorOffset, uint NumDescriptors, CpuDescriptorHandle Handle);
}
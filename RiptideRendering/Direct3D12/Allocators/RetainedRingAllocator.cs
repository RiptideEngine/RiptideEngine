namespace RiptideRendering.Direct3D12.Allocators;

internal struct RetainedRingAllocator {
    public ulong Size { get; private set; }

    private ulong _head, _tail, _usedSize, _currentFrameAllocSize;
    
    public bool IsFull => _usedSize == Size;
    public bool IsEmpty => _usedSize == 0;
    private readonly Queue<AllocatedAttribute> _allocAttribs = [];

    public RetainedRingAllocator() {
        _allocAttribs = [];
        _head = _tail = _usedSize = _currentFrameAllocSize = 0;
        Size = 0;
    }

    public void Reset() {
        _head = _tail = _usedSize = _currentFrameAllocSize = 0;
        _allocAttribs.Clear();        
    }

    public void Reset(ulong newSize) {
        _head = _tail = _usedSize = _currentFrameAllocSize = 0;
        Size = newSize;
        _allocAttribs.Clear();
    }

    public void ClearAllocatedAttributes() => _allocAttribs.Clear();
    
    public bool TryAllocate(ulong size, out ulong offset) {
        if (IsFull) {
            offset = 0;
            return false;
        }

        if (_tail >= _head) {
            if (_tail + size <= Size) {
                offset = _tail;
                _tail += size;
                _usedSize += size;
                _currentFrameAllocSize += size;

                return true;
            }
            
            if (size <= _head) {
                _tail = size;
                
                var add = Size - _tail + size;
                _usedSize += add;
                _currentFrameAllocSize += add;
                
                offset = 0;
                return true;
            }
        } else if (_tail + size <= _head) {
            offset = _tail;

            _tail += size;
            _usedSize += size;
            _currentFrameAllocSize += size;
            
            return true;
        }

        offset = 0;
        return false;
    }

    public bool TryAllocate(ulong size, ulong alignment, out ulong offset) {
        if (IsFull) {
            offset = 0;
            return false;
        }
        
        Debug.Assert(ulong.IsPow2(alignment), "ulong.IsPow2(alignment)");
        ulong alignedTail = MathUtils.AlignUpwardPow2(_tail, alignment);

        if (_tail >= _head) {
            if (alignedTail + size <= Size) {
                offset = alignedTail;

                var add = size + (alignedTail - _tail);
                
                _tail = alignedTail + size;
                _usedSize += add;
                _currentFrameAllocSize += add;

                return true;
            }
        
            if (size <= _head) {
                _tail = size;
            
                var add = Size - _tail + size;
                _usedSize += add;
                _currentFrameAllocSize += add;
            
                offset = 0;
                return true;
            }
        } else if (alignedTail + size <= _head) {
            offset = alignedTail;
            
            var add = size + (alignedTail - _tail);
                
            _tail = alignedTail + size;
            _usedSize += add;
            _currentFrameAllocSize += add;

            return true;
        }

        offset = 0;
        return false;
    }
    
    public void FinishCurrentFrame(ulong fenceValue) {
        if (_currentFrameAllocSize == 0) return;
        
        _allocAttribs.Enqueue(new(fenceValue, _tail, _currentFrameAllocSize));
    }

    public void ReleaseCompletedFrames(ulong completedFenceValue) {
        while (_allocAttribs.TryPeek(out var peek) && peek.Fence <= completedFenceValue) {
            _usedSize -= peek.Size;
            _head = peek.Offset;
            
            _allocAttribs.Dequeue();
        }
    }
    
    private readonly record struct AllocatedAttribute(ulong Fence, ulong Offset, ulong Size);
}
namespace RiptideRendering.Direct3D12.Allocation;

internal abstract class RingBuffer {
    protected readonly record struct AllocationAttribute(ulong FenceValue, ulong Offset, ulong Size);

    public ulong MaxSize => _maxSize;
    public bool IsEmpty => _usedSize == 0;

    public RingBuffer(ulong maxSize) {
        _completedFrameTails = new();
        _maxSize = maxSize;
    }

    protected bool TryAllocateImpl(ulong size, out ulong offset) {
        if (_usedSize == _maxSize) goto failure;

        if (_tail >= _head) {
            if (_tail + size <= _maxSize) {
                offset = _tail;
                _tail += size;
                _usedSize += size;
                _currFrameSize += size;

                return true;
            } else if (size <= _head) {
                ulong add = _maxSize - _tail + size;
                _usedSize += add;
                _currFrameSize += add;
                _tail = size;
                offset = 0;
                return true;
            }
        } else if (_tail + size <= _head) {
            offset = _tail;
            _tail += size;
            _usedSize += size;
            _currFrameSize += size;

            return true;
        }

        failure:
        offset = 0;
        return false;
    }
    public bool HasEnough(ulong size) {
        if (_usedSize == _maxSize) return false;

        if (_tail >= _head) {
            if (_tail + size <= _maxSize || size <= _head) {
                return true;
            }
        } else if (_tail + size <= _head) {
            return true;
        }

        return false;
    }

    public void FinishCurrentFrame(ulong fenceValue) {
        if (_currFrameSize == 0) return;

        _completedFrameTails.Enqueue(new(fenceValue, _tail, _currFrameSize));
        _currFrameSize = 0;
    }

    public void ReleaseCompletedFrames(ulong completedFenceValue) {
        while (_completedFrameTails.TryPeek(out var peeked) && peeked.FenceValue <= completedFenceValue) {
            Debug.Assert(peeked.Size <= _usedSize);

            _usedSize -= peeked.Size;
            _head = peeked.Offset;

            _completedFrameTails.Dequeue();
        }
    }

    protected Queue<AllocationAttribute> _completedFrameTails;
    protected ulong _head, _tail, _usedSize, _currFrameSize;
    private ulong _maxSize;
}
using System.Buffers;
using System.Runtime.InteropServices;

namespace RiptideEngine.Core.Allocation;

public sealed unsafe class SegmentedMemoryBlock(int capacity) : IDisposable {
    private const int MinimumBlockSize = 16;

    private Block _current = new(int.Max(capacity, MinimumBlockSize), 0);
    private int _blockOffset;

    public int Length => _blockOffset + _current.Length;

    private bool _disposed;

    public SegmentedMemoryBlock() : this(MinimumBlockSize) { }

    public SegmentedMemoryBlock Write(ReadOnlySpan<byte> data) {
        fixed (byte* pData = data) {
            return Write(pData, data.Length);
        }
    }

    public SegmentedMemoryBlock WriteReinterpret<T>(in T value) where T : unmanaged {
        fixed (T* pData = &value) {
            return Write(pData, sizeof(T));
        }
    }
    
    public SegmentedMemoryBlock WriteReinterpret<T>(ReadOnlySpan<T> span) where T : unmanaged {
        return Write(MemoryMarshal.AsBytes(span));
    }

    public SegmentedMemoryBlock Write(void* data, int size) {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var writeOffset = _current.Length;
        var capacity = _current.Capacity;
        
        if ((uint)writeOffset + (uint)size > (uint)capacity) {
            int firstLength = capacity - writeOffset;
            int remainLength = size - firstLength;
            
            // Write first part
            new ReadOnlySpan<byte>(data, firstLength).CopyTo(_current.Buffer.AsSpan(writeOffset));
            _current.Length = capacity;
            _blockOffset += capacity;
            
            // Preserve current block.
            var previous = _current;
            _current = new(int.Max(MinimumBlockSize, remainLength), _blockOffset) {
                Previous = previous,
            };
            
            // Write remain
            new ReadOnlySpan<byte>((byte*)data + firstLength, remainLength).CopyTo(_current.Buffer);
            _current.Length = remainLength;
        } else {
            new ReadOnlySpan<byte>(data, size).CopyTo(_current.Buffer.AsSpan(_current.Length));
            _current.Length += size;
        }
        
        return this;
    }

    public SegmentedMemoryBlock Read(Span<byte> destination) {
        if (destination.IsEmpty || Length == 0) return this;
        
        ObjectDisposedException.ThrowIf(_disposed, this);
        
        var current = _current;
        do {
            if (current.Offset >= destination.Length) goto next;
            
            ref var dest = ref destination[current.Offset];
            ref var src = ref current.Buffer[0];

            if (current.Offset + current.Length > destination.Length) {
                Unsafe.CopyBlock(ref dest, ref src, (uint)(destination.Length - current.Offset));
            } else {
                Unsafe.CopyBlock(ref dest, ref src, (uint)current.Length);
            }

            next:
            current = current.Previous;
        } while (current != null);

        return this;
    }
    
    // TODO: Read with start offset.
    // public int Read(Span<byte> data, int offset) {
    //     if (offset >= _length || data.IsEmpty) return 0;
    //
    //     ArgumentOutOfRangeException.ThrowIfNegative(offset);
    //     
    //     int readBytes = 0;
    //
    //     return readBytes;
    // }

    private void Dispose(bool disposing) {
        if (_disposed) return;

        if (disposing) {
            _current.Dispose();
        }
        
        _disposed = true;
    }

    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~SegmentedMemoryBlock() {
        Dispose(false);
    }
    
    private class Block(int capacity, int offset) : IDisposable {
        public Block? Previous;
        public readonly byte[] Buffer = ArrayPool<byte>.Shared.Rent(capacity);
        public int Length;
        public readonly int Offset = offset;
        
        public int Capacity => Buffer.Length;

        public void Dispose() {
            Previous?.Dispose();
            ArrayPool<byte>.Shared.Return(Buffer);
        }
    }
}
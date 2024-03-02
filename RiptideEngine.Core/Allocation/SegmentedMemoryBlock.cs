using System.Buffers;
using System.Runtime.InteropServices;

namespace RiptideEngine.Core.Allocation;

public sealed unsafe class SegmentedMemoryBlock : IDisposable {
    // Not gonna use LinkedList cuz shit is too general-purposed.
    
    private const int MinimumBlockSize = 128;

    private Block _current;
    private int _previousBlockOffset;

    public int Length => _previousBlockOffset + _current.WrittenLength;

    private bool _disposed;

    public SegmentedMemoryBlock() : this(MinimumBlockSize) { }
    public SegmentedMemoryBlock(int capacity) {
        _current = new(int.Max(capacity, MinimumBlockSize), 0);
    }

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

        var writeOffset = _current.WrittenLength;
        var capacity = _current.Capacity;
        
        if ((uint)writeOffset + (uint)size > (uint)capacity) {
            int firstLength = capacity - writeOffset;
            int remainLength = size - firstLength;
            
            // Write first part
            new ReadOnlySpan<byte>(data, firstLength).CopyTo(_current.Buffer.AsSpan(writeOffset));
            _current.WrittenLength = capacity;
            _previousBlockOffset += capacity;
            
            // Preserve current block.
            if (_current.Next == null) {
                var previous = _current;
                
                _current = _current.Next ?? new(int.Max(MinimumBlockSize, remainLength), _previousBlockOffset) {
                    Previous = previous,
                };
                previous.Next = _current;
            } else {
                _current = _current.Next;
            }
            
            // Write remain
            new ReadOnlySpan<byte>((byte*)data + firstLength, remainLength).CopyTo(_current.Buffer);
            _current.WrittenLength = remainLength;
        } else {
            new ReadOnlySpan<byte>(data, size).CopyTo(_current.Buffer.AsSpan(_current.WrittenLength));
            _current.WrittenLength += size;
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

            if (current.Offset + current.WrittenLength > destination.Length) {
                Unsafe.CopyBlock(ref dest, ref src, (uint)(destination.Length - current.Offset));
            } else {
                Unsafe.CopyBlock(ref dest, ref src, (uint)current.WrittenLength);
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

    public void Clear() {
        Block block = _current;

        while (block.Previous != null) {
            block.WrittenLength = 0;
            block = block.Previous;
        }

        block.WrittenLength = 0;
        _previousBlockOffset = 0;
        _current = block;
    }

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
        public Block? Previous, Next;
        public readonly byte[] Buffer = ArrayPool<byte>.Shared.Rent(capacity);
        public int WrittenLength;
        public readonly int Offset = offset;
        
        public int Capacity => Buffer.Length;

        public void Dispose() {
            Previous?.Dispose();
            ArrayPool<byte>.Shared.Return(Buffer);
        }
    }
}
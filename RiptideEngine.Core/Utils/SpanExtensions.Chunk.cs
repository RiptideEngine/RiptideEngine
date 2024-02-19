using System.Text;

namespace RiptideEngine.Core.Utils;

partial class SpanExtensions {
    public static SpanChunkEnumerator<T> Chunk<T>(this Span<T> span, int size) => new(span, size);
    public static ROSpanChunkEnumerator<T> Chunk<T>(this ReadOnlySpan<T> span, int size) => new(span, size);
    
    public ref struct SpanChunkEnumerator<T> {
        private Span<T> _remain;
        private Span<T> _current;

        private readonly int _size;
        
        internal SpanChunkEnumerator(Span<T> span, int size) {
            _remain = span;
            _size = size;
            _current = default;
        }

        public Span<T> Current => _current;
        
        public bool MoveNext() {
            if (_remain.IsEmpty) return false;

            if (_remain.Length <= _size) {
                _current = _remain;
                _remain = default;
            } else {
                _current = _remain[.._size];
                _remain = _remain[_size..];
            }
            
            return true;
        }
        
        public SpanChunkEnumerator<T> GetEnumerator() => this;
    }

    public ref struct ROSpanChunkEnumerator<T> {
        private ReadOnlySpan<T> _remain;
        private ReadOnlySpan<T> _current;

        private readonly int _size;
        
        internal ROSpanChunkEnumerator(ReadOnlySpan<T> span, int size) {
            _remain = span;
            _size = size;
            _current = default;
        }

        public ReadOnlySpan<T> Current => _current;
        
        public bool MoveNext() {
            if (_remain.IsEmpty) return false;

            if (_remain.Length <= _size) {
                _current = _remain;
                _remain = default;
            } else {
                _current = _remain[.._size];
                _remain = _remain[_size..];
            }
            
            return true;
        }
        
        public ROSpanChunkEnumerator<T> GetEnumerator() => this;
    }
}
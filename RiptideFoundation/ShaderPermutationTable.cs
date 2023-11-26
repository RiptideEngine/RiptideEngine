// using System.Collections;
//
// namespace RiptideFoundation;
//
// public sealed class ShaderPermutationTable : IDisposable {
//     private PermutationTable _table;
//     private FeatureKeyword[] _keywords;
//
//     private ImmutableArray<byte> _source;
//
//     private readonly object _lock;
//     
//     private bool _disposed = false;
//
//     public ShaderPermutationTable(ReadOnlySpan<byte> shaderSourceCode, IEnumerable<string> featureKeywords) {
//         _keywords = featureKeywords.Select(x => new FeatureKeyword(x, false)).ToArray();
//         _table = [];
//         _source = shaderSourceCode.ToImmutableArray();
//
//         _lock = new();
//     }
//
//     public bool EnableKeyword(string keyword) {
//         lock (_lock) {
//             ref var kw = ref SearchKeyword(keyword);
//             if (Unsafe.IsNullRef(ref kw)) return false;
//
//             kw.Enabled = true;
//             return true;
//         }
//     }
//     
//     public bool DisableKeyword(string keyword) {
//         lock (_lock) {
//             ref var kw = ref SearchKeyword(keyword);
//             if (Unsafe.IsNullRef(ref kw)) return false;
//
//             kw.Enabled = false;
//             return true;
//         }
//     }
//     
//     public bool ToggleKeyword(string keyword) {
//         lock (_lock) {
//             ref var kw = ref SearchKeyword(keyword);
//             if (Unsafe.IsNullRef(ref kw)) return false;
//
//             kw.Enabled ^= true;
//             return true;
//         }
//     }
//
//     public bool IsKeywordEnabled(string keyword) {
//         lock (_lock) {
//             ref var kw = ref SearchKeyword(keyword);
//             return !Unsafe.IsNullRef(ref kw) && kw.Enabled;
//         }
//     }
//
//     public GraphicalShader Get(Func<nint> compilerFactory) {
//         int hash = 0;
//         foreach (var keyword in _keywords) {
//             if (keyword.Enabled) {
//                 hash = HashCode.Combine(hash, keyword.Keyword);
//             }
//         }
//
//         lock (_lock) {
//             if (_table.TryGetValue(hash, out var shader)) return shader;
//             
//             var pipeline = 
//         }
//     }
//
//     private ref FeatureKeyword SearchKeyword(string keyword) {
//         if (_disposed) return ref Unsafe.NullRef<FeatureKeyword>();
//         
//         foreach (ref var kw in _keywords.AsSpan()) {
//             if (kw.Keyword == keyword) {
//                 return ref kw;
//             }
//         }
//
//         return ref Unsafe.NullRef<FeatureKeyword>();
//     }
//
//     public IEnumerable<string> EnumerateEnabledKeywords() => new EnabledKeywordEnumerator(_keywords);
//
//     private void Dispose(bool disposeManaged) {
//         if (_disposed) return;
//
//         _keywords = [];
//         _source = [];
//
//         foreach ((_, var shader) in _table) {
//             shader.DecrementReference();
//         }
//         _table.Clear();
//
//         _disposed = true;
//     }
//
//     public void Dispose() {
//         Dispose(true);
//         GC.SuppressFinalize(this);
//     }
//
//     ~ShaderPermutationTable() {
//         Dispose(false);
//     }
//
//     private struct EnabledKeywordEnumerator(IReadOnlyList<FeatureKeyword> keywords) : IEnumerator<string>, IEnumerable<string> {
//         private int _index = 0;
//         private string _current = null!;
//
//         public string Current => _current;
//         object IEnumerator.Current => _current;
//
//
//         public bool MoveNext() {
//             while (_index < keywords.Count && !keywords[_index].Enabled) {
//                 _index++;
//             }
//             
//             if (_index >= keywords.Count) return false;
//             
//             _current = keywords[_index++].Keyword;
//             return true;
//         }
//
//         public void Reset() {
//             _index = -1;
//         }
//
//         public IEnumerator<string> GetEnumerator() => this;
//         IEnumerator IEnumerable.GetEnumerator() => this;
//         
//         public void Dispose() { }
//     }
//     private record struct FeatureKeyword(string Keyword, bool Enabled);
//     private sealed class PermutationTable : Dictionary<int, GraphicalShader>;
// }
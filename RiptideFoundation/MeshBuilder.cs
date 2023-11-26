// namespace RiptideFoundation; 
//
// public sealed class MeshBuilder : IDisposable {
//     private bool _disposed;
//
//     public MeshBuilder(Func<List<Vertex>>) {
//         
//     }
//
//     public void Dispose() {
//         GC.SuppressFinalize(this);
//         Dispose(true);
//     }
//
//     private void Dispose(bool disposeManaged) {
//         if (_disposed) return;
//
//         if (disposeManaged) {
//             
//         }
//
//         _disposed = true;
//     }
//
//     ~MeshBuilder() {
//         Dispose(false);
//     }
// }
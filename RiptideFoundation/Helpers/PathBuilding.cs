namespace RiptideFoundation.Helpers;

public static partial class PathBuilding {
    internal static void Build(MeshBuilder builder, ReadOnlySpan<PathOperation> operations, float thickness, Color32 color, VertexWriter<Vertex> writer, IndexFormat indexFormat) {
        Vector2 penPosition = Vector2.Zero;
        Optional<int> subpathStartIndex = Optional<int>.Null;
        int startIndex;
        
        for (int i = 0; i < operations.Length; i++) {
            ref readonly var operation = ref operations[i];

            switch (operation.Type) {
                case PathOperationType.SetColor:
                    if (subpathStartIndex.HasValue) continue;

                    color = operation.Color;
                    break;
                
                case PathOperationType.SetThickness:
                    if (subpathStartIndex.HasValue) continue;

                    thickness = operation.Thickness;
                    break;
                
                case PathOperationType.LineTo or PathOperationType.QuadraticBezier or PathOperationType.CubicBezier:
                    if (!subpathStartIndex.HasValue) {
                        subpathStartIndex = i;
                    }
                    break;

                case PathOperationType.MoveTo: {
                    if (subpathStartIndex.TryGet(out startIndex)) {
                        BuildSubpath(builder, operations[startIndex..i], ref penPosition, ref thickness, ref color, writer, indexFormat);
                    }

                    subpathStartIndex = i + 1;
                    penPosition = operation.Move.Destination;
                    break;
                }

                case PathOperationType.Close: {
                    if (!subpathStartIndex.TryGet(out startIndex)) continue;
                    
                    BuildSubpath(builder, operations[startIndex..(i + 1)], ref penPosition, ref thickness, ref color, writer, indexFormat);

                    subpathStartIndex = Optional<int>.Null;
                    break;
                }
            }
        }

        if (subpathStartIndex.TryGet(out startIndex)) {
            BuildSubpath(builder, operations[startIndex..], ref penPosition, ref thickness, ref color, writer, indexFormat);
        }
    }
    private static void BuildSubpath(MeshBuilder builder, ReadOnlySpan<PathOperation> operations, ref Vector2 penPosition, ref float thickness, ref Color32 color, VertexWriter<Vertex> writer, IndexFormat indexFormat) {
        if (operations.IsEmpty) return;
        
        if (operations[^1] is { Type: PathOperationType.Close, Close.Loop: true }) {
            BuildSubpathLoop(builder, operations, ref penPosition, ref thickness, ref color, writer, indexFormat);
        } else {
            BuildSubpathNoLoop(builder, operations, ref penPosition, ref thickness, ref color, writer, indexFormat);
        }
    }
    
    private readonly record struct PointAttribute(float Thickness, Color32 Color);
    public readonly record struct Vertex(Vector2 Position, Color32 Color);
}
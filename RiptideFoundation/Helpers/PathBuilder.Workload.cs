﻿using SixLabors.ImageSharp;
using Color = RiptideMathematics.Color;

namespace RiptideFoundation.Helpers;

// TODO: Round, Bevel joints.
// TODO: Bezier Curve resolution.

file readonly record struct PointAttribute(float Thickness, Color32 Color);

public partial struct PathBuilder {
    private enum OperationType {
        SetColor,
        SetThickness,
        MoveTo,
        LineTo,
        HorizontalLine,
        VerticalLine,
        QuadraticBezier,
        CubicBezier,
        Close,
    }
    
    [StructLayout(LayoutKind.Explicit)]
    private readonly struct PathOperation {
        [field: FieldOffset(0)] public required OperationType Type { get; init; }
        [field: FieldOffset(4)] public MoveOperation Move { get; init; }
        [field: FieldOffset(4)] public LineOperation Line { get; init; }
        [field: FieldOffset(4)] public AxisAlignedLineOperation HorizontalLine { get; init; }
        [field: FieldOffset(4)] public AxisAlignedLineOperation VerticalLine { get; init; }
        [field: FieldOffset(4)] public Color32 Color { get; init; }
        [field: FieldOffset(4)] public float Thickness { get; init; }
        [field: FieldOffset(4)] public QuadraticBezierOperation QuadraticBezier { get; init; }
        [field: FieldOffset(4)] public CubicBezierOperation CubicBezier { get; init; }
        [field: FieldOffset(4)] public CloseOperation Close { get; init; }

        public struct MoveOperation {
            public required Vector2 Destination;
        }

        public struct LineOperation {
            public required Vector2 Destination;
        }

        public struct AxisAlignedLineOperation {
            public required float Destination;
        }

        public readonly record struct QuadraticBezierOperation(Vector2 Control, Vector2 Destination);
        public readonly record struct CubicBezierOperation(Vector2 StartControl, Vector2 EndControl, Vector2 Destination);
        
        public struct CloseOperation {
            public required bool Loop;
        }
    }

    private static void Build(MeshBuilder builder, ReadOnlySpan<PathOperation> operations, float thickness, Color32 color, VertexWriter<Vertex> writer, IndexFormat indexFormat) {
        Vector2 penPosition = Vector2.Zero;
        Optional<int> subpathStartIndex = Optional<int>.Null;
        int index;

        for (int i = 0; i < operations.Length; i++) {
            ref readonly var operation = ref operations[i];

            switch (operation.Type) {
                case OperationType.SetColor:
                    if (subpathStartIndex.HasValue) continue;

                    color = operation.Color;
                    break;
                
                case OperationType.SetThickness:
                    if (subpathStartIndex.HasValue) continue;

                    thickness = operation.Thickness;
                    break;
                
                case OperationType.LineTo or OperationType.QuadraticBezier or OperationType.CubicBezier:
                    if (!subpathStartIndex.HasValue) {
                        subpathStartIndex = i;
                    }
                    break;

                case OperationType.MoveTo: {
                    if (subpathStartIndex.TryGet(out index)) {
                        if (i - index > 0) {
                            BuildSubpath(builder, operations[index..i], ref penPosition, ref thickness, ref color, writer, indexFormat);
                        }
                    }

                    subpathStartIndex = i + 1;
                    penPosition = operation.Move.Destination;
                    break;
                }

                case OperationType.Close: {
                    if (!subpathStartIndex.TryGet(out index)) continue;

                    if (i - index > 1) {
                        BuildSubpath(builder, operations[index..(i + 1)], ref penPosition, ref thickness, ref color, writer, indexFormat);
                    }

                    subpathStartIndex = Optional<int>.Null;
                    break;
                }
            }
        }

        if (subpathStartIndex.TryGet(out index)) {
            if (operations.Length - index > 1) {
                BuildSubpath(builder, operations[index..], ref penPosition, ref thickness, ref color, writer, indexFormat);
            }
        }
    }
    
    private static void BuildSubpath(MeshBuilder builder, ReadOnlySpan<PathOperation> operations, ref Vector2 penPosition, ref float thickness, ref Color32 color, VertexWriter<Vertex> writer, IndexFormat indexFormat) {
        bool loop = operations[^1] is { Type: OperationType.Close, Close.Loop: true };
        
        const float lineDistanceThreshold = 0.001f;

        if (loop) {
            // Plot normally from the second draw operation, and execute the remain and the first like a whole.
            
            
            throw new NotImplementedException("Looping is currently being implemented.");
        } else {
            var firstPointAttribute = Optional<PointAttribute>.From(new(thickness, color));
            var previousPointAttribute = firstPointAttribute;
            
            for (int i = 0; i < operations.Length; i++) {
                ref readonly var operation = ref operations[i];

                switch (operation.Type) {
                    case OperationType.SetThickness: thickness = operation.Thickness; break;
                    case OperationType.SetColor: color = operation.Color; break;
                    case OperationType.MoveTo: throw new UnreachableException("MoveTo is unexpected because it is associated with closing the current subpath (which marks the slicing boundary).");

                    case OperationType.LineTo: {
                        PlotLineTo(builder, operations[(i + 1)..], ref penPosition, operation.Line.Destination, thickness, color, ref firstPointAttribute, ref previousPointAttribute, writer, indexFormat);
                        break;
                    }

                    case OperationType.HorizontalLine: {
                        PlotLineTo(builder, operations[(i + 1)..], ref penPosition, new(operation.HorizontalLine.Destination, penPosition.Y), thickness, color, ref firstPointAttribute, ref previousPointAttribute, writer, indexFormat);
                        break;
                    }
                    
                    case OperationType.VerticalLine: {
                        PlotLineTo(builder, operations[(i + 1)..], ref penPosition, new(penPosition.X, operation.HorizontalLine.Destination), thickness, color, ref firstPointAttribute, ref previousPointAttribute, writer, indexFormat);
                        break;
                    }
                    
                    case OperationType.QuadraticBezier: {
                        const int resolution = 16;

                        (Vector2 control, Vector2 destination) = operation.QuadraticBezier;

                        float previousThickness;
                        Color32 previousColor;
                        
                        if (firstPointAttribute.TryGet(out var first)) {
                            var direction = Vector2.Normalize(QuadraticBezier.GetVelocity(penPosition, control, destination, 0));
                            var normal = new Vector2(-direction.Y, direction.X) / 2;
                            
                            writer(builder, new(penPosition + normal * first.Thickness, first.Color));
                            writer(builder, new(penPosition - normal * first.Thickness, first.Color));

                            (previousThickness, previousColor) = first;

                            firstPointAttribute = Optional<PointAttribute>.Null;
                        } else {
                            Debug.Assert(previousPointAttribute.HasValue, "previousPointAttribute.HasValue");

                            (previousThickness, previousColor) = previousPointAttribute.GetUnchecked();
                        }
                        
                        for (int s = 1; s < resolution; s++) {
                            float t = 1f / resolution * s;
                            
                            var position = QuadraticBezier.GetPosition(penPosition, control, destination, t);
                            var direction = Vector2.Normalize(QuadraticBezier.GetVelocity(penPosition, control, destination, t));
                            
                            var normal = new Vector2(-direction.Y, direction.X);
                            
                            var vcount = builder.GetLargestWrittenVertexCount();

                            float segmentThickness = float.Lerp(previousThickness, thickness, t) / 2;
                            Color32 segmentColor = (Color32)Color.Lerp(previousColor, color, t);
                            
                            writer(builder, new(position + normal * segmentThickness, segmentColor));
                            writer(builder, new(position - normal * segmentThickness, segmentColor));
                            
                            WriteQuadIndices(builder, indexFormat, (uint)vcount);
                        }

                        {
                            var direction = Vector2.Normalize(QuadraticBezier.GetVelocity(penPosition, control, destination, 1));
                            var normal = new Vector2(-direction.Y, direction.X);

                            var vcount = builder.GetLargestWrittenVertexCount();
                            GenerateJointVerticesPair(builder, operations[(i + 1)..], destination, direction, normal, color, thickness, writer);
                            WriteQuadIndices(builder, indexFormat, (uint)vcount);
                        }
                        
                        previousPointAttribute = Optional<PointAttribute>.From(new(thickness, color));
                        penPosition = destination;
                        break;
                    }

                    case OperationType.CubicBezier: {
                        const int resolution = 16;

                        (Vector2 startControl, Vector2 endControl, Vector2 destination) = operation.CubicBezier;

                        float previousThickness;
                        Color32 previousColor;
                        
                        if (firstPointAttribute.TryGet(out var first)) {
                            var direction = Vector2.Normalize(CubicBezier.GetVelocity(penPosition, startControl, endControl, destination, 0));
                            var normal = new Vector2(-direction.Y, direction.X) / 2;
                            
                            writer(builder, new(penPosition + normal * first.Thickness, first.Color));
                            writer(builder, new(penPosition - normal * first.Thickness, first.Color));

                            (previousThickness, previousColor) = first;

                            firstPointAttribute = Optional<PointAttribute>.Null;
                        } else {
                            Debug.Assert(previousPointAttribute.HasValue, "previousPointAttribute.HasValue");

                            (previousThickness, previousColor) = previousPointAttribute.GetUnchecked();
                        }
                        
                        for (int s = 1; s < resolution; s++) {
                            float t = 1f / resolution * s;
                            
                            var position = CubicBezier.GetPosition(penPosition, startControl, endControl, destination, t);
                            var direction = Vector2.Normalize(CubicBezier.GetVelocity(penPosition, startControl, endControl, destination, t));
                            
                            var normal = new Vector2(-direction.Y, direction.X);
                            
                            var vcount = builder.GetLargestWrittenVertexCount();

                            float segmentThickness = float.Lerp(previousThickness, thickness, t) / 2;
                            Color32 segmentColor = (Color32)Color.Lerp(previousColor, color, t);
                            
                            writer(builder, new(position + normal * segmentThickness, segmentColor));
                            writer(builder, new(position - normal * segmentThickness, segmentColor));
                            
                            WriteQuadIndices(builder, indexFormat, (uint)vcount);
                        }
                        
                        {
                            var direction = Vector2.Normalize(CubicBezier.GetVelocity(penPosition, startControl, endControl, destination, 1));
                            var normal = new Vector2(-direction.Y, direction.X);

                            var vcount = builder.GetLargestWrittenVertexCount();
                            GenerateJointVerticesPair(builder, operations[(i + 1)..], destination, direction, normal, color, thickness, writer);
                            WriteQuadIndices(builder, indexFormat, (uint)vcount);
                        }
                        
                        previousPointAttribute = Optional<PointAttribute>.From(new(thickness, color));
                        penPosition = destination;
                        break;
                    }
                }
            }
        }

        static Optional<Ray2D> GetSecondIntersectionRay(ReadOnlySpan<PathOperation> operations, Vector2 position, float thickness, float lineDistanceThreshold) {
            foreach (ref readonly var operation in operations) {
                switch (operation.Type) {
                    case OperationType.SetThickness: thickness = operation.Thickness; break;
                    case OperationType.LineTo: {
                        var destination = operation.Line.Destination;

                        if (Vector2.DistanceSquared(position, destination) <= lineDistanceThreshold) continue;

                        var direction = Vector2.Normalize(destination - position);
                        var normal = new Vector2(-direction.Y, direction.X);

                        return Optional<Ray2D>.From(Ray2D.CreateWithoutNormalize(position + normal * thickness / 2, -direction));
                    }

                    case OperationType.QuadraticBezier: {
                        var direction = Vector2.Normalize(QuadraticBezier.GetVelocity(position, operation.QuadraticBezier.Control, operation.QuadraticBezier.Destination, 0));
                        var normal = new Vector2(-direction.Y, direction.X);

                        return Optional<Ray2D>.From(Ray2D.CreateWithoutNormalize(position + normal * thickness / 2, -direction));
                    }

                    case OperationType.CubicBezier: {
                        var direction = Vector2.Normalize(CubicBezier.GetVelocity(position, operation.CubicBezier.StartControl, operation.CubicBezier.EndControl, operation.CubicBezier.Destination, 0));
                        var normal = new Vector2(-direction.Y, direction.X);

                        return Optional<Ray2D>.From(Ray2D.CreateWithoutNormalize(position + normal * thickness / 2, -direction));
                    }

                    case OperationType.HorizontalLine: {
                        var destinationX = operation.HorizontalLine.Destination;

                        if (float.Abs(position.X - destinationX) <= lineDistanceThreshold) continue;

                        var direction = new Vector2(float.Sign(destinationX - position.X), 0);
                        var normal = new Vector2(-direction.Y, direction.X);

                        return Optional<Ray2D>.From(Ray2D.CreateWithoutNormalize(position + normal * thickness / 2, -direction));
                    }

                    case OperationType.VerticalLine: {
                        var destinationY = operation.VerticalLine.Destination;

                        if (float.Abs(position.Y - destinationY) <= lineDistanceThreshold) continue;

                        var direction = new Vector2(0, float.Sign(destinationY - position.Y));
                        var normal = new Vector2(-direction.Y, direction.X);

                        return Optional<Ray2D>.From(Ray2D.CreateWithoutNormalize(position + normal * thickness / 2, -direction));
                    }
                }
            }
        
            return Optional<Ray2D>.Null;
        }

        static void GenerateJointVerticesPair(MeshBuilder builder, ReadOnlySpan<PathOperation> operations, Vector2 position, Vector2 direction, Vector2 normal, Color32 color, float thickness, VertexWriter<Vertex> writer) {
            Ray2D ray1 = Ray2D.CreateWithoutNormalize(position + normal * thickness / 2, direction);
            if (GetSecondIntersectionRay(operations, position, thickness, lineDistanceThreshold).TryGet(out var ray2)) {
                var intersect = Intersection.Test(ray1, ray2);

                if (intersect.HasValue) {
                    writer(builder, new(intersect.Value, color));
                    writer(builder, new(position - (intersect.Value - position), color));
                } else {
                    writer(builder, new(position + normal * thickness / 2, color));
                    writer(builder, new(position - normal * thickness / 2, color));
                }
            } else {
                writer(builder, new(position + normal * thickness / 2, color));
                writer(builder, new(position - normal * thickness / 2, color));
            }
        }

        static void PlotLineTo(MeshBuilder builder, ReadOnlySpan<PathOperation> remainOperations, ref Vector2 penPosition, Vector2 lineDestination, float thickness, Color32 color, ref Optional<PointAttribute> firstPointAttribute, ref Optional<PointAttribute> previousPointAttribute, VertexWriter<Vertex> writer, IndexFormat indexFormat) {
            if (Vector2.DistanceSquared(penPosition, lineDestination) <= lineDistanceThreshold) {
                penPosition = lineDestination;
                return;
            }

            var direction = Vector2.Normalize(lineDestination - penPosition);
            var normal = new Vector2(-direction.Y, direction.X);

            if (firstPointAttribute.TryGet(out var first)) {
                writer(builder, new(penPosition + normal * first.Thickness / 2, first.Color));
                writer(builder, new(penPosition - normal * first.Thickness / 2, first.Color));

                firstPointAttribute = Optional<PointAttribute>.Null;
            }

            var vcount = builder.GetLargestWrittenVertexCount();
            GenerateJointVerticesPair(builder, remainOperations, lineDestination, direction, normal, color, thickness, writer);
                        
            WriteQuadIndices(builder, indexFormat, (uint)vcount);
                        
            penPosition = lineDestination;
            previousPointAttribute = Optional<PointAttribute>.From(new(thickness, color));
        }
        
        static void WriteQuadIndices(MeshBuilder builder, IndexFormat format, uint start) {
            if (format == IndexFormat.UInt16) {
                builder.WriteIndices(stackalloc ushort[] {
                    (ushort)(start - 2),
                    (ushort)start,
                    (ushort)(start + 1),
                    (ushort)(start + 1),
                    (ushort)(start - 1),
                    (ushort)(start - 2),
                });
            } else {
                builder.WriteIndices(stackalloc uint[] {
                    start + 0,
                    start + 2,
                    start + 3,
                    start + 3,
                    start + 1,
                    start + 0,
                });
            }
        }
    }
}
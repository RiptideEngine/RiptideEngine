﻿namespace RiptideFoundation.Helpers;

partial class PathBuilding {
    private static void WriteSegmentQuadIndices(MeshBuilder builder, IndexFormat format, uint vcount, WindingDirection windingDirection) {
        if (windingDirection == WindingDirection.Clockwise) {
            ConnectQuadFromIndices(builder, format, vcount + 1, vcount - 1, vcount - 2, vcount - 2, vcount, vcount + 1);
        } else {
            ConnectQuadFromIndices(builder, format, vcount - 2, vcount, vcount + 1, vcount + 1, vcount - 1, vcount - 2);
        }
    }
    
    private static void ConnectQuadFromIndices(MeshBuilder builder, IndexFormat format, uint i1, uint i2, uint i3, uint i4, uint i5, uint i6) {
        if (format == IndexFormat.UInt16) {
            builder.WriteIndices(stackalloc ushort[] {
                (ushort)i1, (ushort)i2, (ushort)i3,
                (ushort)i4, (ushort)i5, (ushort)i6,
            });
        } else {
            builder.WriteIndices(stackalloc uint[] {
                i1, i2, i3,
                i4, i5, i6,
            });
        }
    }
    
    private static void PlotQuadraticCurveBody(MeshBuilder builder, Vector2 penPosition, Vector2 control, Vector2 destination, int resolution, PointAttribute previousAttribute, PointAttribute attribute, WindingDirection windingDirection, VertexWriter<Vertex> writer, IndexFormat indexFormat) {
        float stepSize = 1f / (resolution + 1);
        
        for (int i = 1; i <= resolution; i++) {
            float t = stepSize * i;
            
            var position = QuadraticBezier.GetPosition(penPosition, control, destination, t);
            var direction = Vector2.Normalize(QuadraticBezier.GetVelocity(penPosition, control, destination, t));
            
            var normal = new Vector2(-direction.Y, direction.X);
            
            var vcount = builder.GetLargestWrittenVertexCount();

            float segmentThickness = float.Lerp(previousAttribute.Thickness, attribute.Thickness, t) / 2;
            Color32 segmentColor = (Color32)Color.Lerp(previousAttribute.Color, attribute.Color, t);
            
            writer(builder, new(position + normal * segmentThickness, segmentColor));
            writer(builder, new(position - normal * segmentThickness, segmentColor));
            
            WriteSegmentQuadIndices(builder, indexFormat, (uint)vcount, windingDirection);
        }
    }
    private static void PlotCubicCurveBody(MeshBuilder builder, Vector2 penPosition, Vector2 startControl, Vector2 endControl, Vector2 destination, int resolution, PointAttribute previousAttribute, PointAttribute attribute, WindingDirection windingDirection, VertexWriter<Vertex> writer, IndexFormat indexFormat) {
        float stepSize = 1f / (resolution + 1);
        
        for (int s = 1; s <= resolution; s++) {
            float t = stepSize * s;
            
            var position = CubicBezier.GetPosition(penPosition, startControl, endControl, destination, t);
            var direction = Vector2.Normalize(CubicBezier.GetVelocity(penPosition, startControl, endControl, destination, t));

            var normal = new Vector2(-direction.Y, direction.X);
            
            var vcount = builder.GetLargestWrittenVertexCount();

            float segmentThickness = float.Lerp(previousAttribute.Thickness, attribute.Thickness, t) / 2;
            Color32 segmentColor = (Color32)Color.Lerp(previousAttribute.Color, attribute.Color, t);
            
            writer(builder, new(position + normal * segmentThickness, segmentColor));
            writer(builder, new(position - normal * segmentThickness, segmentColor));
            
            WriteSegmentQuadIndices(builder, indexFormat, (uint)vcount, windingDirection);
        }
    }
    
    private static Optional<Ray2D> GetNextIntersectionRay(ReadOnlySpan<PathOperation> operations, Vector2 position, float thickness, float lineDistanceThreshold) {
        foreach (ref readonly var operation in operations) {
            switch (operation.Type) {
                case PathOperationType.SetThickness: thickness = operation.Thickness; break;
                case PathOperationType.LineTo: {
                    var destination = operation.Line.Destination;

                    if (Vector2.DistanceSquared(position, destination) <= lineDistanceThreshold) continue;

                    var direction = Vector2.Normalize(destination - position);
                    var normal = new Vector2(-direction.Y, direction.X);

                    return Optional<Ray2D>.From(Ray2D.CreateWithoutNormalize(position + normal * thickness / 2, -direction));
                }

                case PathOperationType.QuadraticBezier: {
                    var direction = Vector2.Normalize(QuadraticBezier.GetVelocity(position, operation.QuadraticBezier.Control, operation.QuadraticBezier.Destination, 0));
                    var normal = new Vector2(-direction.Y, direction.X);

                    return Optional<Ray2D>.From(Ray2D.CreateWithoutNormalize(position + normal * thickness / 2, -direction));
                }

                case PathOperationType.CubicBezier: {
                    var direction = Vector2.Normalize(CubicBezier.GetVelocity(position, operation.CubicBezier.StartControl, operation.CubicBezier.EndControl, operation.CubicBezier.Destination, 0));
                    var normal = new Vector2(-direction.Y, direction.X);

                    return Optional<Ray2D>.From(Ray2D.CreateWithoutNormalize(position + normal * thickness / 2, -direction));
                }

                default: continue;
            }
        }
    
        return Optional<Ray2D>.Null;
    }
    
    private enum WindingDirection {
        Unknown,
        Clockwise,
        CounterClockwise,
    }
}
namespace RiptideFoundation.Helpers;

partial class PathBuilding {
    private static void WriteSegmentQuadIndices(MeshBuilder builder, IndexFormat format, uint vcount, WindingDirection windingDirection) {
        if (windingDirection == WindingDirection.Clockwise) {
            ConnectTrianglePairFromIndices(builder, format, vcount + 1, vcount - 1, vcount - 2, vcount - 2, vcount, vcount + 1);
        } else {
            ConnectTrianglePairFromIndices(builder, format, vcount - 2, vcount, vcount + 1, vcount + 1, vcount - 1, vcount - 2);
        }
    }
    
    private static void ConnectTriangleFromIndices(MeshBuilder builder, IndexFormat format, uint i1, uint i2, uint i3) {
        if (format == IndexFormat.UInt16) {
            builder.WriteIndices(stackalloc ushort[] {
                (ushort)i1, (ushort)i2, (ushort)i3,
            });
        } else {
            builder.WriteIndices(stackalloc uint[] {
                i1, i2, i3,
            });
        }
    }
    
    private static void ConnectTrianglePairFromIndices(MeshBuilder builder, IndexFormat format, uint i1, uint i2, uint i3, uint i4, uint i5, uint i6) {
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

    private static void ExecuteRemainConfiguratingOperations(ReadOnlySpan<PathOperation> operations, ref float thickness, ref Color32 color) {
        foreach (ref readonly var operation in operations) {
            switch (operation.Type) {
                case PathOperationType.SetThickness: thickness = operation.Thickness; break;
                case PathOperationType.SetColor: color = operation.Color; break;
            }
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
    
    private static WindingDirection CalculateWindingDirection(Vector2 previousPosition, Vector2 position, ReadOnlySpan<PathOperation> nextOperations, in PathBuildingConfiguration config) {
        float lineDistanceThreshold = config.LineDistanceThreshold;
        
        foreach (ref readonly var operation in nextOperations) {
            switch (operation.Type) {
                case PathOperationType.SetColor or PathOperationType.SetThickness: continue;
                
                case PathOperationType.LineTo: {
                    var nextPosition = operation.Line.Destination;

                    var d1 = position - previousPosition;
                    var d2 = nextPosition - position;

                    var winding = d1.X * d2.Y - d2.X * d1.Y;

                    return winding switch {
                        0 => WindingDirection.Unknown,
                        > 0 => WindingDirection.CounterClockwise,
                        _ => WindingDirection.Clockwise,
                    };
                }

                case PathOperationType.QuadraticBezier: {
                    (var control, var end) = operation.QuadraticBezier;
                    
                    var d1 = position - previousPosition;
                    var d2 = QuadraticBezier.GetVelocity(position, control, end, 0);

                    var winding = d1.X * d2.Y - d2.X * d1.Y;
                    
                    return winding switch {
                        0 => WindingDirection.Unknown,
                        > 0 => WindingDirection.CounterClockwise,
                        _ => WindingDirection.Clockwise,
                    };
                }

                case PathOperationType.CubicBezier: {
                    (var startControl, var endControl, var end) = operation.CubicBezier;
                    
                    var d1 = position - previousPosition;
                    var d2 = CubicBezier.GetVelocity(position, startControl, endControl, end, 0);

                    var winding = d1.X * d2.Y - d2.X * d1.Y;
                    
                    return winding switch {
                        0 => WindingDirection.Unknown,
                        > 0 => WindingDirection.CounterClockwise,
                        _ => WindingDirection.Clockwise,
                    };
                }
            }
        }

        return WindingDirection.Unknown;
    }
    
    private static bool CalculateIntersectionRays(ReadOnlySpan<PathOperation> operations, Vector2 position, Vector2 direction, Vector2 normal, float thickness, WindingDirection windingDirection, in PathBuildingConfiguration config, out Ray2D ray1, out Ray2D ray2) {
        float lineDistanceThreshold = config.LineDistanceThreshold;
        
        foreach (ref readonly var operation in operations) {
            switch (operation.Type) {
                case PathOperationType.SetThickness or PathOperationType.SetColor: continue;
                
                case PathOperationType.LineTo: {
                    var destination = operation.Line.Destination;

                    var direction2 = Vector2.Normalize(destination - position);
                    var normal2 = new Vector2(-direction2.Y, direction2.X);

                    if (windingDirection == WindingDirection.Clockwise) {
                        ray1 = Ray2D.CreateWithoutNormalize(position + normal / 2, direction);
                        ray2 = Ray2D.CreateWithoutNormalize(position + normal2 / 2, -direction2);
                    } else {
                        ray1 = Ray2D.CreateWithoutNormalize(position - normal / 2, direction);
                        ray2 = Ray2D.CreateWithoutNormalize(position - normal2 / 2, -direction2);
                    }
                    return true;
                }

                case PathOperationType.QuadraticBezier: {
                    (Vector2 control, Vector2 destination) = operation.QuadraticBezier;

                    var direction2 = Vector2.Normalize(QuadraticBezier.GetVelocity(position, control, destination, 0));
                    var normal2 = new Vector2(-direction2.Y, direction2.X);

                    if (windingDirection == WindingDirection.Clockwise) {
                        ray1 = Ray2D.CreateWithoutNormalize(position + normal / 2, direction);
                        ray2 = Ray2D.CreateWithoutNormalize(position + normal2 / 2, -direction2);
                    } else {
                        ray1 = Ray2D.CreateWithoutNormalize(position - normal / 2, direction);
                        ray2 = Ray2D.CreateWithoutNormalize(position - normal2 / 2, -direction2);
                    }
                    return true;
                }
                    
                case PathOperationType.CubicBezier: {
                    (Vector2 startControl, Vector2 endControl, Vector2 destination) = operation.CubicBezier;

                    var direction2 = Vector2.Normalize(CubicBezier.GetVelocity(position, startControl, endControl, destination, 0));
                    var normal2 = new Vector2(-direction2.Y, direction2.X);
                    
                    if (windingDirection == WindingDirection.Clockwise) {
                        ray1 = Ray2D.CreateWithoutNormalize(position + normal / 2, direction);
                        ray2 = Ray2D.CreateWithoutNormalize(position + normal2 / 2, -direction2);
                    } else {
                        ray1 = Ray2D.CreateWithoutNormalize(position - normal / 2, direction);
                        ray2 = Ray2D.CreateWithoutNormalize(position - normal2 / 2, -direction2);
                    }
                    return true;
                }
            }
        }

        ray1 = ray2 = default;
        return false;
    }
    
    private static void GenerateJointVerticesPair(MeshBuilder builder, ReadOnlySpan<PathOperation> operations, Vector2 position, Vector2 direction, Vector2 normal, PointAttribute attribute, WindingDirection windingDirection, in PathBuildingConfiguration config, VertexWriter<Vertex> writer) {
        (float thickness, Color32 color) = attribute;

        bool success = CalculateIntersectionRays(operations, position, direction, normal, thickness, windingDirection, config, out var ray1, out var ray2);
        if (success && Intersection.Test(ray1, ray2) is { } intersect) {
            var intersectDistance = Vector2.Distance(position, intersect);
            var extrudeDirection = Vector2.Normalize(intersect - position) * intersectDistance * thickness;

            if (windingDirection == WindingDirection.Clockwise) {
                writer(builder, new(position + extrudeDirection, color));
                writer(builder, new(position - extrudeDirection, color));
            } else {
                writer(builder, new(position - extrudeDirection, color));
                writer(builder, new(position + extrudeDirection, color));
            }
        } else {
            writer(builder, new(position + normal * thickness / 2, color));
            writer(builder, new(position - normal * thickness / 2, color));
        }
    }

    private static bool TryGetFirstAndLastPlottingOperations(ReadOnlySpan<PathOperation> operations, out int start, out int end) {
        for (int i = 0; i < operations.Length; i++) {
            ref readonly var operation = ref operations[i];

            switch (operation.Type) {
                case PathOperationType.LineTo or PathOperationType.QuadraticBezier or PathOperationType.CubicBezier:
                    start = i;

                    for (int j = operations.Length - 1; j >= i; j--) {
                        ref readonly var operation2 = ref operations[j];

                        switch (operation2.Type) {
                            case PathOperationType.LineTo or PathOperationType.QuadraticBezier or PathOperationType.CubicBezier:
                                end = j;
                                return true;
                        }
                    }

                    throw new UnreachableException();
            }
        }

        start = end = 0;
        return false;
    }
    
    private enum WindingDirection {
        Unknown,
        Clockwise,
        CounterClockwise,
    }
}
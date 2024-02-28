namespace RiptideFoundation.Helpers;

// TODO: Customizable Bezier Curve resolution.

partial class PathBuilding {
    private static void BuildSubpathNoLoop(MeshBuilder builder, ReadOnlySpan<PathOperation> operations, ref Vector2 penPosition, ref float thickness, ref Color32 color, in PathBuildingConfiguration config, VertexWriter<Vertex> writer, IndexFormat indexFormat) {
        float lineDistanceThreshold = config.LineDistanceThreshold;
        int BezierCurveResolution = config.BezierCurveResolution;
        int RoundCapResolution = config.RoundCapResolution;
        
        PathCapType capType = operations[^1] is { Type: PathOperationType.Close } close ? close.Close.CapType : PathCapType.Butt;

        var endCapInformation = Optional<CapGenerateInfo>.Null;

        PointAttribute previousPointAttribute = new(thickness, color);

        WindingDirection windingDirection = WindingDirection.Clockwise;
        
        for (int i = 0; i < operations.Length; i++) {
            ref readonly var operation = ref operations[i];

            switch (operation.Type) {
                case PathOperationType.SetThickness: thickness = operation.Thickness; break;
                case PathOperationType.SetColor: color = operation.Color; break;
                case PathOperationType.MoveTo: throw new UnreachableException("MoveTo is unexpected because it is associated with closing the current subpath (which marks the slicing boundary).");

                case PathOperationType.LineTo: {
                    Vector2 lineDestination = operation.Line.Destination;
                    var remainOperations = operations[(i + 1)..];

                    var calcWinding = CalculateWindingDirection(penPosition, lineDestination, remainOperations, config);
                    if (calcWinding != WindingDirection.Unknown && calcWinding != windingDirection) {
                        windingDirection = calcWinding;
                    }
                    
                    var direction = Vector2.Normalize(lineDestination - penPosition);
                    var normal = new Vector2(-direction.Y, direction.X);
                    
                    GenerateHeadCap(builder, new(penPosition, direction, normal, previousPointAttribute), capType, RoundCapResolution, windingDirection, writer, indexFormat);
                    
                    var vcount = builder.GetLargestWrittenVertexCount();
                    GenerateJointVerticesPair(builder, remainOperations, lineDestination, direction, normal, new(thickness, color), windingDirection, config, writer);
                    WriteSegmentQuadIndices(builder, indexFormat, (uint)vcount, windingDirection);

                    endCapInformation = new CapGenerateInfo(lineDestination, direction, normal, new(thickness, color));
                    previousPointAttribute = new(thickness, color);
                    penPosition = lineDestination;
                    goto breakLoop;
                }

                case PathOperationType.QuadraticBezier: {
                    (Vector2 control, Vector2 destination) = operation.QuadraticBezier;
                    var remainOperations = operations[(i + 1)..];

                    var calcWinding = CalculateWindingDirection(penPosition, destination, remainOperations, config);
                    if (calcWinding != WindingDirection.Unknown && calcWinding != windingDirection) {
                        windingDirection = calcWinding;
                    }
                
                    var direction = Vector2.Normalize(QuadraticBezier.GetVelocity(penPosition, control, destination, 0));
                    var normal = new Vector2(-direction.Y, direction.X);

                    GenerateHeadCap(builder, new(penPosition, direction, normal, previousPointAttribute), capType, RoundCapResolution, windingDirection, writer, indexFormat);
                    PlotQuadraticCurveBody(builder, penPosition, control, destination, BezierCurveResolution, previousPointAttribute, new(thickness, color), windingDirection, writer, indexFormat);
                    
                    direction = Vector2.Normalize(QuadraticBezier.GetVelocity(penPosition, control, destination, 1));
                    normal = new(-direction.Y, direction.X);
                    
                    var vcount = builder.GetLargestWrittenVertexCount();
                    GenerateJointVerticesPair(builder, remainOperations, destination, direction, normal, new(thickness, color), windingDirection, config, writer);
                    WriteSegmentQuadIndices(builder, indexFormat, (uint)vcount, windingDirection);
                    
                    endCapInformation = new CapGenerateInfo(destination, direction, normal, new(thickness, color));
                    previousPointAttribute = new(thickness, color);
                    penPosition = destination;
                    goto breakLoop;
                }
                
                case PathOperationType.CubicBezier: {
                    (Vector2 startControl, Vector2 endControl, Vector2 destination) = operation.CubicBezier;
                    var remainOperations = operations[(i + 1)..];

                    var calcWinding = CalculateWindingDirection(penPosition, destination, remainOperations, config);
                    if (calcWinding != WindingDirection.Unknown && calcWinding != windingDirection) {
                        windingDirection = calcWinding;
                    }
                
                    var direction = Vector2.Normalize(CubicBezier.GetVelocity(penPosition, startControl, endControl, destination, 0));
                    var normal = new Vector2(-direction.Y, direction.X);
                    
                    GenerateHeadCap(builder, new(penPosition, direction, normal, previousPointAttribute), capType, RoundCapResolution, windingDirection, writer, indexFormat);
                    PlotCubicCurveBody(builder, penPosition, startControl, endControl, destination, BezierCurveResolution, previousPointAttribute, new(thickness, color), windingDirection, writer, indexFormat);
                
                    direction = Vector2.Normalize(CubicBezier.GetVelocity(penPosition, startControl, endControl, destination, 1));
                    normal = new(-direction.Y, direction.X);
                    
                    var vcount = builder.GetLargestWrittenVertexCount();
                    GenerateJointVerticesPair(builder, remainOperations, destination, direction, normal, new(thickness, color), windingDirection, config, writer);
                    WriteSegmentQuadIndices(builder, indexFormat, (uint)vcount, windingDirection);
                    endCapInformation = new CapGenerateInfo(destination, direction, normal, new(thickness, color));
                    previousPointAttribute = new(thickness, color);
                    penPosition = destination;
                    goto breakLoop;
                }
            }

            continue;
            
            breakLoop:
            operations = operations[(i + 1)..];
            break;
        }
        
        for (int i = 0; i < operations.Length; i++) {
            ref readonly var operation = ref operations[i];

            switch (operation.Type) {
                case PathOperationType.SetThickness: thickness = operation.Thickness; break;
                case PathOperationType.SetColor: color = operation.Color; break;
                case PathOperationType.MoveTo: throw new UnreachableException("MoveTo is unexpected because it is associated with closing the current subpath (which marks the slicing boundary).");

                case PathOperationType.LineTo: {
                    Vector2 lineDestination = operation.Line.Destination;
                    if (Vector2.DistanceSquared(penPosition, lineDestination) <= lineDistanceThreshold) goto finish;
                    
                    var remainOperations = operations[(i + 1)..];
                    
                    var calcWinding = CalculateWindingDirection(penPosition, lineDestination, remainOperations, config);
                    if (calcWinding != WindingDirection.Unknown && calcWinding != windingDirection) {
                        windingDirection = calcWinding;
                    }
                    
                    var direction = Vector2.Normalize(lineDestination - penPosition);
                    var normal = new Vector2(-direction.Y, direction.X);
                    
                    var vcount = builder.GetLargestWrittenVertexCount();
                    GenerateJointVerticesPair(builder, remainOperations, lineDestination, direction, normal, new(thickness, color), windingDirection, config, writer);
                    WriteSegmentQuadIndices(builder, indexFormat, (uint)vcount, windingDirection);

                    endCapInformation = new CapGenerateInfo(lineDestination, direction, normal, new(thickness, color));
                    previousPointAttribute = new(thickness, color);
                    
                    finish:
                    penPosition = lineDestination;
                    break;
                }

                case PathOperationType.QuadraticBezier: {
                    (Vector2 control, Vector2 destination) = operation.QuadraticBezier;
                    var remainOperations = operations[(i + 1)..];

                    var calcWinding = CalculateWindingDirection(penPosition, destination, remainOperations, config);
                    if (calcWinding != WindingDirection.Unknown && calcWinding != windingDirection) {
                        windingDirection = calcWinding;
                    }
                
                    PlotQuadraticCurveBody(builder, penPosition, control, destination, BezierCurveResolution, previousPointAttribute, new(thickness, color), windingDirection, writer, indexFormat);
                    
                    var direction = Vector2.Normalize(QuadraticBezier.GetVelocity(penPosition, control, destination, 1));
                    var normal = new Vector2(-direction.Y, direction.X);
                    
                    var vcount = builder.GetLargestWrittenVertexCount();
                    GenerateJointVerticesPair(builder, remainOperations, destination, direction, normal, new(thickness, color), windingDirection, config, writer);
                    WriteSegmentQuadIndices(builder, indexFormat, (uint)vcount, windingDirection);
                    
                    endCapInformation = new CapGenerateInfo(destination, direction, new(-direction.Y, direction.X), new(thickness, color));
                    previousPointAttribute = new(thickness, color);
                    penPosition = destination;
                    break;
                }
                
                case PathOperationType.CubicBezier: {
                    (Vector2 startControl, Vector2 endControl, Vector2 destination) = operation.CubicBezier;
                    var remainOperations = operations[(i + 1)..];

                    var calcWinding = CalculateWindingDirection(penPosition, destination, remainOperations, config);
                    if (calcWinding != WindingDirection.Unknown && calcWinding != windingDirection) {
                        windingDirection = calcWinding;
                    }
                
                    PlotCubicCurveBody(builder, penPosition, startControl, endControl, destination, BezierCurveResolution, previousPointAttribute, new(thickness, color), windingDirection, writer, indexFormat);
                    
                    var direction = Vector2.Normalize(CubicBezier.GetVelocity(penPosition, startControl, endControl, destination, 1));
                    var normal = new Vector2(-direction.Y, direction.X);
                    
                    var vcount = builder.GetLargestWrittenVertexCount();
                    GenerateJointVerticesPair(builder, remainOperations, destination, direction, normal, new(thickness, color), windingDirection, config, writer);
                    WriteSegmentQuadIndices(builder, indexFormat, (uint)vcount, windingDirection);
                    
                    endCapInformation = new CapGenerateInfo(destination, direction, new(-direction.Y, direction.X), new(thickness, color));
                    previousPointAttribute = new(thickness, color);
                    penPosition = destination;
                    break;
                }
            }
        }
        
        // Generate end cap.
        if (endCapInformation.TryGet(out var information)) {
            GenerateEndCap(builder, information, capType, RoundCapResolution, writer, indexFormat);
        }

        static WindingDirection CalculateWindingDirection(Vector2 previousPosition, Vector2 position, ReadOnlySpan<PathOperation> nextOperations, in PathBuildingConfiguration config) {
            float lineDistanceThreshold = config.LineDistanceThreshold;
            
            foreach (ref readonly var operation in nextOperations) {
                switch (operation.Type) {
                    case PathOperationType.SetColor or PathOperationType.SetThickness: continue;
                    
                    case PathOperationType.LineTo: {
                        var nextPosition = operation.Line.Destination;

                        if (Vector2.DistanceSquared(position, nextPosition) <= lineDistanceThreshold) continue;

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

        static bool CalculateIntersectionRays(ReadOnlySpan<PathOperation> operations, Vector2 position, Vector2 direction, Vector2 normal, float thickness, WindingDirection windingDirection, in PathBuildingConfiguration config, out Ray2D ray1, out Ray2D ray2) {
            float lineDistanceThreshold = config.LineDistanceThreshold;
            
            float nextThickness = thickness;
            
            foreach (ref readonly var operation in operations) {
                switch (operation.Type) {
                    case PathOperationType.SetThickness: nextThickness = operation.Thickness; break;
                    case PathOperationType.SetColor: continue;
                    
                    case PathOperationType.LineTo: {
                        var destination = operation.Line.Destination;

                        if (Vector2.DistanceSquared(position, destination) <= lineDistanceThreshold) continue;

                        var direction2 = Vector2.Normalize(destination - position);
                        var normal2 = new Vector2(-direction2.Y, direction2.X);

                        if (windingDirection == WindingDirection.Clockwise) {
                            ray1 = Ray2D.CreateWithoutNormalize(position + normal * thickness / 2, direction);
                            ray2 = Ray2D.CreateWithoutNormalize(position + normal2 * nextThickness / 2, -direction2);
                        } else {
                            ray1 = Ray2D.CreateWithoutNormalize(position - normal * thickness / 2, direction);
                            ray2 = Ray2D.CreateWithoutNormalize(position - normal2 * nextThickness / 2, -direction2);
                        }
                        return true;
                    }

                    case PathOperationType.QuadraticBezier: {
                        (Vector2 control, Vector2 destination) = operation.QuadraticBezier;

                        var direction2 = Vector2.Normalize(QuadraticBezier.GetVelocity(position, control, destination, 0));
                        var normal2 = new Vector2(-direction2.Y, direction2.X);

                        if (windingDirection == WindingDirection.Clockwise) {
                            ray1 = Ray2D.CreateWithoutNormalize(position + normal * thickness / 2, direction);
                            ray2 = Ray2D.CreateWithoutNormalize(position + normal2 * nextThickness / 2, -direction2);
                        } else {
                            ray1 = Ray2D.CreateWithoutNormalize(position - normal * thickness / 2, direction);
                            ray2 = Ray2D.CreateWithoutNormalize(position - normal2 * nextThickness / 2, -direction2);
                        }
                        return true;
                    }
                        
                    case PathOperationType.CubicBezier: {
                        (Vector2 startControl, Vector2 endControl, Vector2 destination) = operation.CubicBezier;

                        var direction2 = Vector2.Normalize(CubicBezier.GetVelocity(position, startControl, endControl, destination, 0));
                        var normal2 = new Vector2(-direction2.Y, direction2.X);
                        
                        if (windingDirection == WindingDirection.Clockwise) {
                            ray1 = Ray2D.CreateWithoutNormalize(position + normal * thickness / 2, direction);
                            ray2 = Ray2D.CreateWithoutNormalize(position + normal2 * nextThickness / 2, -direction2);
                        } else {
                            ray1 = Ray2D.CreateWithoutNormalize(position - normal * thickness / 2, direction);
                            ray2 = Ray2D.CreateWithoutNormalize(position - normal2 * nextThickness / 2, -direction2);
                        }
                        return true;
                    }
                }
            }

            ray1 = ray2 = default;
            return false;
        }
        
        static void GenerateJointVerticesPair(MeshBuilder builder, ReadOnlySpan<PathOperation> operations, Vector2 position, Vector2 direction, Vector2 normal, PointAttribute attribute, WindingDirection windingDirection, in PathBuildingConfiguration config, VertexWriter<Vertex> writer) {
            (float thickness, Color32 color) = attribute;

            bool success = CalculateIntersectionRays(operations, position, direction, normal, thickness, windingDirection, config, out var ray1, out var ray2);
            if (success) {
                var intersect = Intersection.Test(ray1, ray2);
                
                if (intersect.HasValue) {
                    if (windingDirection == WindingDirection.Clockwise) {
                        writer(builder, new(intersect.Value, color));
                        writer(builder, new(position - (intersect.Value - position), color));
                    } else {
                        writer(builder, new(position - (intersect.Value - position), color));
                        writer(builder, new(intersect.Value, color));
                    }
                } else {
                    writer(builder, new(position + normal * thickness / 2, color));
                    writer(builder, new(position - normal * thickness / 2, color));
                }
            } else {
                writer(builder, new(position + normal * thickness / 2, color));
                writer(builder, new(position - normal * thickness / 2, color));
            }
        }
    }
}
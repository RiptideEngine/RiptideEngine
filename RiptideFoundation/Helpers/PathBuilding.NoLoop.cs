namespace RiptideFoundation.Helpers;

// TODO: Round, Bevel joints.
// TODO: Bezier Curve resolution.

partial class PathBuilding {
    private static void BuildSubpathNoLoop(MeshBuilder builder, ReadOnlySpan<PathOperation> operations, ref Vector2 penPosition, ref float thickness, ref Color32 color, VertexWriter<Vertex> writer, IndexFormat indexFormat) {
        const float lineDistanceThreshold = 0.001f;
        const int BezierCurveResolution = 16;

        const int RoundCapResolution = 8;
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
                    
                    var direction = Vector2.Normalize(lineDestination - penPosition);

                    var calcWinding = CalculateWindingDirection(penPosition, lineDestination, remainOperations);
                    if (calcWinding != WindingDirection.Unknown && calcWinding != windingDirection) {
                        windingDirection = calcWinding;
                    }
                    
                    var normal = new Vector2(-direction.Y, direction.X);
                    
                    GenerateHeadCap(builder, new(penPosition, direction, normal, previousPointAttribute), capType, RoundCapResolution, windingDirection, writer, indexFormat);
                    
                    var vcount = builder.GetLargestWrittenVertexCount();
                    GenerateJointVerticesPair(builder, remainOperations, lineDestination, direction, normal, new(thickness, color), windingDirection, writer);

                    if (windingDirection == WindingDirection.Clockwise) {
                        ConnectQuadFromIndices(builder, indexFormat, (uint)(vcount + 1), (uint)(vcount - 1), (uint)(vcount - 2), (uint)(vcount - 2), (uint)vcount, (uint)(vcount + 1));
                    } else {
                        ConnectQuadFromIndices(builder, indexFormat, (uint)(vcount - 2), (uint)vcount, (uint)(vcount + 1), (uint)(vcount + 1), (uint)(vcount - 1), (uint)(vcount - 2));
                    }

                    endCapInformation = new CapGenerateInfo(lineDestination, direction, normal, new(thickness, color));
                    previousPointAttribute = new(thickness, color);

                    penPosition = lineDestination;
                    goto breakLoop;
                }

                // case PathOperationType.QuadraticBezier: {
                //     (Vector2 control, Vector2 destination) = operation.QuadraticBezier;
                //
                //     var direction = Vector2.Normalize(QuadraticBezier.GetVelocity(penPosition, control, destination, 0));
                //     var normal = new Vector2(-direction.Y, direction.X);
                //     
                //     GenerateHeadCap(builder, new(penPosition, direction, normal, previousPointAttribute), capType, RoundCapResolution, writer, indexFormat);
                //
                //     PlotQuadraticCurveBody(builder, penPosition, control, destination, BezierCurveResolution, previousPointAttribute, new(thickness, color), writer, indexFormat);
                //     
                //     var vcount = builder.GetLargestWrittenVertexCount();
                //     GenerateJointVerticesPair(builder, operations, penPosition, destination, direction, normal, color, thickness, writer);
                //     WriteQuadIndices(builder, indexFormat, (uint)vcount);
                //     
                //     direction = Vector2.Normalize(QuadraticBezier.GetVelocity(penPosition, control, destination, 1));
                //     endCapInformation = new CapGenerateInfo(destination, direction, new(-direction.Y, direction.X), new(thickness, color));
                //     previousPointAttribute = new(thickness, color);
                //     previousPosition = penPosition;
                //     penPosition = destination;
                //     goto breakLoop;
                // }
                //
                // case PathOperationType.CubicBezier: {
                //     (Vector2 startControl, Vector2 endControl, Vector2 destination) = operation.CubicBezier;
                //
                //     var direction = Vector2.Normalize(CubicBezier.GetVelocity(penPosition, startControl, endControl, destination, 0));
                //     var normal = new Vector2(-direction.Y, direction.X);
                //     
                //     GenerateHeadCap(builder, new(penPosition, direction, normal, previousPointAttribute), capType, RoundCapResolution, writer, indexFormat);
                //
                //     PlotCubicCurveBody(builder, penPosition, startControl, endControl, destination, BezierCurveResolution, previousPointAttribute, new(thickness, color), writer, indexFormat);
                //
                //     var vcount = builder.GetLargestWrittenVertexCount();
                //     GenerateJointVerticesPair(builder, operations, penPosition, destination, direction, normal, color, thickness, writer);
                //     WriteQuadIndices(builder, indexFormat, (uint)vcount);
                //     
                //     direction = Vector2.Normalize(CubicBezier.GetVelocity(penPosition, startControl, endControl, destination, 1));
                //     endCapInformation = new CapGenerateInfo(destination, direction, new(-direction.Y, direction.X), new(thickness, color));
                //     previousPointAttribute = new(thickness, color);
                //     previousPosition = penPosition;
                //     penPosition = destination;
                //     goto breakLoop;
                // }
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
                    
                    var calcWinding = CalculateWindingDirection(penPosition, lineDestination, remainOperations);
                    if (calcWinding != WindingDirection.Unknown && calcWinding != windingDirection) {
                        windingDirection = calcWinding;
                    }
                    
                    var direction = Vector2.Normalize(lineDestination - penPosition);
                    var normal = new Vector2(-direction.Y, direction.X);
                    
                    var vcount = builder.GetLargestWrittenVertexCount();
                    GenerateJointVerticesPair(builder, remainOperations, lineDestination, direction, normal, new(thickness, color), windingDirection, writer);
                    //WriteQuadIndices(builder, indexFormat, (uint)vcount);

                    if (windingDirection == WindingDirection.Clockwise) {
                        ConnectQuadFromIndices(builder, indexFormat, (uint)(vcount + 1), (uint)(vcount - 1), (uint)(vcount - 2), (uint)(vcount - 2), (uint)vcount, (uint)(vcount + 1));
                    } else {
                        ConnectQuadFromIndices(builder, indexFormat, (uint)(vcount - 2), (uint)vcount, (uint)(vcount + 1), (uint)(vcount + 1), (uint)(vcount - 1), (uint)(vcount - 2));
                    }
                    
                    endCapInformation = new CapGenerateInfo(lineDestination, direction, normal, new(thickness, color));
                    previousPointAttribute = new(thickness, color);
                    
                    finish:
                    penPosition = lineDestination;
                    break;
                }

                // case PathOperationType.QuadraticBezier: {
                //     (Vector2 control, Vector2 destination) = operation.QuadraticBezier;
                //
                //     PlotQuadraticCurveBody(builder, penPosition, control, destination, BezierCurveResolution, previousPointAttribute, new(thickness, color), writer, indexFormat);
                //
                //     Vector2 direction = Vector2.Normalize(QuadraticBezier.GetVelocity(penPosition, control, destination, 1));
                //     Vector2 normal = new(-direction.Y, direction.X);
                //     
                //     var vcount = builder.GetLargestWrittenVertexCount();
                //     GenerateJointVerticesPair(builder, operations, previousPosition, destination, direction, normal, color, thickness, writer);
                //     WriteQuadIndices(builder, indexFormat, (uint)vcount);
                //     
                //     previousPointAttribute = new(thickness, color);
                //     endCapInformation = new CapGenerateInfo(destination, direction, normal, new(thickness, color));
                //     penPosition = destination;
                //     break;
                // }
                //
                // case PathOperationType.CubicBezier: {
                //     (Vector2 startControl, Vector2 endControl, Vector2 destination) = operation.CubicBezier;
                //
                //     PlotCubicCurveBody(builder, penPosition, startControl, endControl, destination, BezierCurveResolution, previousPointAttribute, new(thickness, color), writer, indexFormat);
                //
                //     Vector2 direction = Vector2.Normalize(CubicBezier.GetVelocity(penPosition, startControl, endControl, destination, 1));
                //     Vector2 normal = new(-direction.Y, direction.X);
                //
                //     var vcount = builder.GetLargestWrittenVertexCount();
                //     GenerateJointVerticesPair(builder, operations, previousPosition, destination, direction, normal, color, thickness, writer);
                //     WriteQuadIndices(builder, indexFormat, (uint)vcount);
                //
                //     direction = Vector2.Normalize(CubicBezier.GetVelocity(penPosition, startControl, endControl, destination, 1));
                //     // endCapInformation = new CapGenerateInfo(destination, direction, new(-direction.Y, direction.X), new(thickness, color));
                //     previousPointAttribute = new(thickness, color);
                //     penPosition = destination;
                //     break;
                // }
            }
        }
        
        // Generate end cap.
        if (endCapInformation.TryGet(out var information)) {
            GenerateEndCap(builder, information, capType, RoundCapResolution, writer, indexFormat);
        }

        static WindingDirection CalculateWindingDirection(Vector2 previousPosition, Vector2 position, ReadOnlySpan<PathOperation> nextOperations) {
            foreach (ref readonly var operation in nextOperations) {
                switch (operation.Type) {
                    case PathOperationType.SetColor or PathOperationType.SetThickness: continue;
                    case PathOperationType.LineTo:
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
            }

            return WindingDirection.Unknown;
        }
        
        static Optional<Ray2D> GetSecondIntersectionRay(ReadOnlySpan<PathOperation> operations, Vector2 previousPosition, Vector2 position, float thickness, float lineDistanceThreshold) {
            foreach (ref readonly var operation in operations) {
                switch (operation.Type) {
                    case PathOperationType.SetThickness: thickness = operation.Thickness; break;
                    case PathOperationType.LineTo: {
                        var destination = operation.Line.Destination;

                        if (Vector2.DistanceSquared(position, destination) <= lineDistanceThreshold) continue;

                        var direction = Vector2.Normalize(destination - position);
                        var normal = new Vector2(-direction.Y, direction.X);

                        var d1 = position - previousPosition;
                        var d2 = destination - position;

                        var winding = d1.X * d2.Y - d2.X * d1.Y;
                        
                        return Optional<Ray2D>.From(Ray2D.CreateWithoutNormalize(position + (winding > 0 ? -normal : normal) * thickness / 2, -direction));
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
        
        static void GenerateJointVerticesPair(MeshBuilder builder, ReadOnlySpan<PathOperation> operations, Vector2 position, Vector2 direction, Vector2 normal, PointAttribute attribute, WindingDirection windingDirection, VertexWriter<Vertex> writer) {
            Ray2D ray1 = default;
            Optional<Ray2D> ray2Opt = Optional<Ray2D>.Null;

            (float thickness, Color32 color) = attribute;
            float nextThickness = thickness;
            
            foreach (ref readonly var operation in operations) {
                switch (operation.Type) {
                    case PathOperationType.SetThickness: nextThickness = operation.Thickness; break;
                    case PathOperationType.LineTo: {
                        var destination = operation.Line.Destination;

                        if (Vector2.DistanceSquared(position, destination) <= lineDistanceThreshold) continue;

                        var direction2 = Vector2.Normalize(destination - position);
                        var normal2 = new Vector2(-direction2.Y, direction2.X);

                        if (windingDirection == WindingDirection.Clockwise) {
                            ray1 = Ray2D.CreateWithoutNormalize(position + normal * thickness / 2, direction);
                            ray2Opt = Ray2D.CreateWithoutNormalize(position + normal2 * nextThickness / 2, -direction2);
                        } else {
                            ray1 = Ray2D.CreateWithoutNormalize(position - normal * thickness / 2, direction);
                            ray2Opt = Ray2D.CreateWithoutNormalize(position - normal2 * nextThickness / 2, -direction2);
                        }
                        goto breakLoop;
                    }
                    
                    // case PathOperationType.QuadraticBezier: {
                    //     var direction = Vector2.Normalize(QuadraticBezier.GetVelocity(position, operation.QuadraticBezier.Control, operation.QuadraticBezier.Destination, 0));
                    //     var normal = new Vector2(-direction.Y, direction.X);
                    //
                    //     return Optional<Ray2D>.From(Ray2D.CreateWithoutNormalize(position + normal * thickness / 2, -direction));
                    // }
                    //
                    // case PathOperationType.CubicBezier: {
                    //     var direction = Vector2.Normalize(CubicBezier.GetVelocity(position, operation.CubicBezier.StartControl, operation.CubicBezier.EndControl, operation.CubicBezier.Destination, 0));
                    //     var normal = new Vector2(-direction.Y, direction.X);
                    //
                    //     return Optional<Ray2D>.From(Ray2D.CreateWithoutNormalize(position + normal * thickness / 2, -direction));
                    // }
                }

                continue;
                
                breakLoop: break;
            }
            
            if (ray2Opt.TryGet(out var ray2)) {
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
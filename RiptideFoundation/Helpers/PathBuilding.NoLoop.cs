namespace RiptideFoundation.Helpers;

// TODO: Round, Bevel joints.
// TODO: Bezier Curve resolution.

partial class PathBuilding {
    private static void BuildSubpathNoLoop(MeshBuilder builder, ReadOnlySpan<PathOperation> operations, ref Vector2 penPosition, ref float thickness, ref Color32 color, VertexWriter<Vertex> writer, IndexFormat indexFormat) {
        const float lineDistanceThreshold = 0.001f;
        const int BezierCurveResolution = 16;
        
        PointAttribute previousPointAttribute = new(thickness, color);

        const int RoundCapResolution = 8;
        PathCapType capType = operations[^1] is { Type: PathOperationType.Close } close ? close.Close.CapType : PathCapType.Butt;

        var endCapInformation = Optional<CapGenerateInfo>.Null;

        for (int i = 0; i < operations.Length; i++) {
            ref readonly var operation = ref operations[i];

            switch (operation.Type) {
                case PathOperationType.SetThickness: thickness = operation.Thickness; break;
                case PathOperationType.SetColor: color = operation.Color; break;
                case PathOperationType.MoveTo: throw new UnreachableException("MoveTo is unexpected because it is associated with closing the current subpath (which marks the slicing boundary).");

                case PathOperationType.LineTo: {
                    Vector2 lineDestination = operation.Line.Destination;
                    
                    var direction = Vector2.Normalize(lineDestination - penPosition);
                    var normal = new Vector2(-direction.Y, direction.X);

                    GenerateHeadCap(builder, new(penPosition, direction, normal, previousPointAttribute), capType, RoundCapResolution, writer, indexFormat);
                    
                    var vcount = builder.GetLargestWrittenVertexCount();
                    GenerateJointVerticesPair(builder, operations[(i + 1)..], lineDestination, direction, normal, color, thickness, writer);
                    
                    WriteQuadIndices(builder, indexFormat, (uint)vcount);

                    endCapInformation = new CapGenerateInfo(lineDestination, direction, normal, new(thickness, color));
                    previousPointAttribute = new(thickness, color);
                    
                    penPosition = lineDestination;
                    goto breakLoop;
                }

                case PathOperationType.QuadraticBezier: {
                    (Vector2 control, Vector2 destination) = operation.QuadraticBezier;

                    var direction = Vector2.Normalize(QuadraticBezier.GetVelocity(penPosition, control, destination, 0));
                    var normal = new Vector2(-direction.Y, direction.X);
                    
                    GenerateHeadCap(builder, new(penPosition, direction, normal, previousPointAttribute), capType, RoundCapResolution, writer, indexFormat);

                    PlotQuadraticCurveBody(builder, penPosition, control, destination, BezierCurveResolution, previousPointAttribute, new(thickness, color), writer, indexFormat);
                    GenerateAndConnectJointVerticesPairToPreviousVerticesPair(builder, operations[(i + 1)..], destination, Vector2.Normalize(QuadraticBezier.GetVelocity(penPosition, control, destination, 1)), color, thickness, writer, indexFormat);

                    direction = Vector2.Normalize(QuadraticBezier.GetVelocity(penPosition, control, destination, 1));
                    endCapInformation = new CapGenerateInfo(destination, direction, new(-direction.Y, direction.X), new(thickness, color));
                    previousPointAttribute = new(thickness, color);
                    penPosition = destination;
                    
                    goto breakLoop;
                }

                case PathOperationType.CubicBezier: {
                    (Vector2 startControl, Vector2 endControl, Vector2 destination) = operation.CubicBezier;

                    var direction = Vector2.Normalize(CubicBezier.GetVelocity(penPosition, startControl, endControl, destination, 0));
                    var normal = new Vector2(-direction.Y, direction.X);
                    
                    GenerateHeadCap(builder, new(penPosition, direction, normal, previousPointAttribute), capType, RoundCapResolution, writer, indexFormat);

                    PlotCubicCurveBody(builder, penPosition, startControl, endControl, destination, BezierCurveResolution, previousPointAttribute, new(thickness, color), writer, indexFormat);
                    GenerateAndConnectJointVerticesPairToPreviousVerticesPair(builder, operations[(i + 1)..], destination, Vector2.Normalize(CubicBezier.GetVelocity(penPosition, startControl, endControl, destination, 1)), color, thickness, writer, indexFormat);

                    direction = Vector2.Normalize(CubicBezier.GetVelocity(penPosition, startControl, endControl, destination, 1));
                    endCapInformation = new CapGenerateInfo(destination, direction, new(-direction.Y, direction.X), new(thickness, color));
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
                    
                    var direction = Vector2.Normalize(lineDestination - penPosition);
                    var normal = new Vector2(-direction.Y, direction.X);

                    var vcount = builder.GetLargestWrittenVertexCount();
                    GenerateJointVerticesPair(builder, operations[(i + 1)..], lineDestination, direction, normal, color, thickness, writer);
                    
                    WriteQuadIndices(builder, indexFormat, (uint)vcount);

                    endCapInformation = new CapGenerateInfo(lineDestination, direction, normal, new(thickness, color));
                    previousPointAttribute = new(thickness, color);
                    
                    finish:
                    penPosition = lineDestination;
                    break;
                }

                case PathOperationType.QuadraticBezier: {
                    (Vector2 control, Vector2 destination) = operation.QuadraticBezier;

                    PlotQuadraticCurveBody(builder, penPosition, control, destination, BezierCurveResolution, previousPointAttribute, new(thickness, color), writer, indexFormat);
                    GenerateAndConnectJointVerticesPairToPreviousVerticesPair(builder, operations[(i + 1)..], destination, Vector2.Normalize(QuadraticBezier.GetVelocity(penPosition, control, destination, 1)), color, thickness, writer, indexFormat);
                    
                    previousPointAttribute = new(thickness, color);

                    var direction = Vector2.Normalize(QuadraticBezier.GetVelocity(penPosition, control, destination, 1));
                    endCapInformation = new CapGenerateInfo(destination, direction, new(-direction.Y, direction.X), new(thickness, color));
                    penPosition = destination;
                    break;
                }

                case PathOperationType.CubicBezier: {
                    (Vector2 startControl, Vector2 endControl, Vector2 destination) = operation.CubicBezier;

                    PlotCubicCurveBody(builder, penPosition, startControl, endControl, destination, BezierCurveResolution, previousPointAttribute, new(thickness, color), writer, indexFormat);
                    GenerateAndConnectJointVerticesPairToPreviousVerticesPair(builder, operations[(i + 1)..], destination, Vector2.Normalize(CubicBezier.GetVelocity(penPosition, startControl, endControl, destination, 1)), color, thickness, writer, indexFormat);

                    var direction = Vector2.Normalize(CubicBezier.GetVelocity(penPosition, startControl, endControl, destination, 1));
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
        
        static Optional<Ray2D> GetSecondIntersectionRay(ReadOnlySpan<PathOperation> operations, Vector2 position, float thickness, float lineDistanceThreshold) {
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

        static void GenerateAndConnectJointVerticesPairToPreviousVerticesPair(MeshBuilder builder, ReadOnlySpan<PathOperation> operations, Vector2 position, Vector2 direction, Color32 color, float thickness, VertexWriter<Vertex> writer, IndexFormat indexFormat) {
            var normal = new Vector2(-direction.Y, direction.X);

            var vcount = builder.GetLargestWrittenVertexCount();
            GenerateJointVerticesPair(builder, operations, position, direction, normal, color, thickness, writer);
            WriteQuadIndices(builder, indexFormat, (uint)vcount);
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
    }

    
}
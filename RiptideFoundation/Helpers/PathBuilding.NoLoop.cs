namespace RiptideFoundation.Helpers;

// TODO: Round, Bevel joints.
// TODO: Bezier Curve resolution.

partial class PathBuilding {
    private static void BuildSubpathNoLoop(MeshBuilder builder, ReadOnlySpan<PathOperation> operations, ref Vector2 penPosition, ref float thickness, ref Color32 color, VertexWriter<Vertex> writer, IndexFormat indexFormat) {
        const float lineDistanceThreshold = 0.001f;
        
        var firstPointAttribute = Optional<PointAttribute>.From(new(thickness, color));
        var previousPointAttribute = firstPointAttribute;

        const int RoundCapResolution = 8;
        PathCapType capType = operations[^1] is { Type: PathOperationType.Close } close ? close.Close.CapType : PathCapType.Butt;

        var endCapInformation = Optional<(PointAttribute Attribute, Vector2 Direction)>.Null;
        
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

                    if (firstPointAttribute.TryGet(out var first)) {
                        GenerateHeadCap(builder, penPosition, normal, first, capType, RoundCapResolution, writer, indexFormat);

                        firstPointAttribute = Optional<PointAttribute>.Null;
                    }
                    
                    var vcount = builder.GetLargestWrittenVertexCount();
                    GenerateJointVerticesPair(builder, operations[(i + 1)..], lineDestination, direction, normal, color, thickness, writer);
                    
                    WriteQuadIndices(builder, indexFormat, (uint)vcount);
                    
                    endCapInformation = (new(thickness, color), direction);
                    previousPointAttribute = Optional<PointAttribute>.From(new(thickness, color));
                    
                    finish:
                    penPosition = lineDestination;
                    break;
                }

                case PathOperationType.QuadraticBezier: {
                    const int resolution = 16;

                    (Vector2 control, Vector2 destination) = operation.QuadraticBezier;

                    PointAttribute previous;
                    
                    if (firstPointAttribute.TryGet(out var first)) {
                        var direction = Vector2.Normalize(QuadraticBezier.GetVelocity(penPosition, control, destination, 0));
                        var normal = new Vector2(-direction.Y, direction.X);
                        
                        GenerateHeadCap(builder, penPosition, normal, first, capType, RoundCapResolution, writer, indexFormat);

                        previous = first;

                        firstPointAttribute = Optional<PointAttribute>.Null;
                    } else {
                        Debug.Assert(previousPointAttribute.HasValue, "previousPointAttribute.HasValue");

                        previous = previousPointAttribute.GetUnchecked();
                    }

                    PlotQuadraticCurveBody(builder, penPosition, control, destination, resolution, previous, new(thickness, color), writer, indexFormat);
                    GenerateAndConnectJointVerticesPairToPreviousVerticesPair(builder, operations[(i + 1)..], destination, Vector2.Normalize(QuadraticBezier.GetVelocity(penPosition, control, destination, 1)), color, thickness, writer, indexFormat);
                    
                    previousPointAttribute = Optional<PointAttribute>.From(new(thickness, color));
                    endCapInformation = (new(thickness, color), Vector2.Normalize(QuadraticBezier.GetVelocity(penPosition, control, destination, 1)));
                    penPosition = destination;
                    break;
                }

                case PathOperationType.CubicBezier: {
                    const int resolution = 16;

                    (Vector2 startControl, Vector2 endControl, Vector2 destination) = operation.CubicBezier;

                    PointAttribute previous;
                    
                    if (firstPointAttribute.TryGet(out var first)) {
                        var direction = Vector2.Normalize(CubicBezier.GetVelocity(penPosition, startControl, endControl, destination, 0));
                        var normal = new Vector2(-direction.Y, direction.X);
                        
                        GenerateHeadCap(builder, penPosition, normal, first, capType, RoundCapResolution, writer, indexFormat);

                        previous = first;

                        firstPointAttribute = Optional<PointAttribute>.Null;
                    } else {
                        Debug.Assert(previousPointAttribute.HasValue, "previousPointAttribute.HasValue");

                        previous = previousPointAttribute.GetUnchecked();
                    }

                    PlotCubicCurveBody(builder, penPosition, startControl, endControl, destination, resolution, previous, new(thickness, color), writer, indexFormat);
                    GenerateAndConnectJointVerticesPairToPreviousVerticesPair(builder, operations[(i + 1)..], destination, Vector2.Normalize(CubicBezier.GetVelocity(penPosition, startControl, endControl, destination, 1)), color, thickness, writer, indexFormat);
                    
                    previousPointAttribute = Optional<PointAttribute>.From(new(thickness, color));
                    endCapInformation = (new(thickness, color), Vector2.Normalize(CubicBezier.GetVelocity(penPosition, startControl, endControl, destination, 1)));
                    penPosition = destination;
                    break;
                }
            }
        }
        
        // Generate end cap.
        if (capType != PathCapType.Butt && endCapInformation.TryGet(out var information)) {
            GenerateHeadCap(builder, penPosition, new(information.Direction.Y, -information.Direction.X), information.Attribute, capType, RoundCapResolution, writer, indexFormat);
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

    private static void GenerateHeadCap(MeshBuilder builder, Vector2 position, Vector2 normal, PointAttribute attribute, PathCapType type, int roundResolution, VertexWriter<Vertex> writer, IndexFormat indexFormat) {
        switch (type) {
            case PathCapType.Butt: GenerateButtCap(builder, position, normal, attribute, writer); break;
            case PathCapType.Round:
                if (roundResolution == 0) {
                    GenerateButtCap(builder, position, normal, attribute, writer);
                } else {
                    GenerateRoundCap(builder, position, normal, attribute, roundResolution, writer, indexFormat);
                }
                break;
            case PathCapType.Square: throw new NotImplementedException("Square cap is being implemented.");
        }
        
        static void GenerateButtCap(MeshBuilder builder, Vector2 position, Vector2 normal, PointAttribute attribute, VertexWriter<Vertex> writer) {
            normal *= attribute.Thickness / 2;
            
            writer(builder, new(position + normal, attribute.Color));
            writer(builder, new(position - normal, attribute.Color));
        }

        static void GenerateRoundCap(MeshBuilder builder, Vector2 position, Vector2 normal, PointAttribute attribute, int resolution, VertexWriter<Vertex> writer, IndexFormat indexFormat) {
            normal *= attribute.Thickness / 2;
            int vcount = builder.GetLargestWrittenVertexCount();

            if (resolution == 1) {
                writer(builder, new(position + new Vector2(-normal.Y, normal.X), attribute.Color));
                writer(builder, new(position + normal, attribute.Color));
                writer(builder, new(position - normal, attribute.Color));

                if (indexFormat == IndexFormat.UInt16) {
                    builder.WriteIndices(stackalloc ushort[] {
                        (ushort)(vcount + 1),
                        (ushort)(vcount + 0),
                        (ushort)(vcount + 2),
                    });
                }
            } else {
                writer(builder, new(position, attribute.Color));
                float step = float.Pi / (resolution + 1);
                
                for (int i = 1; i <= resolution; i++) {
                    var direction = Vector2.TransformNormal(normal, Matrix3x2.CreateRotation(step * i));
                    
                    writer(builder, new(position + direction, attribute.Color));
                }

                int vcount2 = builder.GetLargestWrittenVertexCount();
                
                writer(builder, new(position + normal, attribute.Color));
                writer(builder, new(position - normal, attribute.Color));
                
                // Triangulation.
                if (indexFormat == IndexFormat.UInt16) {
                    Span<ushort> indices = stackalloc ushort[3];
                    indices[0] = (ushort)vcount;
                    
                    for (int i = 1; i < resolution; i++) {
                        indices[1] = (ushort)(vcount + i);
                        indices[2] = (ushort)(vcount + i + 1);
                        
                        builder.WriteIndices(indices);
                    }

                    builder.WriteIndices(stackalloc ushort[] {
                        (ushort)vcount, (ushort)vcount2, (ushort)(vcount + 1),
                        (ushort)(vcount + resolution), (ushort)(vcount2 + 1), (ushort)vcount,
                    });
                } else {
                    Span<uint> indices = stackalloc uint[3];
                    
                    indices[0] = (uint)vcount;
                    
                    for (int i = 1; i < resolution; i++) {
                        indices[1] = (uint)(vcount + i);
                        indices[2] = (uint)(vcount + i + 1);
                        
                        builder.WriteIndices(indices);
                    }

                    builder.WriteIndices(stackalloc uint[] {
                        (uint)vcount, (uint)vcount2, (uint)(vcount + 1),
                        (uint)(vcount + resolution), (uint)(vcount2 + 1), (uint)vcount,
                    });
                }
            }
        }
    }
}
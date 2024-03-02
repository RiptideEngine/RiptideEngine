namespace RiptideFoundation.Helpers;

// TODO: Customizable Bezier Curve resolution.

partial class PathBuilding {
    private static void BuildSubpathNoLoop(MeshBuilder builder, ReadOnlySpan<PathOperation> operations, ref Vector2 penPosition, ref float thickness, ref Color32 color, in PathBuildingConfiguration config, VertexWriter<Vertex> writer, IndexFormat indexFormat) {
        float lineDistanceThreshold = config.LineDistanceThreshold;
        int bezierResolution = config.BezierCurveResolution;
        int roundCapResolution = config.RoundCapResolution;
        
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
                    
                    GenerateHeadCap(builder, new(penPosition, direction, normal, previousPointAttribute), capType, roundCapResolution, windingDirection, writer, indexFormat);
                    
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

                    GenerateHeadCap(builder, new(penPosition, direction, normal, previousPointAttribute), capType, roundCapResolution, windingDirection, writer, indexFormat);
                    PlotQuadraticCurveBody(builder, penPosition, control, destination, bezierResolution, previousPointAttribute, new(thickness, color), windingDirection, writer, indexFormat);
                    
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
                    
                    GenerateHeadCap(builder, new(penPosition, direction, normal, previousPointAttribute), capType, roundCapResolution, windingDirection, writer, indexFormat);
                    PlotCubicCurveBody(builder, penPosition, startControl, endControl, destination, bezierResolution, previousPointAttribute, new(thickness, color), windingDirection, writer, indexFormat);
                
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
                
                    PlotQuadraticCurveBody(builder, penPosition, control, destination, bezierResolution, previousPointAttribute, new(thickness, color), windingDirection, writer, indexFormat);
                    
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
                
                    PlotCubicCurveBody(builder, penPosition, startControl, endControl, destination, bezierResolution, previousPointAttribute, new(thickness, color), windingDirection, writer, indexFormat);
                    
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
            GenerateEndCap(builder, information, capType, roundCapResolution, writer, indexFormat);
        }
    }
}
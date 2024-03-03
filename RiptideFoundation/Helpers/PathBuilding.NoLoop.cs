namespace RiptideFoundation.Helpers;

// TODO: Customizable Bezier Curve resolution.

partial class PathBuilding {
    private static void BuildSubpathNoLoop(MeshBuilder builder, ReadOnlySpan<PathOperation> operations, ref Vector2 penPosition, ref float thickness, ref Color32 color, in PathBuildingConfiguration config, VertexWriter<Vertex> writer, IndexFormat indexFormat) {
        if (!TryGetFirstAndLastPlottingOperations(operations, out var firstDrawIndex, out var lastDrawIndex)) {
            ExecuteRemainConfiguratingOperations(operations, ref thickness, ref color);
            return;
        }

        int bezierCurveResolution = config.BezierCurveResolution;
        int roundCapResolution = config.RoundCapResolution;
        
        PathCapType capType = operations[^1] is { Type: PathOperationType.Close } close ? close.Close.CapType : PathCapType.Butt;

        if (firstDrawIndex == lastDrawIndex) {
            var firstAttribute = new PointAttribute(thickness, color);
            const WindingDirection windingDirection = WindingDirection.Clockwise;

            foreach (ref readonly var operation in operations) {
                switch (operation.Type) {
                    case PathOperationType.SetThickness: thickness = operation.Thickness; break;
                    case PathOperationType.SetColor: color = operation.Color; break;

                    case PathOperationType.LineTo: {
                        var destination = operation.Line.Destination;

                        var direction = Vector2.Normalize(destination - penPosition);
                        var normal = new Vector2(-direction.Y, direction.X);

                        GenerateCap(builder, new(penPosition, -direction, normal, firstAttribute), capType, roundCapResolution, windingDirection, writer, indexFormat);

                        var vcount1 = (uint)builder.GetLargestWrittenVertexCount();
                        
                        GenerateCap(builder, new(destination, direction, normal, new(thickness, color)), capType, roundCapResolution, windingDirection, writer, indexFormat);

                        var vcount2 = (uint)builder.GetLargestWrittenVertexCount();

                        ConnectTrianglePairFromIndices(builder, indexFormat, vcount1 - 2, vcount2 - 2, vcount2 - 1, vcount2 - 1, vcount1 - 1, vcount1 - 2);
                        penPosition = destination;
                        break;
                    }

                    case PathOperationType.QuadraticBezier: {
                        (var control, var destination) = operation.QuadraticBezier;

                        var direction = Vector2.Normalize(QuadraticBezier.GetVelocity(penPosition, control, destination, 0));
                        var normal = new Vector2(-direction.Y, direction.X);
                        
                        GenerateCap(builder, new(penPosition, -direction, normal, firstAttribute), capType, roundCapResolution, windingDirection, writer, indexFormat);
                        PlotQuadraticCurveBody(builder, penPosition, control, destination, bezierCurveResolution, firstAttribute, new(thickness, color), windingDirection, writer, indexFormat);

                        var vcount1 = (uint)builder.GetLargestWrittenVertexCount();
                        
                        direction = Vector2.Normalize(QuadraticBezier.GetVelocity(penPosition, control, destination, 1));
                        normal = new(-direction.Y, direction.X);
                        GenerateCap(builder, new(destination, direction, normal, new(thickness, color)), capType, roundCapResolution, windingDirection, writer, indexFormat);
                        
                        var vcount2 = (uint)builder.GetLargestWrittenVertexCount();
                        
                        ConnectTrianglePairFromIndices(builder, indexFormat, vcount1 - 2, vcount2 - 2, vcount2 - 1, vcount2 - 1, vcount1 - 1, vcount1 - 2);
                        penPosition = destination;
                        break;
                    }

                    case PathOperationType.CubicBezier: {
                        (var startControl, var endControl, var destination) = operation.CubicBezier;

                        var direction = Vector2.Normalize(CubicBezier.GetVelocity(penPosition, startControl, endControl, destination, 0));
                        var normal = new Vector2(-direction.Y, direction.X);
                        
                        GenerateCap(builder, new(penPosition, -direction, normal, firstAttribute), capType, roundCapResolution, windingDirection, writer, indexFormat);
                        PlotCubicCurveBody(builder, penPosition, startControl, endControl, destination, bezierCurveResolution, firstAttribute, new(thickness, color), windingDirection, writer, indexFormat);

                        var vcount1 = (uint)builder.GetLargestWrittenVertexCount();
                        
                        direction = Vector2.Normalize(CubicBezier.GetVelocity(penPosition, startControl, endControl, destination, 1));
                        normal = new(-direction.Y, direction.X);
                        GenerateCap(builder, new(destination, direction, normal, new(thickness, color)), capType, roundCapResolution, windingDirection, writer, indexFormat);

                        var vcount2 = (uint)builder.GetLargestWrittenVertexCount();
                        
                        ConnectTrianglePairFromIndices(builder, indexFormat, vcount1 - 2, vcount2 - 2, vcount2 - 1, vcount2 - 1, vcount1 - 1, vcount1 - 2);
                        penPosition = destination;
                        break;
                    }
                }
            }
        } else {
            var previousAttribute = new PointAttribute(thickness, color);
            WindingDirection windingDirection = WindingDirection.Clockwise;

            for (int i = 0; i <= firstDrawIndex; i++) {
                ref readonly var operation = ref operations[i];
                
                switch (operation.Type) {
                    case PathOperationType.SetThickness: thickness = operation.Thickness; break;
                    case PathOperationType.SetColor: color = operation.Color; break;

                    case PathOperationType.LineTo: {
                        Vector2 lineDestination = operation.Line.Destination;
                        var remainOperations = operations[(i + 1)..];

                        var calcWinding = CalculateWindingDirection(penPosition, lineDestination, remainOperations, config);
                        if (calcWinding != WindingDirection.Unknown && calcWinding != windingDirection) {
                            windingDirection = calcWinding;
                        }

                        var direction = Vector2.Normalize(lineDestination - penPosition);
                        var normal = new Vector2(-direction.Y, direction.X);

                        GenerateCap(builder, new(penPosition, -direction, normal, previousAttribute), capType, roundCapResolution, windingDirection, writer, indexFormat);

                        var vcount = builder.GetLargestWrittenVertexCount();
                        GenerateJointVerticesPair(builder, remainOperations, lineDestination, direction, normal, new(thickness, color), windingDirection, config, writer);
                        WriteSegmentQuadIndices(builder, indexFormat, (uint)vcount, windingDirection);

                        previousAttribute = new(thickness, color);
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
                    
                        var direction = -Vector2.Normalize(QuadraticBezier.GetVelocity(penPosition, control, destination, 0));
                        var normal = new Vector2(-direction.Y, direction.X);
                    
                        GenerateCap(builder, new(penPosition, direction, normal, previousAttribute), capType, roundCapResolution, windingDirection, writer, indexFormat);
                        PlotQuadraticCurveBody(builder, penPosition, control, destination, bezierCurveResolution, previousAttribute, new(thickness, color), windingDirection, writer, indexFormat);
                        
                        direction = Vector2.Normalize(QuadraticBezier.GetVelocity(penPosition, control, destination, 1));
                        normal = new(-direction.Y, direction.X);
                        
                        var vcount = builder.GetLargestWrittenVertexCount();
                        GenerateJointVerticesPair(builder, remainOperations, destination, direction, normal, new(thickness, color), windingDirection, config, writer);
                        WriteSegmentQuadIndices(builder, indexFormat, (uint)vcount, windingDirection);
                        
                        previousAttribute = new(thickness, color);
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
                    
                        var direction = Vector2.Normalize(CubicBezier.GetVelocity(penPosition, startControl, endControl, destination, 0));
                        
                        GenerateCap(builder, new(penPosition, -direction, new(-direction.Y, direction.X), previousAttribute), capType, roundCapResolution, windingDirection, writer, indexFormat);
                        PlotCubicCurveBody(builder, penPosition, startControl, endControl, destination, bezierCurveResolution, previousAttribute, new(thickness, color), windingDirection, writer, indexFormat);
                    
                        direction = Vector2.Normalize(CubicBezier.GetVelocity(penPosition, startControl, endControl, destination, 1));
                        
                        var vcount = builder.GetLargestWrittenVertexCount();
                        GenerateJointVerticesPair(builder, remainOperations, destination, direction, new(-direction.Y, direction.X), new(thickness, color), windingDirection, config, writer);
                        WriteSegmentQuadIndices(builder, indexFormat, (uint)vcount, windingDirection);
                        
                        previousAttribute = new(thickness, color);
                        penPosition = destination;
                        break;
                    }
                }
            }

            for (int i = firstDrawIndex + 1; i < lastDrawIndex; i++) {
                ref readonly var operation = ref operations[i];

                switch (operation.Type) {
                    case PathOperationType.SetThickness: thickness = operation.Thickness; break;
                    case PathOperationType.SetColor: color = operation.Color; break;
                    
                    case PathOperationType.LineTo: {
                        Vector2 lineDestination = operation.Line.Destination;
                        
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
                    
                        previousAttribute = new(thickness, color);
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
                    
                        PlotQuadraticCurveBody(builder, penPosition, control, destination, bezierCurveResolution, previousAttribute, new(thickness, color), windingDirection, writer, indexFormat);
                        
                        var direction = Vector2.Normalize(QuadraticBezier.GetVelocity(penPosition, control, destination, 1));
                        var normal = new Vector2(-direction.Y, direction.X);
                        
                        var vcount = builder.GetLargestWrittenVertexCount();
                        GenerateJointVerticesPair(builder, remainOperations, destination, direction, normal, new(thickness, color), windingDirection, config, writer);
                        WriteSegmentQuadIndices(builder, indexFormat, (uint)vcount, windingDirection);
                        
                        previousAttribute = new(thickness, color);
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
                    
                        PlotCubicCurveBody(builder, penPosition, startControl, endControl, destination, bezierCurveResolution, previousAttribute, new(thickness, color), windingDirection, writer, indexFormat);
                        
                        var direction = Vector2.Normalize(CubicBezier.GetVelocity(penPosition, startControl, endControl, destination, 1));
                        var normal = new Vector2(-direction.Y, direction.X);
                        
                        var vcount = builder.GetLargestWrittenVertexCount();
                        GenerateJointVerticesPair(builder, remainOperations, destination, direction, normal, new(thickness, color), windingDirection, config, writer);
                        WriteSegmentQuadIndices(builder, indexFormat, (uint)vcount, windingDirection);
                        
                        previousAttribute = new(thickness, color);
                        penPosition = destination;
                        break;
                    }
                }
            }
            
            // Plot the last.
            {
                ref readonly var operation = ref operations[lastDrawIndex];

                switch (operation.Type) {
                    case PathOperationType.LineTo: {
                        var destination = operation.Line.Destination;

                        var direction = Vector2.Normalize(destination - penPosition);
                        var normal = new Vector2(-direction.Y, direction.X);

                        var vcount1 = (uint)builder.GetLargestWrittenVertexCount();

                        GenerateCap(builder, new(destination, direction, normal, new(thickness, color)), capType, roundCapResolution, windingDirection, writer, indexFormat);

                        var vcount2 = (uint)builder.GetLargestWrittenVertexCount();

                        ConnectTrianglePairFromIndices(builder, indexFormat, vcount1 - 2, vcount2 - 2, vcount2 - 1, vcount2 - 1, vcount1 - 1, vcount1 - 2);
                        penPosition = destination;
                        break;
                    }

                    case PathOperationType.QuadraticBezier: {
                        (var control, var destination) = operation.QuadraticBezier;
                        
                        PlotQuadraticCurveBody(builder, penPosition, control, destination, bezierCurveResolution, previousAttribute, new(thickness, color), windingDirection, writer, indexFormat);
                        var vcount1 = (uint)builder.GetLargestWrittenVertexCount();
                        
                        var direction = Vector2.Normalize(QuadraticBezier.GetVelocity(penPosition, control, destination, 1));
                        var normal = new Vector2(-direction.Y, direction.X);
                        
                        GenerateCap(builder, new(destination, direction, normal, new(thickness, color)), capType, roundCapResolution, windingDirection, writer, indexFormat);
                        var vcount2 = (uint)builder.GetLargestWrittenVertexCount();

                        ConnectTrianglePairFromIndices(builder, indexFormat, vcount1 - 2, vcount2 - 2, vcount2 - 1, vcount2 - 1, vcount1 - 1, vcount1 - 2);
                        penPosition = destination;
                        break;
                    }

                    case PathOperationType.CubicBezier: {
                        (var startControl, var endControl, var destination) = operation.CubicBezier;
                        
                        PlotCubicCurveBody(builder, penPosition, startControl, endControl, destination, bezierCurveResolution, previousAttribute, new(thickness, color), windingDirection, writer, indexFormat);
                        var vcount1 = (uint)builder.GetLargestWrittenVertexCount();
                        
                        var direction = Vector2.Normalize(CubicBezier.GetVelocity(penPosition, startControl, endControl, destination, 1));
                        var normal = new Vector2(-direction.Y, direction.X);
                        
                        GenerateCap(builder, new(destination, direction, normal, new(thickness, color)), capType, roundCapResolution, windingDirection, writer, indexFormat);
                        var vcount2 = (uint)builder.GetLargestWrittenVertexCount();

                        ConnectTrianglePairFromIndices(builder, indexFormat, vcount1 - 2, vcount2 - 2, vcount2 - 1, vcount2 - 1, vcount1 - 1, vcount1 - 2);
                        penPosition = destination;
                        break;
                    }
                }
            }
            
            ExecuteRemainConfiguratingOperations(operations[(lastDrawIndex + 1)..], ref thickness, ref color);
        }
    }
}
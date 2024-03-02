namespace RiptideFoundation.Helpers;

partial class PathBuilding {
    private const float BezierCurveCollinearThreshold = 0.001f;
    
    private static void BuildSubpathLoop(MeshBuilder builder, ReadOnlySpan<PathOperation> operations, ref Vector2 penPosition, ref float thickness, ref Color32 color, in PathBuildingConfiguration config, VertexWriter<Vertex> writer, IndexFormat indexFormat) {
        Optional<PointAttribute> firstPointAttribute = new PointAttribute(thickness, color);
        
        // Alright how do we handle this thing...
        int firstPlotIndex, lastPlotIndex;

        {
            Optional<int> firstPlottingOp = Optional<int>.Null;
            for (int i = 0; i < operations.Length; i++) {
                ref readonly var operation = ref operations[i];
                
                switch (operation.Type) {
                    case PathOperationType.SetColor: color = operation.Color; break;
                    case PathOperationType.SetThickness: thickness = operation.Thickness; break;
                    case PathOperationType.Close: return;
                    case PathOperationType.MoveTo: throw new UnreachableException("MoveTo is unexpected.");
                    case PathOperationType.LineTo or PathOperationType.QuadraticBezier or PathOperationType.CubicBezier:
                        firstPlottingOp = i;
                        goto breakLoop;
                }

                continue;
                
                breakLoop:
                break;
            }

            if (!firstPlottingOp.TryGet(out firstPlotIndex)) return;

            Optional<int> lastPlottingOp = Optional<int>.Null;

            for (int i = 0; i < operations.Length; i++) {
                ref readonly var operation = ref operations[i];

                switch (operation.Type) {
                    case PathOperationType.SetColor or PathOperationType.SetThickness: break;
                    case PathOperationType.Close: break;
                    case PathOperationType.MoveTo: throw new UnreachableException("MoveTo is unexpected.");
                    case PathOperationType.LineTo or PathOperationType.QuadraticBezier or PathOperationType.CubicBezier:
                        lastPlottingOp = i;
                        goto breakLoop;
                }

                continue;
                
                breakLoop:
                break;
            }

            bool get = lastPlottingOp.TryGet(out lastPlotIndex);
            Debug.Assert(get, "get");
        }

        if (firstPlotIndex == lastPlotIndex) {
            bool get = firstPointAttribute.TryGet(out var first);
            Debug.Assert(get);
            
            HandleSubpathLoopSingularDrawOperation(builder, operations, ref penPosition, ref thickness, ref color, firstPlotIndex, first, config, writer, indexFormat);
        } else {
            throw new NotImplementedException("Fully-fledged looping is still being implemented.");
        }
    }

    private static void HandleSubpathLoopSingularDrawOperation(MeshBuilder builder, ReadOnlySpan<PathOperation> operations, ref Vector2 penPosition, ref float thickness, ref Color32 color, int index, PointAttribute first, in PathBuildingConfiguration config, VertexWriter<Vertex> writer, IndexFormat indexFormat) {
        float lineDistanceThreshold = config.LineDistanceThreshold;
        int bezierCurveThreshold = config.BezierCurveResolution;
        
        const WindingDirection windingDirection = WindingDirection.CounterClockwise;
        
        ref readonly var operation = ref operations[index];
            
        switch (operation.Type) {
            case PathOperationType.LineTo: {
                Vector2 destination = operation.Line.Destination;
                if (Vector2.DistanceSquared(destination, penPosition) <= lineDistanceThreshold) return;
        
                var direction = Vector2.Normalize(destination - penPosition);
                var normal = new Vector2(-direction.Y, direction.X) / 2;
        
                writer(builder, new(penPosition + normal * first.Thickness, first.Color));
                writer(builder, new(penPosition - normal * first.Thickness, first.Color));
                
                var vcount = builder.GetLargestWrittenVertexCount();
                
                writer(builder, new(destination + normal * thickness, color));
                writer(builder, new(destination - normal * thickness, color));

                WriteSegmentQuadIndices(builder, indexFormat, (uint)vcount, windingDirection);
        
                penPosition = destination;
                break;
            }
            case PathOperationType.QuadraticBezier: {
                // I'm 99% sure that Intersection loop can't be used for Quadratic Bezier Curve.
                
                (Vector2 control, Vector2 end) = operation.QuadraticBezier;
            
                if (Vector2.DistanceSquared(penPosition, end) <= 0.001f) {
                    // Handle the degenerate case where start approximately close to end, which form a line extends from start halfway to control point.
                    var direction = Vector2.Normalize(control - penPosition);
                    var centerEnd = (penPosition + control) / 2;
                    var normal = new Vector2(-direction.Y, direction.X) / 2;
                    
                    var middleThickness = float.Lerp(first.Thickness, thickness, 0.5f);
                    var middleColor = (Color32)Color.Lerp(first.Color, color, 0.5f);
                    
                    if (first.Color.A == 255 && color.A == 255) {
                        // If start (almost) equals to the end, it form a line from that point to the point lie between it and control.
                        if (first.Thickness <= thickness) {
                            // Last half overlapped the first, only generate mesh for last half to reduce overdrawn.
                            writer(builder, new(end + normal * thickness, color));
                            writer(builder, new(end - normal * thickness, color));
            
                            var vcount = builder.GetLargestWrittenVertexCount();
            
                            writer(builder, new(centerEnd + normal * middleThickness, middleColor));
                            writer(builder, new(centerEnd - normal * middleThickness, middleColor));
            
                            WriteSegmentQuadIndices(builder, indexFormat, (uint)vcount, windingDirection);
                        } else {
                            writer(builder, new(penPosition + normal * first.Thickness, first.Color));
                            writer(builder, new(penPosition - normal * first.Thickness, first.Color));
            
                            var vcount = builder.GetLargestWrittenVertexCount();
            
                            writer(builder, new(centerEnd + normal * middleThickness, middleColor));
                            writer(builder, new(centerEnd - normal * middleThickness, middleColor));
            
                            WriteSegmentQuadIndices(builder, indexFormat, (uint)vcount, windingDirection);
            
                            vcount = builder.GetLargestWrittenVertexCount();
            
                            writer(builder, new(end + normal * thickness, color));
                            writer(builder, new(end - normal * thickness, color));
            
                            WriteSegmentQuadIndices(builder, indexFormat, (uint)vcount, windingDirection);
                        }
                    } else {
                        var vcount = builder.GetLargestWrittenVertexCount();
                        
                        writer(builder, new(penPosition + normal * first.Thickness, first.Color));
                        writer(builder, new(penPosition - normal * first.Thickness, first.Color));
                        writer(builder, new(centerEnd + normal * middleThickness, middleColor));
                        writer(builder, new(centerEnd - normal * middleThickness, middleColor));
                        writer(builder, new(end + normal * thickness, color));
                        writer(builder, new(end - normal * thickness, color));
            
                        if (indexFormat == IndexFormat.UInt16) {
                            builder.WriteIndices(stackalloc ushort[] {
                                (ushort)vcount, (ushort)(vcount + 2), (ushort)(vcount + 3),
                                (ushort)(vcount + 3), (ushort)(vcount + 1), (ushort)vcount,
                                (ushort)(vcount + 2), (ushort)(vcount + 4), (ushort)(vcount + 5),
                                (ushort)(vcount + 5), (ushort)(vcount + 3), (ushort)(vcount + 2),
                            });
                        } else {
                            builder.WriteIndices(stackalloc uint[] {
                                (uint)vcount, (uint)(vcount + 2), (uint)(vcount + 3),
                                (uint)(vcount + 3), (uint)(vcount + 1), (uint)vcount,
                                (uint)(vcount + 2), (uint)(vcount + 4), (uint)(vcount + 5),
                                (uint)(vcount + 5), (uint)(vcount + 3), (uint)(vcount + 2),
                            });
                        }
                    }
                } else if (MathUtils.IsCollinear(penPosition, control, end, BezierCurveCollinearThreshold)) {
                    // Handle the degenerate case where control approximately close to start or end point, which make intersection math fail.
                    
                    // If the control point lie between start and end, we can draw a straight line from start to end as usual (still need to
                    // be calculated as curvature to preserve color correctness) without loop, or else, the line gonna extrude outside a little.
            
                    // TODO: Simplify the drawing operation into straight line if the thickness and color doesn't change.
                    // if (first.Color == color && first.Thickness == thickness) { }
                    
                    var direction = Vector2.Normalize(QuadraticBezier.GetVelocity(penPosition, control, end, 0));
                    var normal = new Vector2(-direction.Y, direction.X) / 2;
            
                    writer(builder, new(penPosition + normal * first.Thickness, first.Color));
                    writer(builder, new(penPosition - normal * first.Thickness, first.Color));
                    
                    PlotQuadraticCurveBody(builder, penPosition, control, end, bezierCurveThreshold, first, new(thickness, color), windingDirection, writer, indexFormat);
            
                    uint vcount = (uint)builder.GetLargestWrittenVertexCount();
                    
                    direction = Vector2.Normalize(QuadraticBezier.GetVelocity(penPosition, control, end, 1));
                    normal = new Vector2(-direction.Y, direction.X) / 2;
            
                    writer(builder, new(end + normal * thickness, color));
                    writer(builder, new(end - normal * thickness, color));
                    
                    WriteSegmentQuadIndices(builder, indexFormat, vcount, windingDirection);
                } else {
                    var startEndCenter = Vector2.Lerp(penPosition, end, 0.5f);
                    var startEndDirection = Vector2.Normalize(end - penPosition);
                    var startEndNormal = new Vector2(-startEndDirection.Y, startEndDirection.X);
            
                    var headDirection = Vector2.Normalize(QuadraticBezier.GetVelocity(penPosition, control, end, 0));
                    var tailDirection = Vector2.Normalize(QuadraticBezier.GetVelocity(penPosition, control, end, 1));
            
                    var headNormal = new Vector2(-headDirection.Y, headDirection.X);
                    var tailNormal = new Vector2(-tailDirection.Y, tailDirection.X);
                    
                    int headIndex, tailIndex;
                    
                    // Just generate the head.
                    {
                        var ray1Normal = startEndNormal * float.Lerp(first.Thickness, thickness, 0.5f) / 2f;
            
                        var ray1 = Ray2D.CreateWithoutNormalize(startEndCenter + ray1Normal, -startEndDirection);
                        var ray2 = Ray2D.CreateWithoutNormalize(penPosition - headNormal * first.Thickness / 2, headDirection);
            
                        var intersect = Intersection.Test(ray1, ray2);
                        Debug.Assert(intersect.HasValue, "intersect.HasValue");
            
                        ray1 = Ray2D.CreateWithoutNormalize(startEndCenter - ray1Normal, -startEndDirection);
                        ray2 = Ray2D.CreateWithoutNormalize(penPosition + headNormal * first.Thickness / 2, -headDirection);
                        
                        var intersect2 = Intersection.Test(ray1, ray2);
                        Debug.Assert(intersect2.HasValue, "intersect2.HasValue");
            
                        headIndex = builder.GetLargestWrittenVertexCount();
                        
                        writer(builder, new(intersect2.Value, first.Color));
                        writer(builder, new(intersect.Value, first.Color));
                    }
                    
                    PlotQuadraticCurveBody(builder, penPosition, control, end, bezierCurveThreshold, first, new(thickness, color), windingDirection, writer, indexFormat);
            
                    // Generate the tail.
                    {
                        var ray1Normal = startEndNormal * float.Lerp(first.Thickness, thickness, 0.5f) / 2;
                        
                        var ray1 = Ray2D.CreateWithoutNormalize(startEndCenter + ray1Normal, startEndDirection);
                        var ray2 = Ray2D.CreateWithoutNormalize(end - tailNormal * thickness / 2, -tailDirection);
            
                        var intersect = Intersection.Test(ray1, ray2);
                        Debug.Assert(intersect.HasValue, "intersect.HasValue");
            
                        ray1 = Ray2D.CreateWithoutNormalize(startEndCenter - ray1Normal, startEndDirection);
                        ray2 = Ray2D.CreateWithoutNormalize(end + tailNormal * thickness / 2, -tailDirection);
            
                        var intersect2 = Intersection.Test(ray1, ray2);
                        Debug.Assert(intersect2.HasValue, "intersect2.HasValue");
            
                        int vcount = builder.GetLargestWrittenVertexCount();
                        tailIndex = vcount;
                        
                        writer(builder, new(intersect2.Value, color));
                        writer(builder, new(intersect.Value, color));
                        
                        WriteSegmentQuadIndices(builder, indexFormat, (uint)vcount, windingDirection);
                    }
            
                    if (indexFormat == IndexFormat.UInt16) {
                        builder.WriteIndices(stackalloc ushort[] {
                            (ushort)headIndex, (ushort)tailIndex, (ushort)(tailIndex + 1),
                            (ushort)(tailIndex + 1), (ushort)(headIndex + 1), (ushort)headIndex,
                        });
                    } else {
                        builder.WriteIndices(stackalloc uint[] {
                            (uint)headIndex, (uint)tailIndex, (uint)(tailIndex + 1),
                            (uint)(tailIndex + 1), (uint)(headIndex + 1), (uint)headIndex,
                        });
                    }
                }
                
                penPosition = end;
                break;
            }
            
            case PathOperationType.CubicBezier: {
                var loopType = operations[^1].Close.Loop;
                
                (Vector2 startControl, Vector2 endControl, Vector2 end) = operation.CubicBezier;
                
                if (MathUtils.IsCollinear(penPosition, startControl, endControl, end, BezierCurveCollinearThreshold)) {
                    // If all points collinear, draw the curve normally without loop.
                    var direction = Vector2.Normalize(CubicBezier.GetVelocity(penPosition, startControl, endControl, end, 0));
                    var normal = new Vector2(-direction.Y, direction.X) / 2;
                
                    writer(builder, new(penPosition + normal * first.Thickness, first.Color));
                    writer(builder, new(penPosition - normal * first.Thickness, first.Color));
                    
                    PlotCubicCurveBody(builder, penPosition, startControl, endControl, end, bezierCurveThreshold, first, new(thickness, color), windingDirection, writer, indexFormat);
                
                    uint vcount = (uint)builder.GetLargestWrittenVertexCount();
                    
                    direction = Vector2.Normalize(CubicBezier.GetVelocity(penPosition, startControl, endControl, end, 1));
                    normal = new Vector2(-direction.Y, direction.X) / 2;
                
                    writer(builder, new(end + normal * thickness, color));
                    writer(builder, new(end - normal * thickness, color));
                    
                    WriteSegmentQuadIndices(builder, indexFormat, vcount, windingDirection);
                } else {
                    var headDirection = Vector2.Normalize(CubicBezier.GetVelocity(penPosition, startControl, endControl, end, 0));
                    var tailDirection = Vector2.Normalize(CubicBezier.GetVelocity(penPosition, startControl, endControl, end, 1));
                    
                    var headNormal = new Vector2(-headDirection.Y, headDirection.X);
                    var tailNormal = new Vector2(-tailDirection.Y, tailDirection.X);

                    switch (loopType) {
                        case PathLooping.Intersection: {
                            var ray1 = Ray2D.CreateWithoutNormalize(penPosition, -headDirection);
                            var ray2 = Ray2D.CreateWithoutNormalize(end, tailDirection);

                            if (Intersection.Test(ray1, ray2) is { } intersect) {
                                ray1 = Ray2D.CreateWithoutNormalize(penPosition + headNormal * thickness / 2, -headDirection);
                                ray2 = Ray2D.CreateWithoutNormalize(penPosition - headNormal * thickness / 2, -headDirection);
                                var ray3 = Ray2D.CreateWithoutNormalize(end + tailNormal * thickness / 2, tailDirection);
                                var ray4 = Ray2D.CreateWithoutNormalize(end - tailNormal * thickness / 2, tailDirection);
                                
                                var intersect1 = Intersection.Test(ray1, ray3);
                                var intersect2 = Intersection.Test(ray2, ray4);
                                
                                if (intersect1.HasValue && intersect2.HasValue) {
                                    uint headvcount = (uint)builder.GetLargestWrittenVertexCount();
                                    
                                    writer(builder, new(penPosition + headNormal * first.Thickness / 2, first.Color));
                                    writer(builder, new(penPosition - headNormal * first.Thickness / 2, first.Color));
                                    
                                    PlotCubicCurveBody(builder, penPosition, startControl, endControl, end, bezierCurveThreshold, first, new(thickness, color), windingDirection, writer, indexFormat);
                                
                                    uint tailvcount = (uint)builder.GetLargestWrittenVertexCount();
                                    
                                    writer(builder, new(end + tailNormal * thickness / 2, color));
                                    writer(builder, new(end - tailNormal * thickness / 2, color));
                                    
                                    WriteSegmentQuadIndices(builder, indexFormat, tailvcount, windingDirection);
                                
                                    uint vcount = (uint)builder.GetLargestWrittenVertexCount();
                                    
                                    // Interpolate depend on distance.
                                    var distance1 = Vector2.Distance(penPosition, intersect);
                                    var distance2 = Vector2.Distance(intersect, end);
                                
                                    float weight = distance1 / (distance1 + distance2);

                                    Color32 interpolatedColor = (Color32)Color.Lerp(first.Color, color, weight);
                                    
                                    writer(builder, new(intersect1.Value, interpolatedColor));
                                    writer(builder, new(intersect2.Value, interpolatedColor));
                                
                                    ConnectQuadFromIndices(builder, indexFormat, headvcount, vcount, vcount + 1, vcount + 1, headvcount + 1, headvcount);
                                    ConnectQuadFromIndices(builder, indexFormat, vcount, tailvcount, tailvcount + 1, tailvcount + 1, vcount + 1, vcount);
                                } else {
                                    
                                }
                            } else {
                                goto case PathLooping.Connect;
                            }
                            
                            // var ray1 = Ray2D.CreateWithoutNormalize(penPosition + headNormal * thickness / 2, -headDirection);
                            // var ray2 = Ray2D.CreateWithoutNormalize(penPosition - headNormal * thickness / 2, -headDirection);
                            // var ray3 = Ray2D.CreateWithoutNormalize(end + tailNormal * thickness / 2, tailDirection);
                            // var ray4 = Ray2D.CreateWithoutNormalize(end - tailNormal * thickness / 2, tailDirection);
                            //
                            // var intersect1 = Intersection.Test(ray1, ray3);
                            // var intersect2 = Intersection.Test(ray2, ray4);
                            //
                            // if (intersect1.HasValue && intersect2.HasValue) {
                            //     uint headvcount = (uint)builder.GetLargestWrittenVertexCount();
                            //     
                            //     writer(builder, new(penPosition + headNormal * first.Thickness / 2, first.Color));
                            //     writer(builder, new(penPosition - headNormal * first.Thickness / 2, first.Color));
                            //     
                            //     PlotCubicCurveBody(builder, penPosition, startControl, endControl, end, bezierCurveThreshold, first, new(thickness, color), windingDirection, writer, indexFormat);
                            //
                            //     uint tailvcount = (uint)builder.GetLargestWrittenVertexCount();
                            //     
                            //     writer(builder, new(end + tailNormal * thickness / 2, first.Color));
                            //     writer(builder, new(end - tailNormal * thickness / 2, first.Color));
                            //     
                            //     WriteSegmentQuadIndices(builder, indexFormat, tailvcount, windingDirection);
                            //
                            //     uint vcount = (uint)builder.GetLargestWrittenVertexCount();
                            //     
                            //     // Interpolate depend on distance.
                            //     var distance1 = Vector2.Distance(penPosition + headNormal * first.Thickness / 2, intersect1.Value);
                            //     var distance2 = Vector2.Distance(intersect1.Value, end + tailNormal * thickness / 2);
                            //
                            //     float weight = distance1 / (distance1 + distance2);
                            //     
                            //     writer(builder, new(intersect1.Value, first.Color));
                            //     writer(builder, new(intersect2.Value, first.Color));
                            //
                            //     ConnectQuadFromIndices(builder, indexFormat, headvcount, vcount, vcount + 1, vcount + 1, headvcount + 1, headvcount);
                            //     ConnectQuadFromIndices(builder, indexFormat, vcount, tailvcount, tailvcount + 1, tailvcount + 1, vcount + 1, vcount);
                            // } else {
                            //     // uint headvcount = (uint)builder.GetLargestWrittenVertexCount();
                            //     //
                            //     // writer(builder, new(penPosition + headNormal * first.Thickness / 2, first.Color));
                            //     // writer(builder, new(penPosition - headNormal * first.Thickness / 2, first.Color));
                            //     //
                            //     // PlotCubicCurveBody(builder, penPosition, startControl, endControl, end, bezierCurveThreshold, first, new(thickness, color), windingDirection, writer, indexFormat);
                            //     //
                            //     // uint tailvcount = (uint)builder.GetLargestWrittenVertexCount();
                            //     //
                            //     // writer(builder, new(end + tailNormal * thickness / 2, first.Color));
                            //     // writer(builder, new(end - tailNormal * thickness / 2, first.Color));
                            //     //
                            //     // WriteSegmentQuadIndices(builder, indexFormat, tailvcount, windingDirection);
                            //
                            //     // uint vcount = (uint)builder.GetLargestWrittenVertexCount();
                            //     //
                            //     // writer(builder, new(intersect1.Value, first.Color));
                            //     // writer(builder, new(intersect2.Value, first.Color));
                            // }
                            break;
                        }

                        case PathLooping.Connect: {

                            break;
                        }
                    }
                    
                //     
                //     if (Vector2.DistanceSquared(penPosition, end) <= 0.001f) {
                //         // Handle closed shape in which doing intersection test would cause head and tail go NaN due to invalid start end direction.
                //         
                //         
                //     } else {
                //         var startEndCenter = Vector2.Lerp(penPosition, end, 0.5f);
                //         var startEndDirection = Vector2.Normalize(end - penPosition);
                //         var startEndNormal = new Vector2(-startEndDirection.Y, startEndDirection.X);
                //         
                //         int headIndex, tailIndex;
                //         
                //         // Just generate the head.
                //         {
                //             var ray1Normal = startEndNormal * float.Lerp(first.Thickness, thickness, 0.5f) / 2f;
                //
                //             var ray1 = Ray2D.CreateWithoutNormalize(startEndCenter + ray1Normal, -startEndDirection);
                //             var ray2 = Ray2D.CreateWithoutNormalize(penPosition - headNormal * first.Thickness / 2, headDirection);
                //
                //             var intersect1 = Intersection.Test(ray1, ray2);
                //             Debug.Assert(intersect1.HasValue, "intersect.HasValue");
                //
                //             ray1 = Ray2D.CreateWithoutNormalize(startEndCenter - ray1Normal, -startEndDirection);
                //             ray2 = Ray2D.CreateWithoutNormalize(penPosition + headNormal * first.Thickness / 2, -headDirection);
                //             
                //             var intersect2 = Intersection.Test(ray1, ray2);
                //             Debug.Assert(intersect2.HasValue, "intersect2.HasValue");
                //             
                //             headIndex = builder.GetLargestWrittenVertexCount();
                //             
                //             writer(builder, new(intersect2.Value, first.Color));
                //             writer(builder, new(intersect1.Value, first.Color));
                //         }
                //         
                //         PlotCubicCurveBody(builder, penPosition, startControl, endControl, end, resolution, first, new(thickness, color), writer, indexFormat);
                //
                //         // Generate the tail.
                //         {
                //             var ray1Normal = startEndNormal * float.Lerp(first.Thickness, thickness, 0.5f) / 2;
                //             
                //             var ray1 = Ray2D.CreateWithoutNormalize(startEndCenter + ray1Normal, startEndDirection);
                //             var ray2 = Ray2D.CreateWithoutNormalize(end - tailNormal * thickness / 2, -tailDirection);
                //
                //             var intersect = Intersection.Test(ray1, ray2);
                //             Debug.Assert(intersect.HasValue, "intersect.HasValue");
                //
                //             ray1 = Ray2D.CreateWithoutNormalize(startEndCenter - ray1Normal, startEndDirection);
                //             ray2 = Ray2D.CreateWithoutNormalize(end + tailNormal * thickness / 2, -tailDirection);
                //
                //             var intersect2 = Intersection.Test(ray1, ray2);
                //             Debug.Assert(intersect2.HasValue, "intersect2.HasValue");
                //
                //             int vcount = builder.GetLargestWrittenVertexCount();
                //             tailIndex = vcount;
                //             
                //             writer(builder, new(intersect2.Value, color));
                //             writer(builder, new(intersect.Value, color));
                //             
                //             WriteQuadIndices(builder, indexFormat, (uint)vcount);
                //         }
                //
                //         if (indexFormat == IndexFormat.UInt16) {
                //             builder.WriteIndices(stackalloc ushort[] {
                //                 (ushort)headIndex, (ushort)tailIndex, (ushort)(tailIndex + 1),
                //                 (ushort)(tailIndex + 1), (ushort)(headIndex + 1), (ushort)headIndex,
                //             });
                //         } else {
                //             builder.WriteIndices(stackalloc uint[] {
                //                 (uint)headIndex, (uint)tailIndex, (uint)(tailIndex + 1),
                //                 (uint)(tailIndex + 1), (uint)(headIndex + 1), (uint)headIndex,
                //             });
                //         }
                //     }
                }
                break;
            }
        
            default: throw new UnreachableException();
        }
        
        for (int i = index + 1; i < operations.Length; i++) {
            ref readonly var remainOperation = ref operations[i];
        
            switch (remainOperation.Type) {
                case PathOperationType.SetColor: color = operation.Color; break;
                case PathOperationType.SetThickness: thickness = operation.Thickness; break;
                case PathOperationType.Close: break;
                case PathOperationType.MoveTo: throw new UnreachableException("MoveTo is unexpected.");
                default: continue;
            }
        }
    }
}
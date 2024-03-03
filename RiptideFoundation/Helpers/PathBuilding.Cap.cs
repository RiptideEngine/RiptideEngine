namespace RiptideFoundation.Helpers;

partial class PathBuilding {
    private static void GenerateCap(MeshBuilder builder, CapGenerateInfo info, PathCapType type, int roundResolution, WindingDirection windingDirection, VertexWriter<Vertex> writer, IndexFormat indexFormat) {
        switch (type) {
            case PathCapType.Butt: GenerateButtCap(builder, info, windingDirection, writer); break;
            case PathCapType.Round:
                if (roundResolution == 0)
                    GenerateButtCap(builder, info, windingDirection, writer);
                else
                    GenerateRoundCap(builder, info, roundResolution, windingDirection, writer, indexFormat);
                break;
        }

        static void GenerateButtCap(MeshBuilder builder, CapGenerateInfo info, WindingDirection windingDirection, VertexWriter<Vertex> writer) {
            var normal = info.Normal * info.Attribute.Thickness / 2;

            writer(builder, new(info.Position + normal, info.Attribute.Color));
            writer(builder, new(info.Position - normal, info.Attribute.Color));
        }

        static void GenerateRoundCap(MeshBuilder builder, CapGenerateInfo info, int resolution, WindingDirection windingDirection, VertexWriter<Vertex> writer, IndexFormat indexFormat) {
            var color = info.Attribute.Color;
            var halfThickness = info.Attribute.Thickness / 2;
            
            var normal = info.Normal * halfThickness;
            var position = info.Position;

            var vcount = (uint)builder.GetLargestWrittenVertexCount();

            if (resolution == 1) {
                writer(builder, new(position + info.Direction, color));
                writer(builder, new(position + normal, color));
                writer(builder, new(position - normal, color));
                
                ConnectTriangleFromIndices(builder, indexFormat, vcount, vcount + 1, vcount + 2);
            } else {
                writer(builder, new(position, color));
                float step = float.Pi / (resolution + 1) * float.Sign(info.Normal.X * info.Direction.Y - info.Normal.Y * info.Direction.X);
                
                for (int i = 1; i <= resolution; i++) {
                    var direction = Vector2.TransformNormal(normal, Matrix3x2.CreateRotation(step * i));
                    
                    writer(builder, new(position + direction, color));
                }
                
                var vcount2 = (uint)builder.GetLargestWrittenVertexCount();
                
                writer(builder, new(position + normal, color));
                writer(builder, new(position - normal, color));
                
                // Triangulating.
                Span<ushort> indices = stackalloc ushort[3];
                indices[0] = (ushort)vcount;
                
                for (int i = 1; i < resolution; i++) {
                    indices[1] = (ushort)(vcount + i);
                    indices[2] = (ushort)(vcount + i + 1);
                    
                    ConnectTriangleFromIndices(builder, indexFormat, vcount, vcount + (uint)i, vcount + (uint)i + 1);
                }
                
                ConnectTrianglePairFromIndices(builder, indexFormat, vcount2, vcount + 1, vcount, vcount, vcount + (uint)resolution, vcount2 + 1);
            }
        }
    }
    
    // private static void GenerateHeadCap(MeshBuilder builder, CapGenerateInfo info, PathCapType type, int roundResolution, WindingDirection windingDirection, VertexWriter<Vertex> writer, IndexFormat indexFormat) {
    //     switch (type) {
    //         case PathCapType.Butt: GenerateButtCap(builder, info, windingDirection, writer); break;
    //         case PathCapType.Round:
    //             if (roundResolution == 0) {
    //                 GenerateButtCap(builder, info,windingDirection,  writer);
    //             } else {
    //                 GenerateRoundCap(builder, info, roundResolution, windingDirection, writer, indexFormat);
    //             }
    //             break;
    //         case PathCapType.Square: GenerateSquareCap(builder, info, windingDirection, writer, indexFormat); break;
    //     }
    //     
    //     static void GenerateButtCap(MeshBuilder builder, CapGenerateInfo info, WindingDirection windingDirection, VertexWriter<Vertex> writer) {
    //         var normal = info.Normal * info.Attribute.Thickness / 2;
    //
    //         writer(builder, new(info.Position + normal, info.Attribute.Color));
    //         writer(builder, new(info.Position - normal, info.Attribute.Color));
    //     }
    //
    //     static void GenerateRoundCap(MeshBuilder builder, CapGenerateInfo info, int resolution, WindingDirection windingDirection, VertexWriter<Vertex> writer, IndexFormat indexFormat) {
    //         var halfThickness = info.Attribute.Thickness / 2;
    //         var normal = info.Normal * halfThickness;
    //         int vcount = builder.GetLargestWrittenVertexCount();
    //
    //         var position = info.Position;
    //         var color = info.Attribute.Color;
    //
    //         if (resolution == 1) {
    //             writer(builder, new(position - info.Direction * halfThickness, color));
    //             writer(builder, new(position + normal, color));
    //             writer(builder, new(position - normal, color));
    //
    //             if (indexFormat == IndexFormat.UInt16) {
    //                 builder.WriteIndices(stackalloc ushort[] {
    //                     (ushort)(vcount + 1),
    //                     (ushort)(vcount + 0),
    //                     (ushort)(vcount + 2),
    //                 });
    //             }
    //         } else {
    //             writer(builder, new(position, color));
    //             float step = float.Pi / (resolution + 1);
    //             
    //             for (int i = 1; i <= resolution; i++) {
    //                 var direction = Vector2.TransformNormal(normal, Matrix3x2.CreateRotation(step * i));
    //                 
    //                 writer(builder, new(position + direction, color));
    //             }
    //
    //             int vcount2 = builder.GetLargestWrittenVertexCount();
    //             
    //             writer(builder, new(position + normal, color));
    //             writer(builder, new(position - normal, color));
    //             
    //             // Triangulation.
    //             if (indexFormat == IndexFormat.UInt16) {
    //                 Span<ushort> indices = stackalloc ushort[3];
    //                 indices[0] = (ushort)vcount;
    //                 
    //                 for (int i = 1; i < resolution; i++) {
    //                     indices[1] = (ushort)(vcount + i);
    //                     indices[2] = (ushort)(vcount + i + 1);
    //                     
    //                     builder.WriteIndices(indices);
    //                 }
    //
    //                 builder.WriteIndices(stackalloc ushort[] {
    //                     (ushort)vcount, (ushort)vcount2, (ushort)(vcount + 1),
    //                     (ushort)(vcount + resolution), (ushort)(vcount2 + 1), (ushort)vcount,
    //                 });
    //             } else {
    //                 Span<uint> indices = stackalloc uint[3];
    //                 
    //                 indices[0] = (uint)vcount;
    //                 
    //                 for (int i = 1; i < resolution; i++) {
    //                     indices[1] = (uint)(vcount + i);
    //                     indices[2] = (uint)(vcount + i + 1);
    //                     
    //                     builder.WriteIndices(indices);
    //                 }
    //
    //                 builder.WriteIndices(stackalloc uint[] {
    //                     (uint)vcount, (uint)vcount2, (uint)(vcount + 1),
    //                     (uint)(vcount + resolution), (uint)(vcount2 + 1), (uint)vcount,
    //                 });
    //             }
    //         }
    //     }
    //
    //     static void GenerateSquareCap(MeshBuilder builder, CapGenerateInfo info, WindingDirection windingDirection, VertexWriter<Vertex> writer, IndexFormat format) {
    //         var halfThickness = info.Attribute.Thickness / 2;
    //         var normal = info.Normal * halfThickness;
    //
    //         var capPosition = info.Position - info.Direction * halfThickness;
    //         var color = info.Attribute.Color;
    //
    //         int vcount = builder.GetLargestWrittenVertexCount();
    //
    //         writer(builder, new(capPosition + normal, color));
    //         writer(builder, new(capPosition - normal, color));
    //         writer(builder, new(info.Position + normal, color));
    //         writer(builder, new(info.Position - normal, color));
    //
    //         if (format == IndexFormat.UInt16) {
    //             builder.WriteIndices(stackalloc ushort[] {
    //                 (ushort)vcount,
    //                 (ushort)(vcount + 2),
    //                 (ushort)(vcount + 3),
    //                 (ushort)(vcount + 3),
    //                 (ushort)(vcount + 1),
    //                 (ushort)vcount,
    //             });
    //         } else {
    //             builder.WriteIndices(stackalloc uint[] {
    //                 (uint)vcount,
    //                 (uint)(vcount + 2),
    //                 (uint)(vcount + 3),
    //                 (uint)(vcount + 3),
    //                 (uint)(vcount + 1),
    //                 (uint)vcount,
    //             });
    //         }
    //     }
    // }
    //
    // private static void GenerateEndCap(MeshBuilder builder, CapGenerateInfo info, PathCapType type, int roundResolution, VertexWriter<Vertex> writer, IndexFormat indexFormat) {
    //     switch (type) {
    //         case PathCapType.Butt: break;
    //         case PathCapType.Round:
    //             if (roundResolution != 0) {
    //                 GenerateRoundCap(builder, info, roundResolution, writer, indexFormat);
    //             }
    //             break;
    //         case PathCapType.Square: GenerateSquareCap(builder, info, writer, indexFormat); break;
    //     }
    //     
    //     static void GenerateRoundCap(MeshBuilder builder, CapGenerateInfo info, int resolution, VertexWriter<Vertex> writer, IndexFormat indexFormat) {
    //         var halfThickness = info.Attribute.Thickness / 2;
    //         var color = info.Attribute.Color;
    //
    //         var position = info.Position;
    //         
    //         int vcount = builder.GetLargestWrittenVertexCount();
    //
    //         if (resolution == 1) {
    //             writer(builder, new(position + info.Direction * halfThickness, color));
    //
    //             if (indexFormat == IndexFormat.UInt16) {
    //                 builder.WriteIndices(stackalloc ushort[] {
    //                     (ushort)(vcount - 2),
    //                     (ushort)(vcount + 0),
    //                     (ushort)(vcount - 1),
    //                 });
    //             }
    //         } else {
    //             writer(builder, new(position, color));
    //
    //             float step = float.Pi / (resolution + 1);
    //             var normal = -info.Normal * halfThickness;
    //             
    //             for (int i = 1; i <= resolution; i++) {
    //                 var direction = Vector2.TransformNormal(normal, Matrix3x2.CreateRotation(step * i));
    //                 writer(builder, new(position + direction, color));
    //             }
    //             
    //             // Triangulation.
    //             if (indexFormat == IndexFormat.UInt16) {
    //                 Span<ushort> indices = stackalloc ushort[3];
    //                 indices[0] = (ushort)vcount;
    //                 
    //                 for (int i = 1; i < resolution; i++) {
    //                     indices[1] = (ushort)(vcount + i);
    //                     indices[2] = (ushort)(vcount + i + 1);
    //                     
    //                     builder.WriteIndices(indices);
    //                 }
    //                 
    //                 builder.WriteIndices(stackalloc ushort[] {
    //                     (ushort)(vcount - 1), (ushort)(vcount + 1), (ushort)vcount,
    //                     (ushort)vcount, (ushort)(vcount + resolution), (ushort)(vcount - 2),
    //                 });
    //             } else {
    //                 Span<uint> indices = stackalloc uint[3];
    //                 indices[0] = (uint)vcount;
    //                 
    //                 for (int i = 1; i < resolution; i++) {
    //                     indices[1] = (uint)(vcount + i);
    //                     indices[2] = (uint)(vcount + i + 1);
    //                     
    //                     builder.WriteIndices(indices);
    //                 }
    //                 
    //                 builder.WriteIndices(stackalloc uint[] {
    //                     (uint)(vcount - 1), (uint)(vcount + 1), (uint)vcount,
    //                     (uint)vcount, (uint)(vcount + resolution), (uint)(vcount - 2),
    //                 });
    //             }
    //         }
    //     }
    //
    //     static void GenerateSquareCap(MeshBuilder builder, CapGenerateInfo info, VertexWriter<Vertex> writer, IndexFormat format) {
    //         var halfThickness = info.Attribute.Thickness / 2;
    //         var normal = info.Normal * halfThickness;
    //         var color = info.Attribute.Color;
    //
    //         var capPos = info.Position + info.Direction * halfThickness;
    //         
    //         int vcount = builder.GetLargestWrittenVertexCount();
    //         
    //         writer(builder, new(capPos + normal, color));
    //         writer(builder, new(capPos - normal, color));
    //
    //         if (format == IndexFormat.UInt16) {
    //             builder.WriteIndices(stackalloc ushort[] {
    //                 (ushort)(vcount - 2),
    //                 (ushort)vcount,
    //                 (ushort)(vcount + 1),
    //                 (ushort)(vcount + 1),
    //                 (ushort)(vcount - 1),
    //                 (ushort)(vcount - 2),
    //             });
    //         } else {
    //             builder.WriteIndices(stackalloc uint[] {
    //                 (uint)(vcount - 2),
    //                 (uint)vcount,
    //                 (uint)(vcount + 1),
    //                 (uint)(vcount + 1),
    //                 (uint)(vcount - 1),
    //                 (uint)(vcount - 2),
    //             });
    //         }
    //     }
    // }

    private readonly record struct CapGenerateInfo(Vector2 Position, Vector2 Direction, Vector2 Normal, PointAttribute Attribute);
}
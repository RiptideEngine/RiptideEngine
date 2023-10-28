namespace RiptideFoundation;

public enum MeshBoundaryShapeType {
    AABB,
    Sphere,
}

[StructLayout(LayoutKind.Explicit)]
public struct MeshBoundaryShape {
    [FieldOffset(0)] public MeshBoundaryShapeType Type;
    [FieldOffset(4)] public Bound3D<float> AABB;
    [FieldOffset(4)] public Sphere<float> Sphere;

    public MeshBoundaryShape(Bound3D<float> aabb) {
        Type = MeshBoundaryShapeType.AABB;
        AABB = aabb;
    }

    public MeshBoundaryShape(Sphere<float> sphere) {
        Type = MeshBoundaryShapeType.Sphere;
        Sphere = sphere;
    }
}
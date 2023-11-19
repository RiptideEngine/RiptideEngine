namespace RiptideFoundation;

public enum MeshBoundaryShapeType {
    AABB,
    Sphere,
}

[StructLayout(LayoutKind.Explicit)]
public struct MeshBoundaryShape {
    [FieldOffset(0)] public MeshBoundaryShapeType Type;
    [FieldOffset(4)] public Bound3D AABB;
    [FieldOffset(4)] public Sphere Sphere;

    public MeshBoundaryShape(Bound3D aabb) {
        Type = MeshBoundaryShapeType.AABB;
        AABB = aabb;
    }

    public MeshBoundaryShape(Sphere sphere) {
        Type = MeshBoundaryShapeType.Sphere;
        Sphere = sphere;
    }
}
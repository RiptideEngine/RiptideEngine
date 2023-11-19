namespace RiptideFoundation;

public struct Frustum {
    public SNPlane Top, Bottom;
    public SNPlane Left, Right;
    public SNPlane Near, Far;
}

public static class FrustumCulling {
    public static Frustum CalculateFrustumPlanes(Matrix4x4 viewProjectionMatrix) {
        return new Frustum {
            Left = SNPlane.Normalize(new(viewProjectionMatrix.M11 + viewProjectionMatrix.M14, viewProjectionMatrix.M21 + viewProjectionMatrix.M24, viewProjectionMatrix.M31 + viewProjectionMatrix.M34, viewProjectionMatrix.M41 + viewProjectionMatrix.M44)),
            Right = SNPlane.Normalize(new(viewProjectionMatrix.M14 - viewProjectionMatrix.M11, viewProjectionMatrix.M24 - viewProjectionMatrix.M21, viewProjectionMatrix.M34 - viewProjectionMatrix.M31, viewProjectionMatrix.M44 - viewProjectionMatrix.M41)),
            Top = SNPlane.Normalize(new(viewProjectionMatrix.M14 - viewProjectionMatrix.M12, viewProjectionMatrix.M24 - viewProjectionMatrix.M22, viewProjectionMatrix.M34 - viewProjectionMatrix.M32, viewProjectionMatrix.M44 - viewProjectionMatrix.M42)),
            Bottom = SNPlane.Normalize(new(viewProjectionMatrix.M14 + viewProjectionMatrix.M12, viewProjectionMatrix.M24 + viewProjectionMatrix.M22, viewProjectionMatrix.M34 + viewProjectionMatrix.M32, viewProjectionMatrix.M44 + viewProjectionMatrix.M42)),
            Near = SNPlane.Normalize(new(viewProjectionMatrix.M13, viewProjectionMatrix.M23, viewProjectionMatrix.M33, viewProjectionMatrix.M43)),
            Far = SNPlane.Normalize(new(viewProjectionMatrix.M14 - viewProjectionMatrix.M13, viewProjectionMatrix.M24 - viewProjectionMatrix.M23, viewProjectionMatrix.M34 - viewProjectionMatrix.M33, viewProjectionMatrix.M44 - viewProjectionMatrix.M43))
        };
    }

    public static bool Test(in Frustum frustum, Vector3 point) {
        return SNPlane.DotCoordinate(frustum.Left, point) >= 0 && SNPlane.DotCoordinate(frustum.Right, point) >= 0 &&
               SNPlane.DotCoordinate(frustum.Top, point) >= 0 && SNPlane.DotCoordinate(frustum.Bottom, point) >= 0 &&
               SNPlane.DotCoordinate(frustum.Near, point) >= 0 && SNPlane.DotCoordinate(frustum.Far, point) >= 0;
    }

    public static unsafe bool Test(in Frustum frustum, Sphere sphere) {
        fixed (SNPlane* pPlanes = &frustum.Top) {
            for (int i = 0; i < 6; i++) {
                float distance = SNPlane.DotCoordinate(pPlanes[i], sphere.Position);

                if (distance < -sphere.Radius) return false;
            }
        }

        return true;
    }

    public static unsafe bool Test(in Frustum frustum, Bound3D box) {
        fixed (SNPlane* pPlanes = &frustum.Top) {
            Vector3 boxMin = box.Min, boxMax = box.Max;

            Vector3* vertices = stackalloc Vector3[8] {
                boxMin,
                boxMin with { X = boxMax.X },
                boxMin with { Y = boxMax.Y },
                boxMax with { Z = boxMin.Z },
                boxMin with { Z = boxMax.Z },
                boxMax with { Y = boxMin.Y },
                boxMax with { X = boxMin.X },
                boxMax,
            };

            for (int i = 0; i < 6; i++) {
                if (SNPlane.DotCoordinate(pPlanes[i], vertices[0]) < 0 &&
                    SNPlane.DotCoordinate(pPlanes[i], vertices[1]) < 0 &&
                    SNPlane.DotCoordinate(pPlanes[i], vertices[2]) < 0 &&
                    SNPlane.DotCoordinate(pPlanes[i], vertices[3]) < 0 &&
                    SNPlane.DotCoordinate(pPlanes[i], vertices[4]) < 0 &&
                    SNPlane.DotCoordinate(pPlanes[i], vertices[5]) < 0 &&
                    SNPlane.DotCoordinate(pPlanes[i], vertices[6]) < 0 &&
                    SNPlane.DotCoordinate(pPlanes[i], vertices[7]) < 0) return false;
            }
        }

        return true;
    }

    //public static unsafe bool TestBox(in Frustum frustum, Vector3 boxOrigin, Vector3 boxSize) {
    //    fixed (SNPlane* pPlanes = &frustum.Top) {
    //        for (int i = 0; i < 6; i++) {
    //            if (SNPlane.DotCoordinate(pPlanes[i], boxOrigin) < 0 &&
    //                SNPlane.DotCoordinate(pPlanes[i], boxOrigin + new Vector3(boxSize.X, 0, 0)) < 0 &&
    //                SNPlane.DotCoordinate(pPlanes[i], boxOrigin + new Vector3(0, boxSize.Y, 0)) < 0 &&
    //                SNPlane.DotCoordinate(pPlanes[i], boxOrigin + new Vector3(boxSize.X, boxSize.Y, 0)) < 0 &&
    //                SNPlane.DotCoordinate(pPlanes[i], boxOrigin + new Vector3(0, 0, boxSize.Z)) < 0 &&
    //                SNPlane.DotCoordinate(pPlanes[i], boxOrigin + new Vector3(boxSize.X, 0, boxSize.Z)) < 0 &&
    //                SNPlane.DotCoordinate(pPlanes[i], boxOrigin + new Vector3(0, boxSize.Y, boxSize.Z)) < 0 &&
    //                SNPlane.DotCoordinate(pPlanes[i], boxOrigin + boxSize) < 0) return false;
    //        }
    //    }

    //    return true;
    //}
}
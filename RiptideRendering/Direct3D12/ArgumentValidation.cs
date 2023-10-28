namespace RiptideRendering.Direct3D12;

internal static unsafe class ArgumentValidation {
    /// <summary>
    /// Determine the whether the given box is fully lied inside resource's boundary.
    /// </summary>
    /// <param name="desc">Descriptor of the resource.</param>
    /// <param name="box">Box to check.</param>
    /// <returns><see langword="true"> if the given box has a valid boundary and fully lies inside the resource, <see langword="false"/> otherwise.</returns>
    public static bool ValidateTextureBoundaryBox(in ResourceDesc desc, in Bound3D<uint> box) {
        if (!ValidateBoundaryBox(box)) return false;

        var width = desc.Width;
        var height = desc.Height;
        var depth = desc.DepthOrArraySize;

        if (box.MinX >= width || box.MinY >= height || box.MinZ >= depth) return false;
        if (box.MaxX > width || box.MaxY > height || box.MaxZ > depth) return false;

        return true;
    }

    /// <summary>
    /// Determine whether the given box has valid boundaries.
    /// </summary>
    /// <param name="box">Box to check.</param>
    /// <returns><see langword="true"> if minimum boundary is less than or equals to maximum boundary on 3 axes, <see langword="false"> otherwise.</returns>
    public static bool ValidateBoundaryBox(in Bound3D<uint> box) {
        return box.MinX <= box.MaxX && box.MinY <= box.MaxY && box.MinZ <= box.MaxZ;
    }
}
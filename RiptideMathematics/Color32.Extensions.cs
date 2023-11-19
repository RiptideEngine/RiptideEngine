namespace RiptideMathematics; 

partial struct Color32 {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Color32 Add(Color32 left, Color32 right) => new((byte)(left.R + right.R), (byte)(left.G + right.G), (byte)(left.B + right.B), (byte)(left.A + right.A));
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Color32 Subtract(Color32 left, Color32 right) => new((byte)(left.R - right.R), (byte)(left.G - right.G), (byte)(left.B - right.B), (byte)(left.A - right.A));
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Color32 Multiply(Color32 left, Color32 right) => new((byte)(left.R * right.R), (byte)(left.G * right.G), (byte)(left.B * right.B), (byte)(left.A * right.A));
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Color32 Divide(Color32 left, Color32 right) => new((byte)(left.R / right.R), (byte)(left.G / right.G), (byte)(left.B / right.B), (byte)(left.A / right.A));
}
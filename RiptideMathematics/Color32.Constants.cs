namespace RiptideMathematics; 

partial struct Color32 {
    public static Color32 Black => new(0, 0, 0);
    public static Color32 Transparent => new(0, 0, 0, 0);
    public static Color32 Red => new(255, 0, 0);
    public static Color32 Green => new(0, 255, 0);
    public static Color32 Blue => new(0, 0, 255);
    public static Color32 White => new(255, 255, 255);
}
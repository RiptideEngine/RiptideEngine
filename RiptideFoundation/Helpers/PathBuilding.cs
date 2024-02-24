﻿namespace RiptideFoundation.Helpers;

public partial struct PathBuilder {
    private enum OperationType {
        SetColor,
        SetThickness,
        MoveTo,
        LineTo,
        HorizontalLine,
        VerticalLine,
        QuadraticBezier,
        CubicBezier,
        Close,
    }
    
    [StructLayout(LayoutKind.Explicit)]
    private readonly struct PathOperation {
        [field: FieldOffset(0)] public required OperationType Type { get; init; }
        [field: FieldOffset(4)] public MoveOperation Move { get; init; }
        [field: FieldOffset(4)] public LineOperation Line { get; init; }
        [field: FieldOffset(4)] public AxisAlignedLineOperation HorizontalLine { get; init; }
        [field: FieldOffset(4)] public AxisAlignedLineOperation VerticalLine { get; init; }
        [field: FieldOffset(4)] public Color32 Color { get; init; }
        [field: FieldOffset(4)] public float Thickness { get; init; }
        [field: FieldOffset(4)] public QuadraticBezierOperation QuadraticBezier { get; init; }
        [field: FieldOffset(4)] public CubicBezierOperation CubicBezier { get; init; }
        [field: FieldOffset(4)] public CloseOperation Close { get; init; }

        public struct MoveOperation {
            public required Vector2 Destination;
        }

        public struct LineOperation {
            public required Vector2 Destination;
        }

        public struct AxisAlignedLineOperation {
            public required float Destination;
        }

        public readonly record struct QuadraticBezierOperation(Vector2 Control, Vector2 Destination);
        public readonly record struct CubicBezierOperation(Vector2 StartControl, Vector2 EndControl, Vector2 Destination);
        
        public struct CloseOperation {
            public required bool Loop;
        }
    }
    
    private readonly record struct PointAttribute(float Thickness, Color32 Color);
}
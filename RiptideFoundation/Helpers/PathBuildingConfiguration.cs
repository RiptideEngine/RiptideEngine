namespace RiptideFoundation.Helpers;

public readonly struct PathBuildingConfiguration {
    public static PathBuildingConfiguration Default => new() {
        LineDistanceThreshold = 0.001f,
        BezierCurveResolution = 16,
        RoundCapResolution = 8,
    };
    
    public required float LineDistanceThreshold { get; init; }
    public required int BezierCurveResolution { get; init; }
    public required int RoundCapResolution { get; init; }
    
    public PathBuildingConfiguration() { }
}
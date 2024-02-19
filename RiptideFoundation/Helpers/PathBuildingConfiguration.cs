namespace RiptideFoundation.Helpers;

public readonly struct PathBuildingConfiguration {
    public required readonly float LineDistanceThreshold { get; init; } = 0.001f;
    
    public PathBuildingConfiguration() { }
}
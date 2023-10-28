namespace RiptideFoundation;

public interface ISceneGraphService : IRiptideService {
    SceneContext Context { get; }
}
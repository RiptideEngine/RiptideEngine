namespace RiptideFoundation;

public static class Components
{
    internal static IComponentDatabase Database { get; private set; } = null!;

    internal static void Initialize(RiptideServices services)
    {
        Database = services.TryGetService<IComponentDatabase>(out var service) ? service : services.CreateService<IComponentDatabase, ComponentDatabaseService>();
    }
}
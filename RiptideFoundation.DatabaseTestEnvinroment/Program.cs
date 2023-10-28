using RiptideDatabase;
using RiptideEngine.Core;
using RiptideFoundation.Database;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace RiptideFoundation.DatabaseTestEnvinroment;

public class ReferenceAsset : IResourceAsset {
    public string Name { get; set; } = string.Empty;
    public ReferenceAsset? Other { get; set; }
    public bool IsResourceAsset { get; }

    public bool CanInstantiate<T>() {
        throw new NotImplementedException();
    }

    public bool CanInstantiate(Type outputType) {
        throw new NotImplementedException();
    }

    public bool TryInstantiate<T>([NotNullWhen(true)] out T? output) {
        throw new NotImplementedException();
    }

    public bool TryInstantiate(Type outputType, [NotNullWhen(true)] out object? output) {
        throw new NotImplementedException();
    }
}

public sealed class FileProtocolProvider : IDataProvider {
    public bool CanProvide(string protocol, string path) {
        return protocol == "file" && File.Exists(path);
    }

    public Stream CreateStream(string path) {
        return File.OpenRead(path);
    }
}

public sealed class ReferenceAssetProvider : IResourceImporter {
    public bool CanImport(Type requestingType) => requestingType.IsAssignableTo(typeof(ReferenceAsset));

    public object CreateVessel(Type requestingType) {
        return new ReferenceAsset();
    }

    public async Task PopulateData(Stream dataStream, Type requestingType, object created) {
        Debug.Assert(created is ReferenceAsset);
        
        var node = JsonSerializer.Deserialize<JsonNode>(dataStream)!;

        var name = node["Name"]!.GetValue<string>();
        var reference = node["Other"]!.GetValue<string>();

        ReferenceAsset asset = Unsafe.As<ReferenceAsset>(created);

        var other = Program.Database.LoadResource($"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", reference)}", new ImportingOptions("file", typeof(ReferenceAsset)));

        asset.Name = name;
        asset.Other = (ReferenceAsset)(await other.Task)!;
    }
}

internal class Program {
    public static ResourceDatabase Database { get; private set; } = null!;

    private static async Task Main(string[] args) {
        Console.WriteLine("Begin Database Test.");

        RiptideServices services = new();

        Database = services.CreateService<IResourceDatabase, ResourceDatabase>();

        Database.RegisterDataProvider(new FileProtocolProvider());
        Database.RegisterResourceProvider(new ReferenceAssetProvider());

        var result = Database.LoadResource($"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Reference1.txt")}", new ImportingOptions("file", typeof(ReferenceAsset)));

        Console.WriteLine(result.Task.Status);

        await result.Task;

        services.RemoveAllServices();
        Console.WriteLine("Finish");
    }
}
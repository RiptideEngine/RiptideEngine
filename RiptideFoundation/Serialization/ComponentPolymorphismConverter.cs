namespace RiptideFoundation.Serialization;

internal sealed class ComponentPolymorphismConverter : JsonConverter<Component> {
    private static readonly JsonSerializerOptions SerializeOptions = new() {
        WriteIndented = false,
        IncludeFields = true,
        IgnoreReadOnlyFields = true,
        IgnoreReadOnlyProperties = true,
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
        Converters = { new ResourceReferenceConverter() },
    };

    public sealed override Component? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException("Expected StartObject token.");

        // Requires type and values must be in order, which is not a standard of JSON but it's easier to implement.

        reader.Read();
        if (!reader.ValueTextEquals("$type")) throw new JsonException("Expected type discriminator '$type'.");

        reader.Read();
        var componentTypeGuid = reader.GetGuid();

        if (!RuntimeFoundation.ComponentDatabase.TryGetComponentType(componentTypeGuid, out var componentType)) return null;

        reader.Read();
        if (!reader.ValueTextEquals("$values")) throw new JsonException("Expected value metadata '$values' after type discriminator.");

        reader.Read();
        var component = (Component?)JsonSerializer.Deserialize(ref reader, options.GetTypeInfo(componentType));

        reader.Read();
        if (reader.TokenType != JsonTokenType.EndObject) throw new JsonException("Expected EndObject token.");

        return component;
    }

    public sealed override void Write(Utf8JsonWriter writer, Component value, JsonSerializerOptions options) {
        writer.WriteStartObject();

        if (RuntimeFoundation.ComponentDatabase.TryGetComponentGuid(value.GetType(), out var guid)) {
            writer.WriteString("$type"u8, guid);

            writer.WritePropertyName("$values"u8);

            JsonSerializer.Serialize(writer, value, value.GetType(), SerializeOptions);
        }

        writer.WriteEndObject();
    }
}

internal sealed class ResourceReferenceConverter : JsonConverter<IResourceAsset> {
    public override bool CanConvert(Type typeToConvert) {
        return typeToConvert.IsAssignableTo(typeof(IResourceAsset));
    }

    public override IResourceAsset? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        throw new NotImplementedException();
    }

    public override void Write(Utf8JsonWriter writer, IResourceAsset value, JsonSerializerOptions options) {
        writer.WriteStartObject();

        if (RuntimeFoundation.ResourceDatabase.TryGetResourceImportLocation(value, out var location)) {
            writer.WriteString("$ref"u8, $"{location.Protocol}:{location.ResourceGuid}");
        } else {
            writer.WriteNull("$ref"u8);
        }

        writer.WriteEndObject();
    }
}
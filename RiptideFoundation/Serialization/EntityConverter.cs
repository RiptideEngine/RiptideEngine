
namespace RiptideFoundation.Serialization;

public sealed class EntityConverter : JsonConverter<Entity> {
    private static readonly JsonSerializerOptions ComponentSerializationOptions = new() {
        WriteIndented = false,
        IncludeFields = true,
        IgnoreReadOnlyFields = true,
        IgnoreReadOnlyProperties = true,
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
        Converters = { new ComponentPolymorphismConverter() },
    };

    public override Entity? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException("Expected StartObject token.");

        var output = new Entity();

        while (reader.Read()) {
            switch (reader.TokenType) {
                case JsonTokenType.EndObject: return output;
                case JsonTokenType.PropertyName:
                    string propertyName = reader.GetString()!;

                    switch (propertyName) {
                        case "Name":
                            reader.Read();
                            output.Name = reader.GetString();
                            break;

                        case "Position":
                            reader.Read();
                            output.GlobalPosition = JsonSerializer.Deserialize<Vector3>(ref reader, options);
                            break;

                        case "Rotation":
                            reader.Read();
                            output.GlobalRotation = JsonSerializer.Deserialize<Quaternion>(ref reader, options);
                            break;

                        case "Scale":
                            reader.Read();
                            output.GlobalScale = JsonSerializer.Deserialize<Vector3>(ref reader, options);
                            break;

                        case "Components":
                            reader.Read();
                            
                            foreach (var deserialized in JsonSerializer.Deserialize<List<Component>>(ref reader, ComponentSerializationOptions) ?? Enumerable.Empty<Component>()) {
                                output.AddDeserializedComponent(deserialized);
                            }
                            break;

                        default: reader.Skip(); break;
                    }
                    break;
            }
        }

        throw new JsonException();
    }

    public override void Write(Utf8JsonWriter writer, Entity value, JsonSerializerOptions options) {
        writer.WriteStartObject();

        writer.WriteString("Name"u8, value.Name);

        writer.WritePropertyName("Position"u8);
        JsonSerializer.Serialize(writer, value.GlobalPosition, options);
        writer.WritePropertyName("Rotation"u8);
        JsonSerializer.Serialize(writer, value.GlobalRotation, options);
        writer.WritePropertyName("Scale"u8);
        JsonSerializer.Serialize(writer, value.GlobalScale, options);

        writer.WriteStartArray("Components"u8);
        
        foreach (var component in value.EnumerateComponents()) {
            JsonSerializer.Serialize(writer, component, ComponentSerializationOptions);
        }

        writer.WriteEndArray();

        writer.WriteEndObject();
    }
}
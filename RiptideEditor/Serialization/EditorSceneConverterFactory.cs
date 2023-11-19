namespace RiptideEditor.Serialization;

internal sealed class EditorSceneConverter : JsonConverter<Scene> {
    private static readonly JsonSerializerOptions EntitySerializeOptions = new() {
        WriteIndented = false,
        IncludeFields = true,
        IgnoreReadOnlyFields = true,
        IgnoreReadOnlyProperties = true,
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
        Converters = { new EntityConverter() },
    };

    public override Scene? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        throw new NotImplementedException();
    }

    public override void Write(Utf8JsonWriter writer, Scene value, JsonSerializerOptions options) {
        writer.WriteStartObject();

        writer.WriteString("Name"u8, value.Name);

        writer.WriteStartArray("Entities"u8);
        if (value.RootEntityCount != 0) {
            foreach (var entity in value.EnumerateRootEntities()) {
                JsonSerializer.Serialize(writer, entity, EntitySerializeOptions);
            }
        }
        writer.WriteEndArray();

        writer.WriteEndObject();
    }
}

internal sealed class EditorSceneConverterFactory : JsonConverterFactory {
    private static readonly Type _sceneType = typeof(Scene);
    private static readonly Type _entityType = typeof(Entity);
    private static readonly Type _componentType = typeof(Component);
    private static readonly Type _assetInterfaceType = typeof(IResourceAsset);

    public override bool CanConvert(Type typeToConvert) {
        return typeToConvert == _sceneType || typeToConvert == _entityType || typeToConvert == _componentType || typeToConvert.IsAssignableTo(_assetInterfaceType);
    }

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options) {
        return typeToConvert switch {
            var _ when typeToConvert == _sceneType => new EditorSceneConverter(),
            var _ when typeToConvert == _entityType => new EntityConverter(),
            var _ when typeToConvert == _componentType => new ComponentPolymorphismConverter(),
            var _ when typeToConvert.IsAssignableTo(_assetInterfaceType) => new AssetReferenceConverter(),
            _ => throw new NotImplementedException($"Failed to create JsonConverter for type '{typeToConvert.FullName}'."),
        };
    }

    private sealed class EditorSceneConverter : JsonConverter<Scene> {
        public override Scene? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            //if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException("Expected StartObject token.");

            //Scene output = new();

            //options.Converters = null;

            //while (reader.Read()) {
            //    switch (reader.TokenType) {
            //        case JsonTokenType.EndObject: return output;
            //        case JsonTokenType.PropertyName:
            //            string propertyName = reader.GetString()!;

            //            switch (propertyName) {
            //                case "Name":
            //                    reader.Read();
            //                    output.Name = reader.GetString();
            //                    break;

            //                case "Entities":
            //                    reader.Read();

            //                    foreach (var deserialized in JsonSerializer.Deserialize<List<Entity>>(ref reader, options) ?? Enumerable.Empty<Entity>()) {
            //                        output.AddDeserializedEntity(deserialized);
            //                    }
            //                    break;

            //                default: reader.Skip(); break;
            //            }
            //            break;
            //    }
            //}

            //throw new JsonException();

            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, Scene value, JsonSerializerOptions options) {
            writer.WriteStartObject();

            if (RuntimeFoundation.ResourceDatabase.TryGetResourceImportLocation(value, out var location)) {
                writer.WriteString("$ref"u8, $"{location.Protocol}:{location.ResourceGuid}");
            } else {
                writer.WriteString("Name"u8, value.Name);

                writer.WritePropertyName("Entities");
                writer.WriteStartArray();
                {
                    foreach (var entity in value.EnumerateRootEntities()) {
                        JsonSerializer.Serialize(writer, entity, options);
                    }
                }
                writer.WriteEndArray();
            }

            writer.WriteEndObject();
        }
    }
    private sealed class AssetReferenceConverter : JsonConverter<IResourceAsset> {
        public override IResourceAsset? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            switch (reader.TokenType) {
                case JsonTokenType.Null: return null;
                case JsonTokenType.StartObject:
                    IResourceAsset? output = null;

                    while (reader.Read()) {
                        switch (reader.TokenType) {
                            case JsonTokenType.EndObject: return output;
                            case JsonTokenType.PropertyName:
                                string propertyName = reader.GetString()!;

                                switch (propertyName) {
                                    case "$ref":
                                        reader.Read();
                                        //output = RuntimeFoundation.ResourceDatabase.LoadResource();
                                        break;

                                    default: reader.Skip(); break;
                                }
                                break;
                        }
                    }

                    throw new JsonException();

                default: throw new JsonException("Expected Null value or StartObject token.");
            }
        }

        public override void Write(Utf8JsonWriter writer, IResourceAsset value, JsonSerializerOptions options) {
            writer.WriteStartObject();

            if (RuntimeFoundation.ResourceDatabase.TryGetResourceImportLocation(value, out var location)) {
                writer.WriteString("$ref", $"{location.Protocol}:{location.ResourceGuid}");
            }

            writer.WriteEndObject();
        }
    }
}
namespace RiptideFoundation;

public sealed class RuntimeSceneConverter : JsonConverter<Scene> {
    public override void Write(Utf8JsonWriter writer, Scene value, JsonSerializerOptions options) {
        writer.WriteStartArray("entities"u8);

        foreach (var entity in value.EnumerateRootEntities()) {
            JsonSerializer.Serialize(writer, entity);
        }

        writer.WriteEndArray();
    }

    public override Scene? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        throw new NotImplementedException();

        //reader.Read();

        //if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException("Start object token expected.");

        //while (reader.Read()) {
        //    if (reader.TokenType == JsonTokenType.EndObject) break;

        //    switch (reader.TokenType) {

        //        case JsonTokenType.PropertyName:
        //            if (reader.ValueTextEquals("entities"u8)) {

        //            }
        //            break;
        //    }
        //}
    }
}
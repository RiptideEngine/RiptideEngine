namespace RiptideMathematics;

internal sealed class ColorConverter : JsonConverter<Color> {
    public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        Color output = default;
        
        switch (reader.TokenType) {
            case JsonTokenType.StartArray:
                ref float destination = ref Unsafe.As<Color, float>(ref output);

                for (int i = 0; i < 4; i++) {
                    reader.Read();
                    if (reader.TokenType == JsonTokenType.EndArray) break;

                    destination = reader.GetSingle();
                    destination = ref Unsafe.Add(ref destination, 1);
                }

                // Consume the rest of the array.
                while (reader.TokenType != JsonTokenType.EndArray) {
                    reader.Read();

                    if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray) {
                        reader.Skip();
                        reader.Read();
                    }
                }

                return output;

            case JsonTokenType.StartObject:
                while (reader.Read()) {
                    switch (reader.TokenType) {
                        case JsonTokenType.EndObject: return output;

                        case JsonTokenType.PropertyName:
                            string propertyName = reader.GetString()!;
                            reader.Read();

                            switch (propertyName) {
                                case "R": output.R = reader.GetSingle(); break;
                                case "G": output.G = reader.GetSingle(); break;
                                case "B": output.B = reader.GetSingle(); break;
                                case "A": output.A = reader.GetSingle(); break;
                            }
                            break;
                    }
                }

                break;

            default: throw new JsonException();
        }

        throw new JsonException();
    }

    public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options) {
        writer.WriteStartArray();

        writer.WriteNumberValue(value.R);
        writer.WriteNumberValue(value.G);
        writer.WriteNumberValue(value.B);
        writer.WriteNumberValue(value.A);

        writer.WriteEndArray();
    }
}
namespace RiptideMathematics;

internal sealed class RectangleConverterFactory : JsonConverterFactory {
    public override bool CanConvert(Type type) {
        return type.GetGenericTypeDefinition() == typeof(Rectangle<>) && type.GetGenericArguments()[0].IsPrimitive;
    }

    public override JsonConverter? CreateConverter(Type type, JsonSerializerOptions options) {
        if (type == typeof(Rectangle<float>)) return new Float32Converter();
        if (type == typeof(Rectangle<double>)) return new Float64Converter();
        if (type == typeof(Rectangle<byte>)) return new ByteConverter();
        if (type == typeof(Rectangle<sbyte>)) return new SByteConverter();
        if (type == typeof(Rectangle<short>)) return new Int16Converter();
        if (type == typeof(Rectangle<ushort>)) return new UInt16Converter();
        if (type == typeof(Rectangle<int>)) return new Int32Converter();
        if (type == typeof(Rectangle<uint>)) return new UInt32Converter();
        if (type == typeof(Rectangle<long>)) return new Int64Converter();
        if (type == typeof(Rectangle<ulong>)) return new UInt64Converter();
        if (type == typeof(Rectangle<char>)) return new CharConverter();

        throw new NotImplementedException($"Unimplemented converter creation for type '{type.FullName}'.");
    }

    private abstract class Converter<T> : JsonConverter<Rectangle<T>> where T : unmanaged, INumber<T> {
        protected abstract T Deserialize(ref Utf8JsonReader reader);
        public sealed override Rectangle<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            Rectangle<T> output = default;

            switch (reader.TokenType) {
                case JsonTokenType.StartArray:
                    ref T destination = ref Unsafe.As<Rectangle<T>, T>(ref output);

                    for (int i = 0; i < 4; i++) {
                        reader.Read();
                        if (reader.TokenType == JsonTokenType.EndArray) break;

                        destination = Deserialize(ref reader);
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
                                    case "R": output.X = Deserialize(ref reader); break;
                                    case "G": output.Y = Deserialize(ref reader); break;
                                    case "B": output.W = Deserialize(ref reader); break;
                                    case "A": output.H = Deserialize(ref reader); break;
                                }
                                break;
                        }
                    }

                    break;

                default: throw new JsonException();
            }

            throw new JsonException();
        }
        
        protected abstract void WriteRectangleValues(Utf8JsonWriter writer, Rectangle<T> value, JsonSerializerOptions options);
        public sealed override void Write(Utf8JsonWriter writer, Rectangle<T> value, JsonSerializerOptions options) {
            writer.WriteStartArray();

            WriteRectangleValues(writer, value, options);

            writer.WriteEndArray();
        }
    }
    private sealed class Float32Converter : Converter<float> {
        protected override float Deserialize(ref Utf8JsonReader reader) {
            return reader.GetSingle();
        }

        protected override void WriteRectangleValues(Utf8JsonWriter writer, Rectangle<float> value, JsonSerializerOptions options) {
            writer.WriteNumberValue(value.X);
            writer.WriteNumberValue(value.Y);
            writer.WriteNumberValue(value.W);
            writer.WriteNumberValue(value.H);
        }
    }
    private sealed class Float64Converter : Converter<double> {
        protected override double Deserialize(ref Utf8JsonReader reader) {
            return reader.GetDouble();
        }

        protected override void WriteRectangleValues(Utf8JsonWriter writer, Rectangle<double> value, JsonSerializerOptions options) {
            writer.WriteNumberValue(value.X);
            writer.WriteNumberValue(value.Y);
            writer.WriteNumberValue(value.W);
            writer.WriteNumberValue(value.H);
        }
    }
    private sealed class ByteConverter : Converter<byte> {
        protected override byte Deserialize(ref Utf8JsonReader reader) {
            return reader.GetByte();
        }

        protected override void WriteRectangleValues(Utf8JsonWriter writer, Rectangle<byte> value, JsonSerializerOptions options) {
            writer.WriteNumberValue(value.X);
            writer.WriteNumberValue(value.Y);
            writer.WriteNumberValue(value.W);
            writer.WriteNumberValue(value.H);
        }
    }
    private sealed class SByteConverter : Converter<sbyte> {
        protected override sbyte Deserialize(ref Utf8JsonReader reader) {
            return reader.GetSByte();
        }

        protected override void WriteRectangleValues(Utf8JsonWriter writer, Rectangle<sbyte> value, JsonSerializerOptions options) {
            writer.WriteNumberValue(value.X);
            writer.WriteNumberValue(value.Y);
            writer.WriteNumberValue(value.W);
            writer.WriteNumberValue(value.H);
        }
    }
    private sealed class Int16Converter : Converter<short> {
        protected override short Deserialize(ref Utf8JsonReader reader) {
            return reader.GetInt16();
        }

        protected override void WriteRectangleValues(Utf8JsonWriter writer, Rectangle<short> value, JsonSerializerOptions options) {
            writer.WriteNumberValue(value.X);
            writer.WriteNumberValue(value.Y);
            writer.WriteNumberValue(value.W);
            writer.WriteNumberValue(value.H);
        }
    }
    private sealed class UInt16Converter : Converter<ushort> {
        protected override ushort Deserialize(ref Utf8JsonReader reader) {
            return reader.GetUInt16();
        }

        protected override void WriteRectangleValues(Utf8JsonWriter writer, Rectangle<ushort> value, JsonSerializerOptions options) {
            writer.WriteNumberValue(value.X);
            writer.WriteNumberValue(value.Y);
            writer.WriteNumberValue(value.W);
            writer.WriteNumberValue(value.H);
        }
    }
    private sealed class Int32Converter : Converter<int> {
        protected override int Deserialize(ref Utf8JsonReader reader) {
            return reader.GetInt32();
        }

        protected override void WriteRectangleValues(Utf8JsonWriter writer, Rectangle<int> value, JsonSerializerOptions options) {
            writer.WriteNumberValue(value.X);
            writer.WriteNumberValue(value.Y);
            writer.WriteNumberValue(value.W);
            writer.WriteNumberValue(value.H);
        }
    }
    private sealed class UInt32Converter : Converter<uint> {
        protected override uint Deserialize(ref Utf8JsonReader reader) {
            return reader.GetUInt32();
        }

        protected override void WriteRectangleValues(Utf8JsonWriter writer, Rectangle<uint> value, JsonSerializerOptions options) {
            writer.WriteNumberValue(value.X);
            writer.WriteNumberValue(value.Y);
            writer.WriteNumberValue(value.W);
            writer.WriteNumberValue(value.H);
        }
    }
    private sealed class Int64Converter : Converter<long> {
        protected override long Deserialize(ref Utf8JsonReader reader) {
            return reader.GetInt64();
        }

        protected override void WriteRectangleValues(Utf8JsonWriter writer, Rectangle<long> value, JsonSerializerOptions options) {
            writer.WriteNumberValue(value.X);
            writer.WriteNumberValue(value.Y);
            writer.WriteNumberValue(value.W);
            writer.WriteNumberValue(value.H);
        }
    }
    private sealed class UInt64Converter : Converter<ulong> {
        protected override ulong Deserialize(ref Utf8JsonReader reader) {
            return reader.GetUInt64();
        }

        protected override void WriteRectangleValues(Utf8JsonWriter writer, Rectangle<ulong> value, JsonSerializerOptions options) {
            writer.WriteNumberValue(value.X);
            writer.WriteNumberValue(value.Y);
            writer.WriteNumberValue(value.W);
            writer.WriteNumberValue(value.H);
        }
    }
    private sealed class CharConverter : Converter<char> {
        protected override char Deserialize(ref Utf8JsonReader reader) {
            return (char)reader.GetUInt16();
        }

        protected override void WriteRectangleValues(Utf8JsonWriter writer, Rectangle<char> value, JsonSerializerOptions options) {
            writer.WriteNumberValue(value.X);
            writer.WriteNumberValue(value.Y);
            writer.WriteNumberValue(value.W);
            writer.WriteNumberValue(value.H);
        }
    }
}
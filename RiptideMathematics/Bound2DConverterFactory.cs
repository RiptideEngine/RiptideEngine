namespace RiptideMathematics;

internal sealed class Bound2DConverterFactory : JsonConverterFactory {
    public override bool CanConvert(Type type) {
        return type.GetGenericTypeDefinition() == typeof(Bound2D<>) && type.GetGenericArguments()[0].IsPrimitive;
    }

    public override JsonConverter? CreateConverter(Type type, JsonSerializerOptions options) {
        if (type == typeof(Bound2D<float>)) return new Float32Converter();
        if (type == typeof(Bound2D<double>)) return new Float64Converter();
        if (type == typeof(Bound2D<byte>)) return new ByteConverter();
        if (type == typeof(Bound2D<sbyte>)) return new SByteConverter();
        if (type == typeof(Bound2D<short>)) return new Int16Converter();
        if (type == typeof(Bound2D<ushort>)) return new UInt16Converter();
        if (type == typeof(Bound2D<int>)) return new Int32Converter();
        if (type == typeof(Bound2D<uint>)) return new UInt32Converter();
        if (type == typeof(Bound2D<long>)) return new Int64Converter();
        if (type == typeof(Bound2D<ulong>)) return new UInt64Converter();
        if (type == typeof(Bound2D<char>)) return new CharConverter();

        throw new NotImplementedException($"Unimplemented converter creation for type '{type.FullName}'.");
    }

    private abstract class Converter<T> : JsonConverter<Bound2D<T>> where T : unmanaged, INumber<T> {
        protected abstract T Deserialize(ref Utf8JsonReader reader);
        public sealed override Bound2D<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            Bound2D<T> output = default;

            switch (reader.TokenType) {
                case JsonTokenType.StartArray:
                    ref T destination = ref Unsafe.As<Bound2D<T>, T>(ref output);

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
                                    case "MinX": output.MinX = Deserialize(ref reader); break;
                                    case "MinY": output.MinY = Deserialize(ref reader); break;
                                    case "MaxX": output.MaxX = Deserialize(ref reader); break;
                                    case "MaxY": output.MaxY = Deserialize(ref reader); break;
                                }
                                break;
                        }
                    }

                    break;

                default: throw new JsonException();
            }

            throw new JsonException();
        }

        protected abstract void WriteRectangleValues(Utf8JsonWriter writer, Bound2D<T> value, JsonSerializerOptions options);
        public sealed override void Write(Utf8JsonWriter writer, Bound2D<T> value, JsonSerializerOptions options) {
            writer.WriteStartArray();

            WriteRectangleValues(writer, value, options);

            writer.WriteEndArray();
        }
    }
    private sealed class Float32Converter : Converter<float> {
        protected override float Deserialize(ref Utf8JsonReader reader) {
            return reader.GetSingle();
        }

        protected override void WriteRectangleValues(Utf8JsonWriter writer, Bound2D<float> value, JsonSerializerOptions options) {
            writer.WriteNumberValue(value.MinX);
            writer.WriteNumberValue(value.MinY);
            writer.WriteNumberValue(value.MaxX);
            writer.WriteNumberValue(value.MaxY);
        }
    }
    private sealed class Float64Converter : Converter<double> {
        protected override double Deserialize(ref Utf8JsonReader reader) {
            return reader.GetDouble();
        }

        protected override void WriteRectangleValues(Utf8JsonWriter writer, Bound2D<double> value, JsonSerializerOptions options) {
            writer.WriteNumberValue(value.MinX);
            writer.WriteNumberValue(value.MinY);
            writer.WriteNumberValue(value.MaxX);
            writer.WriteNumberValue(value.MaxY);
        }
    }
    private sealed class ByteConverter : Converter<byte> {
        protected override byte Deserialize(ref Utf8JsonReader reader) {
            return reader.GetByte();
        }

        protected override void WriteRectangleValues(Utf8JsonWriter writer, Bound2D<byte> value, JsonSerializerOptions options) {
            writer.WriteNumberValue(value.MinX);
            writer.WriteNumberValue(value.MinY);
            writer.WriteNumberValue(value.MaxX);
            writer.WriteNumberValue(value.MaxY);
        }
    }
    private sealed class SByteConverter : Converter<sbyte> {
        protected override sbyte Deserialize(ref Utf8JsonReader reader) {
            return reader.GetSByte();
        }

        protected override void WriteRectangleValues(Utf8JsonWriter writer, Bound2D<sbyte> value, JsonSerializerOptions options) {
            writer.WriteNumberValue(value.MinX);
            writer.WriteNumberValue(value.MinY);
            writer.WriteNumberValue(value.MaxX);
            writer.WriteNumberValue(value.MaxY);
        }
    }
    private sealed class Int16Converter : Converter<short> {
        protected override short Deserialize(ref Utf8JsonReader reader) {
            return reader.GetInt16();
        }

        protected override void WriteRectangleValues(Utf8JsonWriter writer, Bound2D<short> value, JsonSerializerOptions options) {
            writer.WriteNumberValue(value.MinX);
            writer.WriteNumberValue(value.MinY);
            writer.WriteNumberValue(value.MaxX);
            writer.WriteNumberValue(value.MaxY);
        }
    }
    private sealed class UInt16Converter : Converter<ushort> {
        protected override ushort Deserialize(ref Utf8JsonReader reader) {
            return reader.GetUInt16();
        }

        protected override void WriteRectangleValues(Utf8JsonWriter writer, Bound2D<ushort> value, JsonSerializerOptions options) {
            writer.WriteNumberValue(value.MinX);
            writer.WriteNumberValue(value.MinY);
            writer.WriteNumberValue(value.MaxX);
            writer.WriteNumberValue(value.MaxY);
        }
    }
    private sealed class Int32Converter : Converter<int> {
        protected override int Deserialize(ref Utf8JsonReader reader) {
            return reader.GetInt32();
        }

        protected override void WriteRectangleValues(Utf8JsonWriter writer, Bound2D<int> value, JsonSerializerOptions options) {
            writer.WriteNumberValue(value.MinX);
            writer.WriteNumberValue(value.MinY);
            writer.WriteNumberValue(value.MaxX);
            writer.WriteNumberValue(value.MaxY);
        }
    }
    private sealed class UInt32Converter : Converter<uint> {
        protected override uint Deserialize(ref Utf8JsonReader reader) {
            return reader.GetUInt32();
        }

        protected override void WriteRectangleValues(Utf8JsonWriter writer, Bound2D<uint> value, JsonSerializerOptions options) {
            writer.WriteNumberValue(value.MinX);
            writer.WriteNumberValue(value.MinY);
            writer.WriteNumberValue(value.MaxX);
            writer.WriteNumberValue(value.MaxY);
        }
    }
    private sealed class Int64Converter : Converter<long> {
        protected override long Deserialize(ref Utf8JsonReader reader) {
            return reader.GetInt64();
        }

        protected override void WriteRectangleValues(Utf8JsonWriter writer, Bound2D<long> value, JsonSerializerOptions options) {
            writer.WriteNumberValue(value.MinX);
            writer.WriteNumberValue(value.MinY);
            writer.WriteNumberValue(value.MaxX);
            writer.WriteNumberValue(value.MaxY);
        }
    }
    private sealed class UInt64Converter : Converter<ulong> {
        protected override ulong Deserialize(ref Utf8JsonReader reader) {
            return reader.GetUInt64();
        }

        protected override void WriteRectangleValues(Utf8JsonWriter writer, Bound2D<ulong> value, JsonSerializerOptions options) {
            writer.WriteNumberValue(value.MinX);
            writer.WriteNumberValue(value.MinY);
            writer.WriteNumberValue(value.MaxX);
            writer.WriteNumberValue(value.MaxY);
        }
    }
    private sealed class CharConverter : Converter<char> {
        protected override char Deserialize(ref Utf8JsonReader reader) {
            return (char)reader.GetUInt16();
        }

        protected override void WriteRectangleValues(Utf8JsonWriter writer, Bound2D<char> value, JsonSerializerOptions options) {
            writer.WriteNumberValue(value.MinX);
            writer.WriteNumberValue(value.MinY);
            writer.WriteNumberValue(value.MaxX);
            writer.WriteNumberValue(value.MaxY);
        }
    }
}
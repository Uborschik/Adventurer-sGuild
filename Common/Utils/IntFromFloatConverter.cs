using System;
using System.Text.Json;
using System.Text.Json.Serialization;

public class IntFromFloatConverter : JsonConverter<int>
{
    public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            if (reader.TryGetInt32(out int intValue))
                return intValue;

            double doubleValue = reader.GetDouble();
            return (int)Math.Round(doubleValue);
        }

        throw new JsonException($"Ожидалось число, получено {reader.TokenType}");
    }

    public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
        => writer.WriteNumberValue(value);
}
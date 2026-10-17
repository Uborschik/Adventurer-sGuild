namespace AdventurersGuild.Data.IO;

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using AdventurersGuild.Domain.Quests;

public class OutcomeInfoConverter : JsonConverter<OutcomeInfo>
{
    public override OutcomeInfo Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            return new OutcomeInfo { Id = reader.GetString() };
        }

        if (reader.TokenType == JsonTokenType.StartObject)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;

            return new OutcomeInfo
            {
                Id = root.TryGetProperty("id", out var id) ? id.GetString() : null,
                Actor = root.TryGetProperty("actor", out var actor) ? actor.GetInt32() : 0,
                Target = root.TryGetProperty("target", out var target) ? target.GetInt32() : 0,
                Consequences = root.TryGetProperty("consequences", out var cons)
                    ? cons.Deserialize<List<ConsequenceInfo>>(options) ?? []
                    : []
            };
        }

        throw new JsonException("Неверный формат outcome (ожидалась строка или объект)");
    }

    public override void Write(Utf8JsonWriter writer, OutcomeInfo value, JsonSerializerOptions options) => throw new NotImplementedException();
}
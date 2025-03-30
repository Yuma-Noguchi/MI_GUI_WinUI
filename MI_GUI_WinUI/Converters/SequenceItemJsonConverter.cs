using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace MI_GUI_WinUI.Converters
{
    public class SequenceItemJsonConverter : JsonConverter<Models.SequenceItem>
    {
        public override Models.SequenceItem ReadJson(JsonReader reader, Type objectType, Models.SequenceItem existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var jsonObject = JObject.Load(reader);
            var sequenceItem = new Models.SequenceItem();

            if (jsonObject.TryGetValue("press", out JToken pressToken))
            {
                sequenceItem.Type = "press";
                if (pressToken.Type == JTokenType.Array)
                {
                    sequenceItem.Value = pressToken.First?.ToString() ?? string.Empty;
                }
                else
                {
                    sequenceItem.Value = pressToken.ToString();
                }
            }
            else if (jsonObject.TryGetValue("sleep", out JToken sleepToken))
            {
                sequenceItem.Type = "sleep";
                if (sleepToken.Type == JTokenType.Array)
                {
                    sequenceItem.Value = sleepToken.First?.ToString() ?? "1.0";
                }
                else
                {
                    sequenceItem.Value = sleepToken.ToString();
                }
            }

            return sequenceItem;
        }

        public override void WriteJson(JsonWriter writer, Models.SequenceItem value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            if (value.IsPress)
            {
                writer.WritePropertyName("press");
                writer.WriteStartArray();
                writer.WriteValue(value.Value.ToLowerInvariant());
                writer.WriteEndArray();
            }
            else if (value.IsSleep)
            {
                writer.WritePropertyName("sleep");
                writer.WriteStartArray();
                if (double.TryParse(value.Value, out double sleepValue))
                {
                    writer.WriteValue(sleepValue);
                }
                else
                {
                    writer.WriteValue(1.0); // Default sleep value
                }
                writer.WriteEndArray();
            }

            writer.WriteEndObject();
        }
    }
}

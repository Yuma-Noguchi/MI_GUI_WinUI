using System;
using MI_GUI_WinUI.Models;
using Newtonsoft.Json;

namespace MI_GUI_WinUI.Converters
{
    public class ActionJsonConverter : JsonConverter<ActionData>
    {
        public override void WriteJson(JsonWriter writer, ActionData value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            writer.WriteStartObject();

            // Write basic properties
            writer.WritePropertyName("name");
            writer.WriteValue(value.Name);
            
            writer.WritePropertyName("class");
            writer.WriteValue(value.Class);

            writer.WritePropertyName("method");
            writer.WriteValue(value.Method);

            // Write args array
            writer.WritePropertyName("args");
            serializer.Serialize(writer, value.Args);

            writer.WriteEndObject();
        }

        public override ActionData ReadJson(JsonReader reader, Type objectType, ActionData existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            var action = existingValue ?? new ActionData();

            // Read the entire object and let default serialization handle known properties
            serializer.Populate(reader, action);

            return action;
        }
    }
}

using System;
using System.Collections.ObjectModel;
using System.Linq;
using MI_GUI_WinUI.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MI_GUI_WinUI.Converters
{
    public class ActionJsonConverter : JsonConverter<ActionData>
    {
        public override void WriteJson(JsonWriter writer, ActionData value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            // Write basic properties
            writer.WritePropertyName("name");
            writer.WriteValue(value.Name);
            
            writer.WritePropertyName("id");
            writer.WriteValue(value.Id);

            // Write sequence wrapper
            writer.WritePropertyName("sequence");
            writer.WriteStartObject();
            
            // Write action object
            writer.WritePropertyName("action");
            writer.WriteStartObject();

            writer.WritePropertyName("class");
            writer.WriteValue(value.Class);

            writer.WritePropertyName("method");
            writer.WriteValue(value.Method);

            // Write args array
            writer.WritePropertyName("args");
            writer.WriteStartArray();

            // Write each sequence item
            foreach (var item in value.Sequence)
            {
                serializer.Serialize(writer, item);
            }

            writer.WriteEndArray();
            writer.WriteEndObject(); // End action
            writer.WriteEndObject(); // End sequence
            writer.WriteEndObject(); // End root
        }

        public override ActionData ReadJson(JsonReader reader, Type objectType, ActionData existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var obj = JObject.Load(reader);
            var action = new ActionData
            {
                Name = obj["name"]?.ToString() ?? "",
                Id = obj["id"]?.ToString() ?? Guid.NewGuid().ToString()
            };

            // Read sequence
            var sequence = obj["sequence"]?["action"];
            if (sequence != null)
            {
                action.Class = sequence["class"]?.ToString() ?? "ds4_gamepad";
                action.Method = sequence["method"]?.ToString() ?? "chain";

                var args = sequence["args"] as JArray;
                if (args != null)
                {
                    foreach (var arg in args)
                    {
                        var item = arg.ToObject<SequenceItem>(serializer);
                        if (item != null)
                        {
                            action.Sequence.Add(item);
                        }
                    }
                }
            }

            return action;
        }
    }
}

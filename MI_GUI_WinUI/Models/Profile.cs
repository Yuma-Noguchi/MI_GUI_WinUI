﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Windows.Storage;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
namespace MI_GUI_WinUI.Models;


public struct ActionConfig
{
    [JsonProperty("class")]
    public string ClassName { get; set; }

    [JsonProperty("method")]
    public string MethodName { get; set; }

    [JsonProperty("args")]
    public List<object> Arguments { get; set; }
}

public struct GuiElement
{
    [JsonProperty("file")]
    public string File { get; set; }

    [JsonProperty("pos")]
    public List<int> Position { get; set; }

    [JsonProperty("radius")]
    public int Radius { get; set; }

    [JsonProperty("skin")]
    public string Skin { get; set; }

    [JsonProperty("triggered_skin")]
    public string TriggeredSkin { get; set; }

    [JsonProperty("action")]
    public ActionConfig Action { get; set; }
}

public struct SpeechActionConfig
{
    [JsonProperty("class")]
    public string ClassName { get; set; }

    [JsonProperty("method")]
    public string MethodName { get; set; }

    [JsonProperty("args")]
    public List<object> Arguments { get; set; }
}

public struct SpeechCommand
{
    [Newtonsoft.Json.JsonIgnore]
    public string CommandName { get; set; }

    [JsonProperty("action")]
    public SpeechActionConfig Action { get; set; }
}

public struct Profile
{
    [JsonProperty("config")]
    public required Dictionary<string, string> GlobalConfig { get; set; }

    [JsonProperty("gui")]
    public required List<GuiElement> GuiElements { get; set; }

    [JsonProperty("poses")]
    public required List<PoseGuiElement> Poses { get; set; }

    [JsonProperty("speech")]
    public required Dictionary<string, SpeechCommand> SpeechCommands { get; set; }

    [Newtonsoft.Json.JsonIgnore]
    public string Name { get; set; }
}

﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Windows.Storage;
using Newtonsoft.Json; // Using Newtonsoft.Json
using Newtonsoft.Json.Converters; // Might be needed for custom converters (though probably not in this simple case)


namespace MI_GUI_WinUI.Models;

public struct ActionConfig
{
    [JsonProperty("class")] // Newtonsoft.Json attribute
    public string ClassName { get; set; }

    [JsonProperty("method")] // Newtonsoft.Json attribute
    public string MethodName { get; set; }

    [JsonProperty("args")] // Newtonsoft.Json attribute
    public List<string> Arguments { get; set; }
}

public struct GuiElement
{
    [JsonProperty("file")] // Newtonsoft.Json attribute
    public string File { get; set; }

    [JsonProperty("pos")] // Newtonsoft.Json attribute
    public List<int> Position { get; set; }

    [JsonProperty("radius")] // Newtonsoft.Json attribute
    public int Radius { get; set; }

    [JsonProperty("skin")] // Newtonsoft.Json attribute
    public string Skin { get; set; }

    [JsonProperty("triggered_skin")] // Newtonsoft.Json attribute
    public string TriggeredSkin { get; set; }

    [JsonProperty("action")] // Newtonsoft.Json attribute
    public ActionConfig Action { get; set; }
}

public struct PoseConfig
{
    [JsonProperty("file")] // Newtonsoft.Json attribute
    public string File { get; set; }

    [JsonProperty("x")] // Newtonsoft.Json attribute
    public int X { get; set; }

    [JsonProperty("y")] // Newtonsoft.Json attribute
    public int Y { get; set; }

    [JsonProperty("jitter_correction_strength")] // Newtonsoft.Json attribute
    public int JitterCorrectionStrength { get; set; }

    [JsonProperty("finger")] // Newtonsoft.Json attribute
    public string Finger { get; set; }

    [JsonProperty("pinch_threshold")] // Newtonsoft.Json attribute
    public int PinchThreshold { get; set; }

    [JsonProperty("unpinch_threshold")] // Newtonsoft.Json attribute
    public int UnpinchThreshold { get; set; }

    [JsonProperty("action")] // Newtonsoft.Json attribute
    public ActionConfig Action { get; set; } // Note: Action can be null if not present
}

public struct SpeechActionConfig
{
    [JsonProperty("class")] // Newtonsoft.Json attribute
    public string ClassName { get; set; }

    [JsonProperty("method")] // Newtonsoft.Json attribute
    public string MethodName { get; set; }

    [JsonProperty("args")] // Newtonsoft.Json attribute
    public List<object> Arguments { get; set; } // Using object for flexibility as args can be boolean
}

public struct SpeechCommand
{
    [Newtonsoft.Json.JsonIgnore] // We'll handle the key separately - still using JsonIgnore from Newtonsoft.Json is fine
    public string CommandName { get; set; }

    [JsonProperty("action")] // Newtonsoft.Json attribute
    public SpeechActionConfig Action { get; set; }
}

public struct Profile
{
    [JsonProperty("config")] // Newtonsoft.Json attribute
    public required Dictionary<string, string> GlobalConfig { get; set; } // For the "config" section

    [JsonProperty("gui")] // Newtonsoft.Json attribute
    public required List<GuiElement> GuiElements { get; set; }

    [JsonProperty("poses")] // Newtonsoft.Json attribute
    public required List<PoseConfig> Poses { get; set; }

    [JsonProperty("speech")] // Newtonsoft.Json attribute
    public required Dictionary<string, SpeechCommand> SpeechCommands { get; set; } // Dictionary for speech commands

    [Newtonsoft.Json.JsonIgnore]
    public string Name { get; set; }
}

// public class JsonReaderNewtonsoft
// {
    

    // public static void Main(string[] args)
    // {
    //     string filePath = "config.json"; // Replace with the actual path to your JSON file

    //     if (!File.Exists(filePath))
    //     {
    //         Console.WriteLine($"Error: JSON file not found at path: {filePath}");
    //         return;
    //     }

    //     Config mainConfig = ReadConfigFromJsonFileNewtonsoft(filePath);

    //     Console.WriteLine("Config Data (using Newtonsoft.Json):");
    //     Console.WriteLine("------------------");

    //     Console.WriteLine("\nGlobal Config:");
    //     if (mainConfig.GlobalConfig != null)
    //     {
    //         foreach (var kvp in mainConfig.GlobalConfig)
    //         {
    //             Console.WriteLine($"  {kvp.Key}: {kvp.Value}");
    //         }
    //     }

    //     Console.WriteLine("\nGUI Elements:");
    //     if (mainConfig.GuiElements != null)
    //     {
    //         foreach (var guiElement in mainConfig.GuiElements)
    //         {
    //             Console.WriteLine($"  File: {guiElement.File}");
    //             Console.WriteLine($"  Position: [{string.Join(", ", guiElement.Position)}]");
    //             Console.WriteLine($"  Radius: {guiElement.Radius}");
    //             Console.WriteLine($"  Skin: {guiElement.Skin}");
    //             Console.WriteLine($"  Triggered Skin: {guiElement.TriggeredSkin}");
    //             Console.WriteLine($"  Action Class: {guiElement.Action.ClassName}");
    //             Console.WriteLine($"  Action Method: {guiElement.Action.MethodName}");
    //             Console.WriteLine($"  Action Args: [{string.Join(", ", guiElement.Action.Arguments)}]");
    //             Console.WriteLine("  ------------------");
    //         }
    //     }

    //     Console.WriteLine("\nPose Configs:");
    //     if (mainConfig.Poses != null)
    //     {
    //         foreach (var poseConfig in mainConfig.Poses)
    //         {
    //             Console.WriteLine($"  File: {poseConfig.File}");
    //             Console.WriteLine($"  X: {poseConfig.X}, Y: {poseConfig.Y}");
    //             Console.WriteLine($"  Jitter Correction Strength: {poseConfig.JitterCorrectionStrength}");
    //             Console.WriteLine($"  Finger: {poseConfig.Finger}");
    //             Console.WriteLine($"  Pinch Threshold: {poseConfig.PinchThreshold}");
    //             Console.WriteLine($"  Unpinch Threshold: {poseConfig.UnpinchThreshold}");
    //             if (poseConfig.Action.ClassName != null) // Check if Action is present
    //             {
    //                 Console.WriteLine($"  Action Class: {poseConfig.Action.ClassName}");
    //                 Console.WriteLine($"  Action Method: {poseConfig.Action.MethodName}");
    //                 Console.WriteLine($"  Action Args: [{string.Join(", ", poseConfig.Action.Arguments)}]");
    //             }
    //             Console.WriteLine("  ------------------");
    //         }
    //     }

    //     Console.WriteLine("\nSpeech Commands:");
    //     if (mainConfig.SpeechCommands != null)
    //     {
    //         foreach (var kvp in mainConfig.SpeechCommands)
    //         {
    //             SpeechCommand speechCommand = kvp.Value;
    //             Console.WriteLine($"  Command: {kvp.Key}");
    //             Console.WriteLine($"  Action Class: {speechCommand.Action.ClassName}");
    //             Console.WriteLine($"  Action Method: {speechCommand.Action.MethodName}");
    //             Console.WriteLine($"  Action Args: [{string.Join(", ", speechCommand.Action.Arguments)}]");
    //             Console.WriteLine("  ------------------");
    //         }
    //     }
    // }
// }
public class ProfileService
{
    public List<Profile> ReadProfileFromJson(string folderPath)
    {
        string fullPath = Path.Combine(Windows.ApplicationModel.Package.Current.InstalledLocation.Path, folderPath);
        List<Profile> profiles = new List<Profile>();

        if (!Directory.Exists(fullPath))
        {
            throw new DirectoryNotFoundException($"Directory not found at path: {fullPath}");
        }

        foreach (var file in Directory.EnumerateFiles(fullPath))
        {
            // Read the file
            string jsonString = File.ReadAllText(file);

            // Deserialize the JSON string to a Profile object
            Profile profile = JsonConvert.DeserializeObject<Profile>(jsonString);
            
            // Set the display name from the filename
            profile.Name = ProfileNameHelper.GetDisplayNameFromFileName(file);

            profiles.Add(profile);
        }

        return profiles;
    }

    public void SaveProfilesToJson(List<Profile> profiles, string folderPath)
    {
        string fullPath = Path.Combine(Windows.ApplicationModel.Package.Current.InstalledLocation.Path, folderPath);
        
        if (!Directory.Exists(fullPath))
        {
            Directory.CreateDirectory(fullPath);
        }

        foreach (var profile in profiles)
        {
            string fileName = ProfileNameHelper.GetFileNameFromDisplayName(profile.Name);
            string filePath = Path.Combine(fullPath, $"{fileName}.json");
            WriteConfigToJsonFile(profile, filePath);
        }
    }

    private static void WriteConfigToJsonFile(Profile profile, string filePath)
    {
        //if (config == null)
        //{
        //    throw new ArgumentNullException(nameof(config), "Config object cannot be null.");
        //}

        if (string.IsNullOrEmpty(filePath))
        {
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
        }

        try
        {
            // Serialize the Config object to a JSON string
            string jsonString = JsonConvert.SerializeObject(profile, Formatting.Indented); // Formatting.Indented for pretty JSON

            // Write the JSON string to the file
            File.WriteAllText(filePath, jsonString);
        }
        catch (JsonException ex)
        {
            throw new JsonException($"Error serializing Config object to JSON. {ex.Message}", ex);
        }
        catch (IOException ioEx)
        {
            throw new IOException($"Error writing JSON to file: {filePath}. {ioEx.Message}", ioEx);
        }
    }
}

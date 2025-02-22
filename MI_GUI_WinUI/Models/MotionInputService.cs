using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json; // Using Newtonsoft.Json
using Newtonsoft.Json.Linq; // Using Newtonsoft.Json.Linq

namespace MI_GUI_WinUI.Models;


public class MotionInputService
{

    private Process? MotionInput;
    private string configFilePath = Path.Combine(Windows.ApplicationModel.Package.Current.InstalledLocation.Path, "MotionInput\\data\\config.json");
    public static string ReadModeFromJsonFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"JSON file not found at path: {filePath}");
        }

        string jsonString = File.ReadAllText(filePath);

        try
        {
            JObject jsonObject = JObject.Parse(jsonString); // Parse JSON into a dynamic JObject
            string modeValue = (string)jsonObject["mode"]; // Access "mode" property as string

            return modeValue;
        }
        catch (JsonException ex)
        {
            throw new JsonException($"Error parsing JSON from file: {filePath}. {ex.Message}", ex);
        }
        catch (InvalidCastException castEx)
        {
            throw new InvalidCastException($"Error casting 'mode' value to string from JSON file: {filePath}.  Ensure 'mode' is a string. {castEx.Message}", castEx);
        }
        catch (NullReferenceException nullRefEx)
        {
            throw new NullReferenceException($"'mode' property not found in JSON file: {filePath}. {nullRefEx.Message}", nullRefEx);
        }
    }

    public static void WriteModeToJsonFile(string filePath, string modeValue)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
        }
        if (modeValue == null) // Mode value can be empty string, but not null in this context
        {
            throw new ArgumentNullException(nameof(modeValue), "Mode value cannot be null.");
        }

        try
        {
            JObject jsonObject;

            // Try to read existing JSON file, if it exists. If not, create a new empty JObject.
            if (File.Exists(filePath))
            {
                string existingJsonString = File.ReadAllText(filePath);
                jsonObject = JObject.Parse(existingJsonString);
            }
            else
            {
                jsonObject = new JObject(); // Create a new empty JSON object
            }

            // Update or add the "mode" property
            jsonObject["mode"] = modeValue;

            // Serialize the JObject back to a JSON string (with indentation for readability)
            string jsonString = JsonConvert.SerializeObject(jsonObject, Formatting.Indented);

            // Write the JSON string to the file
            File.WriteAllText(filePath, jsonString);
        }
        catch (JsonException ex)
        {
            throw new JsonException($"Error serializing JSON to file: {filePath}. {ex.Message}", ex);
        }
        catch (IOException ioEx)
        {
            throw new IOException($"Error writing to file: {filePath}. {ioEx.Message}", ioEx);
        }
    }

    public async Task<bool> Start(string profileName)
    {
        WriteModeToJsonFile(configFilePath, profileName);
        return await Launch();
    }

    public async Task<bool> ChangeMode(string mode)
    {
        try
        {
            // Read the JSON file
            string configJson = File.ReadAllText(configFilePath);

            // Parse the JSON file
            JObject configJsonObj = JObject.Parse(configJson);

            // Modify the specific key-value pairs
            configJsonObj["mode"] = mode;

            // Write the modified JSON back to the file
            File.WriteAllText(configFilePath, configJsonObj.ToString());

            return await Launch();
        }
        catch (Exception e)
        {
            return false;
        }
    }

    public async Task<bool> Launch()
    {
        try
        {
            // Path to the executable
            string FilePath = Path.Combine(Windows.ApplicationModel.Package.Current.InstalledLocation.Path, "MotionInput\\MotionInput.exe");

            if (MotionInput != null)
            {
                MotionInput.Kill();
            }
            MotionInput = new();
            MotionInput.StartInfo.UseShellExecute = true;
            MotionInput.StartInfo.FileName = FilePath;
            MotionInput.StartInfo.WorkingDirectory = Path.GetDirectoryName(FilePath);
            MotionInput.Start();
        }
        catch (Exception e)
        {
            return false;
        }
        return true;
    }
}
﻿﻿﻿﻿﻿using System;
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

// [Previous struct definitions remained unchanged...]
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

public class ProfileService
{
    private readonly Dictionary<string, Profile> _profileCache;
    private readonly string _baseProfilePath;
    private readonly ILogger<ProfileService> _logger;
    private readonly JsonSerializerSettings _jsonSettings;
    
    public ProfileService(ILogger<ProfileService> logger)
    {
        _logger = logger;
        _profileCache = new Dictionary<string, Profile>();
        _baseProfilePath = Windows.ApplicationModel.Package.Current.InstalledLocation.Path;
        _jsonSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            Error = (sender, args) =>
            {
                _logger.LogError($"JSON Error: {args.ErrorContext.Error.Message}");
                args.ErrorContext.Handled = true;
            }
        };
    }

    public async Task DeleteProfileAsync(string profileName, string folderPath)
    {
        try
        {
            string fullPath = Path.Combine(_baseProfilePath, folderPath);
            string fileName = ProfileNameHelper.GetFileNameFromDisplayName(profileName);
            string filePath = Path.Combine(fullPath, $"{fileName}.json");
            
            _logger.LogInformation($"Attempting to delete profile file: {filePath}");

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _profileCache.Remove(profileName);
                _logger.LogInformation($"Successfully deleted profile file: {filePath}");
            }
            else
            {
                _logger.LogWarning($"Profile file not found for deletion: {filePath}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting profile file for {profileName}");
            throw;
        }
    }

    public async Task<List<Profile>> ReadProfilesFromJsonAsync(string folderPath)
    {
        string fullPath = Path.Combine(_baseProfilePath, folderPath);
        List<Profile> profiles = new List<Profile>();

        if (!Directory.Exists(fullPath))
        {
            _logger.LogError($"Directory not found: {fullPath}");
            throw new DirectoryNotFoundException($"Directory not found at path: {fullPath}");
        }

        try
        {
            foreach (var file in Directory.EnumerateFiles(fullPath, "*.json"))
            {
                if (await LoadProfileFromFileAsync(file) is Profile profile)
                {
                    profiles.Add(profile);
                }
            }

            // Update cache
            _profileCache.Clear();
            foreach (var profile in profiles)
            {
                _profileCache[profile.Name] = profile;
            }

            return profiles;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error reading profiles from {folderPath}");
            throw;
        }
    }

    private async Task<Profile?> LoadProfileFromFileAsync(string filePath)
    {
        try
        {
            // Check cache first
            string profileName = ProfileNameHelper.GetDisplayNameFromFileName(filePath);
            if (_profileCache.TryGetValue(profileName, out Profile cachedProfile))
            {
                return cachedProfile;
            }

            // Read and parse file
            string jsonString = await File.ReadAllTextAsync(filePath);
            var profile = JsonConvert.DeserializeObject<Profile>(jsonString, _jsonSettings);

            profile.Name = profileName;
            return profile;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, $"JSON parsing error in {filePath}");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error loading profile from {filePath}");
            return null;
        }
    }

    public async Task SaveProfilesToJsonAsync(List<Profile> profiles, string folderPath)
    {
        string fullPath = Path.Combine(_baseProfilePath, folderPath);
        
        try
        {
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
            }

            foreach (var profile in profiles)
            {
                await SaveProfileToFileAsync(profile, fullPath);
            }

            // Update cache
            _profileCache.Clear();
            foreach (var profile in profiles)
            {
                _profileCache[profile.Name] = profile;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error saving profiles to {folderPath}");
            throw;
        }
    }

    private async Task SaveProfileToFileAsync(Profile profile, string folderPath)
    {
        if (string.IsNullOrEmpty(profile.Name))
        {
            throw new ArgumentException("Profile name cannot be empty", nameof(profile));
        }

        string fileName = ProfileNameHelper.GetFileNameFromDisplayName(profile.Name);
        string filePath = Path.Combine(folderPath, $"{fileName}.json");

        try
        {
            string jsonString = JsonConvert.SerializeObject(profile, _jsonSettings);
            await File.WriteAllTextAsync(filePath, jsonString);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error saving profile to {filePath}");
            throw;
        }
    }

    public Profile? GetProfileFromCache(string profileName)
    {
        _profileCache.TryGetValue(profileName, out Profile profile);
        return profile;
    }

    public void ClearCache()
    {
        _profileCache.Clear();
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MI_GUI_WinUI.Models;
using MI_GUI_WinUI.Services.Interfaces;
using Newtonsoft.Json;

namespace MI_GUI_WinUI.Services
{
    public class ProfileService : IProfileService
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

        public async Task SaveProfileAsync(Profile profile, string folderPath)
        {
            try
            {
                string fullPath = Path.Combine(_baseProfilePath, folderPath);
                
                if (!Directory.Exists(fullPath))
                {
                    Directory.CreateDirectory(fullPath);
                }

                await SaveProfileToFileAsync(profile, fullPath);
                
                // Update cache
                _profileCache[profile.Name] = profile;
                
                _logger.LogInformation($"Successfully saved profile: {profile.Name}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error saving profile {profile.Name}");
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

    internal static class ProfileNameHelper
    {
        public static string GetFileNameFromDisplayName(string displayName)
        {
            return displayName.Replace(" ", "_").ToLower();
        }

        public static string GetDisplayNameFromFileName(string filePath)
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            return fileName.Replace("_", " ");
        }
    }
}
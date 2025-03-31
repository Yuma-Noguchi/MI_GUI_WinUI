using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Logging;
using MI_GUI_WinUI.Models;
using MI_GUI_WinUI.Services.Interfaces;
using Newtonsoft.Json;
using Windows.Storage;

namespace MI_GUI_WinUI.Services
{
    public class ProfileService : IProfileService
    {
        private readonly Dictionary<string, Profile> _profileCache;
        private readonly ILogger<ProfileService> _logger;
        private readonly JsonSerializerSettings _jsonSettings;
        
        public ProfileService(ILogger<ProfileService> logger)
        {
            _logger = logger;
            _profileCache = new Dictionary<string, Profile>();
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
                StorageFolder installedLocation = Windows.ApplicationModel.Package.Current.InstalledLocation;
                StorageFolder profilesFolder = await installedLocation.GetFolderAsync(folderPath);
                string fileName = ProfileNameHelper.GetFileNameFromDisplayName(profileName);
                
                _logger.LogInformation($"Attempting to delete profile file: {fileName}.json");

                try
                {
                    StorageFile file = await profilesFolder.GetFileAsync($"{fileName}.json");
                    await file.DeleteAsync();
                    _profileCache.Remove(profileName);
                    _logger.LogInformation($"Successfully deleted profile file: {fileName}.json");
                }
                catch (FileNotFoundException)
                {
                    _logger.LogWarning($"Profile file not found for deletion: {fileName}.json");
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
            List<Profile> profiles = new List<Profile>();
            try
            {
                StorageFolder installedLocation = Windows.ApplicationModel.Package.Current.InstalledLocation;
                StorageFolder profilesFolder = await installedLocation.GetFolderAsync(folderPath);

                var files = await profilesFolder.GetFilesAsync();
                foreach (var file in files.Where(f => f.FileType == ".json"))
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

                _logger.LogInformation($"Successfully loaded {profiles.Count} profiles from {folderPath}");
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
                StorageFolder installedLocation = Windows.ApplicationModel.Package.Current.InstalledLocation;
                
                // Ensure the folder exists
                StorageFolder profilesFolder;
                try
                {
                    profilesFolder = await installedLocation.GetFolderAsync(folderPath);
                }
                catch (FileNotFoundException)
                {
                    string[] pathParts = folderPath.Split('/', '\\').Where(p => !string.IsNullOrEmpty(p)).ToArray();
                    profilesFolder = installedLocation;
                    foreach (string part in pathParts)
                    {
                        StorageFolder nextFolder;
                        try
                        {
                            nextFolder = await profilesFolder.GetFolderAsync(part);
                        }
                        catch (FileNotFoundException)
                        {
                            nextFolder = await profilesFolder.CreateFolderAsync(part);
                        }
                        profilesFolder = nextFolder;
                    }
                }

                await SaveProfileToFileAsync(profile, profilesFolder);
                
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

        private async Task<Profile?> LoadProfileFromFileAsync(StorageFile file)
        {
            try
            {
                // Check cache first
                string profileName = ProfileNameHelper.GetDisplayNameFromFileName(file.Name);
                if (_profileCache.TryGetValue(profileName, out Profile cachedProfile))
                {
                    return cachedProfile;
                }

                // Read and parse file
                string jsonString = await FileIO.ReadTextAsync(file);
                var profile = JsonConvert.DeserializeObject<Profile>(jsonString, _jsonSettings);

                profile.Name = profileName;
                return profile;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, $"JSON parsing error in {file.Name}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading profile from {file.Name}");
                return null;
            }
        }

        private async Task SaveProfileToFileAsync(Profile profile, StorageFolder profilesFolder)
        {
            if (string.IsNullOrEmpty(profile.Name))
            {
                throw new ArgumentException("Profile name cannot be empty", nameof(profile));
            }

            try
            {
                string fileName = ProfileNameHelper.GetFileNameFromDisplayName(profile.Name);
                StorageFile file = await profilesFolder.CreateFileAsync($"{fileName}.json", CreationCollisionOption.ReplaceExisting);
                string jsonString = JsonConvert.SerializeObject(profile, _jsonSettings);
                
                await FileIO.WriteTextAsync(file, jsonString);
                _logger.LogInformation($"Successfully saved profile to {fileName}.json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error saving profile {profile.Name}");
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
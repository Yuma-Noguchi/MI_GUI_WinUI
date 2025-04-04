using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MI_GUI_WinUI.Services;
using MI_GUI_WinUI.Services.Interfaces;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MI_GUI_WinUI.Models;
using Newtonsoft.Json;

namespace MI_GUI_WinUI.Tests.TestUtils.TestServices
{
    // A testable version of ProfileService that doesn't rely on Windows.ApplicationModel.Package.Current
    public class TestableProfileService : IProfileService
    {
        private readonly Dictionary<string, Profile> _profileCache = new Dictionary<string, Profile>();
        private readonly ILogger<ProfileService> _logger;

        public TestableProfileService(ILogger<ProfileService> logger)
        {
            _logger = logger;
        }

        public async Task<List<Profile>> ReadProfilesFromJsonAsync(string folderPath)
        {
            List<Profile> profiles = new List<Profile>();
            try
            {
                if (!Directory.Exists(folderPath))
                {
                    _logger.LogError($"Directory not found: {folderPath}");
                    throw new Exception($"Directory not found: {folderPath}", new DirectoryNotFoundException(folderPath));
                }

                var files = Directory.GetFiles(folderPath, "*.json");
                foreach (var file in files)
                {
                    try
                    {
                        string json = await File.ReadAllTextAsync(file);
                        var profile = JsonConvert.DeserializeObject<Profile>(json);
                        
                        // Set the profile name based on the filename
                        string fileName = Path.GetFileNameWithoutExtension(file);
                        profile.Name = fileName.Replace("_", " ");
                        
                        profiles.Add(profile);
                        
                        // Add to cache
                        _profileCache[profile.Name] = profile;
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogError(ex, $"JSON parsing error in {file}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error loading profile from {file}");
                    }
                }

                _logger.LogInformation($"Successfully loaded {profiles.Count} profiles from {folderPath}");
                return profiles;
            }
            catch (DirectoryNotFoundException ex)
            {
                _logger.LogError(ex, $"Error reading profiles from {folderPath}");
                throw new Exception($"Failed to read profiles from {folderPath}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error reading profiles from {folderPath}");
                throw new Exception($"Failed to read profiles from {folderPath}", ex);
            }
        }

        public async Task SaveProfileAsync(Profile profile, string folderPath)
        {

            if (string.IsNullOrEmpty(profile.Name))
            {
                var ex = new ArgumentException("Profile name cannot be empty", nameof(profile));
                _logger.LogError(ex, "Profile name cannot be empty");
                throw ex;
            }

            try
            {
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                string fileName = profile.Name.Replace(" ", "_").ToLower();
                string filePath = Path.Combine(folderPath, $"{fileName}.json");
                
                string json = JsonConvert.SerializeObject(profile);
                await File.WriteAllTextAsync(filePath, json);
                
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

        public async Task DeleteProfileAsync(string profileName, string folderPath)
        {
            if (string.IsNullOrEmpty(profileName))
            {
                var ex = new ArgumentException("Profile name cannot be empty", nameof(profileName));
                _logger.LogError(ex, "Profile name cannot be empty");
                throw ex;
            }

            try
            {
                string fileName = profileName.Replace(" ", "_").ToLower();
                string filePath = Path.Combine(folderPath, $"{fileName}.json");
                
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    _profileCache.Remove(profileName);
                    _logger.LogInformation($"Successfully deleted profile file: {fileName}.json");
                }
                else
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

        public Profile? GetProfileFromCache(string profileName)
        {
            if (_profileCache.TryGetValue(profileName, out Profile profile))
            {
                return profile;
            }
            return null;
        }

        public void ClearCache()
        {
            _profileCache.Clear();
        }
    }
}
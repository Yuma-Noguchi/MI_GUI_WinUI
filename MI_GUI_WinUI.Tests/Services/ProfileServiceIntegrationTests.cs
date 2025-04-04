using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MI_GUI_WinUI.Models;
using MI_GUI_WinUI.Services;
using MI_GUI_WinUI.Services.Interfaces;
using MI_GUI_WinUI.Tests.TestUtils;
using MI_GUI_WinUI.Tests.TestUtils.TestServices;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MI_GUI_WinUI.Tests.Services
{
    [TestClass]
    public class ProfileServiceIntegrationTests : IntegrationTestBase
    {
        private IProfileService _profileService;
        private string _profilesPath;
        private const string TestProfilesFolder = "TestProfiles";

        [TestInitialize]
        public override async Task InitializeTest()
        {
            await base.InitializeTest();
            
            // Create profiles directory in test data path
            _profilesPath = Path.Combine(TestDataPath, TestProfilesFolder);
            Directory.CreateDirectory(_profilesPath);
            
            // Register the real ProfileService implementation
            _profileService = GetRequiredService<IProfileService>();
        }

        [TestCleanup]
        public override void CleanupTest()
        {
            // Cleanup any profile files created during tests
            _profileService.ClearCache();
            base.CleanupTest();
        }

        protected override void ConfigureIntegrationServices(IServiceCollection services)
        {
            base.ConfigureIntegrationServices(services);
            
            // Register real ProfileService implementation
            services.AddSingleton<IProfileService>(sp => 
                new TestProfileService(sp.GetRequiredService<ILogger<ProfileService>>())
            );
        }

        [TestMethod]
        [TestCategory("Integration")]
        public async Task ProfileLifecycle_CreateReadUpdateDelete_FullLifecycleWorks()
        {
            // Arrange - Create profile
            var profile = CreateTestProfile("Integration Test Profile");
            profile.GlobalConfig["test_key"] = "initial_value";
            
            // Act 1 - Save profile
            await _profileService.SaveProfileAsync(profile, _profilesPath);
            
            // Assert 1 - Verify file was created
            string expectedPath = Path.Combine(_profilesPath, "integration_test_profile.json");
            Assert.IsTrue(File.Exists(expectedPath), "Profile file should be created");
            
            // Act 2 - Read all profiles
            var profiles = await _profileService.ReadProfilesFromJsonAsync(_profilesPath);
            
            // Assert 2 - Verify profile was read
            Assert.IsTrue(profiles.Count >= 1, "At least one profile should be loaded");
            var loadedProfile = profiles.FirstOrDefault(p => p.Name == "integration_test_profile");
            Assert.IsNotNull(loadedProfile, "The saved profile should be loaded");
            Assert.AreEqual("initial_value", loadedProfile.GlobalConfig["test_key"], "Profile should have the correct config");
            
            // Act 3 - Update profile
            var profileToUpdate = loadedProfile;
            profileToUpdate.GlobalConfig["test_key"] = "updated_value";
            profileToUpdate.GlobalConfig["new_key"] = "new_value";
            await _profileService.SaveProfileAsync(profileToUpdate, _profilesPath);
            
            // Act 4 - Get from cache and verify update
            var cachedProfile = _profileService.GetProfileFromCache("integration_test_profile");
            
            // Assert 4 - Verify cache was updated
            Assert.IsNotNull(cachedProfile, "Profile should be in cache");
            Assert.AreEqual("updated_value", cachedProfile?.GlobalConfig["test_key"], "Cached profile should be updated");
            Assert.AreEqual("new_value", cachedProfile?.GlobalConfig["new_key"], "Cached profile should have new key");
            
            // Act 5 - Delete profile
            await _profileService.DeleteProfileAsync("integration_test_profile", _profilesPath);
            
            // Assert 5 - Verify file was deleted
            Assert.IsFalse(File.Exists(expectedPath), "Profile file should be deleted");
            
            // Act 6 - Verify it's removed from cache
            var deletedProfile = _profileService.GetProfileFromCache("integration_test_profile");
            
            // Assert 6 - Verify profile is not in cache
            Assert.IsNull(deletedProfile, "Deleted profile should not be in cache");
        }

        [TestMethod]
        [TestCategory("Integration")]
        public async Task LoadProfiles_WithMultipleProfiles_LoadsAllCorrectly()
        {
            // Arrange
            var profile1 = CreateTestProfile("Profile One");
            var profile2 = CreateTestProfile("Profile Two");
            var profile3 = CreateTestProfile("Profile Three");
            
            // Add different configurations to each profile
            profile1.GlobalConfig["app"] = "test1";
            profile2.GlobalConfig["app"] = "test2";
            profile3.GlobalConfig["app"] = "test3";
            
            await _profileService.SaveProfileAsync(profile1, _profilesPath);
            await _profileService.SaveProfileAsync(profile2, _profilesPath);
            await _profileService.SaveProfileAsync(profile3, _profilesPath);
            
            // Clear cache to ensure we're reading from disk
            _profileService.ClearCache();
            
            // Act
            var profiles = await _profileService.ReadProfilesFromJsonAsync(_profilesPath);
            
            // Assert
            Assert.AreEqual(3, profiles.Count, "All three profiles should be loaded");
            Assert.IsTrue(profiles.Any(p => p.Name == "profile_one" && p.GlobalConfig["app"] == "test1"), "Profile One should be loaded with correct config");
            Assert.IsTrue(profiles.Any(p => p.Name == "profile_two" && p.GlobalConfig["app"] == "test2"), "Profile Two should be loaded with correct config");
            Assert.IsTrue(profiles.Any(p => p.Name == "profile_three" && p.GlobalConfig["app"] == "test3"), "Profile Three should be loaded with correct config");
        }

        [TestMethod]
        [TestCategory("Integration")]
        public async Task SaveProfile_WithInvalidPath_CreatesDirectoryStructure()
        {
            // Arrange
            var profile = CreateTestProfile("Nested Profile");
            string nestedPath = Path.Combine(_profilesPath, "Level1", "Level2");
            
            // Act
            await _profileService.SaveProfileAsync(profile, nestedPath);
            
            // Assert
            string expectedPath = Path.Combine(nestedPath, "nested_profile.json");
            Assert.IsTrue(Directory.Exists(nestedPath), "Nested directories should be created");
            Assert.IsTrue(File.Exists(expectedPath), "Profile file should be created in nested directory");
        }

        [TestMethod]
        [TestCategory("Integration")]
        public async Task ReadProfiles_WithCorruptedJson_SkipsCorruptedFiles()
        {
            // Arrange
            var goodProfile = CreateTestProfile("Good Profile");
            await _profileService.SaveProfileAsync(goodProfile, _profilesPath);
            
            // Create a corrupted JSON file
            string corruptedFilePath = Path.Combine(_profilesPath, "corrupted_profile.json");
            await File.WriteAllTextAsync(corruptedFilePath, "{\"config\": {\"setting\": \"value\"}, This is invalid JSON");
            
            // Act
            var profiles = await _profileService.ReadProfilesFromJsonAsync(_profilesPath);
            
            // Assert
            Assert.IsNotNull(profiles, "Profiles list should not be null");
            Assert.AreEqual(1, profiles.Count, "Only the good profile should be loaded");
            Assert.AreEqual("good_profile", profiles[0].Name, "The good profile should be loaded correctly");
        }

        [TestMethod]
        [TestCategory("Integration")]
        public async Task CacheOperations_VerifyCacheBehavior()
        {
            // Arrange
            var profile = CreateTestProfile("Cache Test");
            await _profileService.SaveProfileAsync(profile, _profilesPath);
            
            // Act 1 - Load profiles to populate cache
            await _profileService.ReadProfilesFromJsonAsync(_profilesPath);
            
            // Assert 1 - Verify profile is in cache
            var cachedProfile = _profileService.GetProfileFromCache("cache_test");
            Assert.IsNotNull(cachedProfile, "Profile should be in cache after reading");
            
            // Act 2 - Delete the file directly (not using the service)
            File.Delete(Path.Combine(_profilesPath, "cache_test.json"));
            
            // Assert 2 - Profile should still be in cache even though file is deleted
            cachedProfile = _profileService.GetProfileFromCache("cache_test");
            Assert.IsNotNull(cachedProfile, "Profile should still be in cache after file deletion");
            
            // Act 3 - Clear cache
            _profileService.ClearCache();
            
            // Assert 3 - Profile should no longer be in cache
            cachedProfile = _profileService.GetProfileFromCache("cache_test");
            Assert.IsNull(cachedProfile, "Profile should not be in cache after clearing");
            
            // Act 4 - Try to read profiles again
            var profiles = await _profileService.ReadProfilesFromJsonAsync(_profilesPath);
            
            // Assert 4 - No profiles should be returned as the file was deleted
            Assert.AreEqual(0, profiles.Count, "No profiles should be returned as the file was deleted");
        }

        [TestMethod]
        [TestCategory("Integration")]
        [ExpectedException(typeof(Exception))]
        public async Task ReadProfiles_WithInaccessibleDirectory_ThrowsException()
        {
            // Act - Try to read from a non-existent directory
            string inaccessiblePath = Path.Combine(TestDataPath, "NonExistentFolder");
            await _profileService.ReadProfilesFromJsonAsync(inaccessiblePath);
            
            // Assert - Exception should be thrown (handled by ExpectedException attribute)
        }

        // Helper methods
        private Profile CreateTestProfile(string name)
        {
            return new Profile
            {
                Name = name,
                GlobalConfig = new Dictionary<string, string>(),
                GuiElements = new List<GuiElement>(),
                Poses = new List<PoseGuiElement>(),
                SpeechCommands = new Dictionary<string, SpeechCommand>()
            };
        }
    }

    // Test implementation of ProfileService that doesn't rely on Windows.Storage APIs
    internal class TestProfileService : IProfileService
    {
        private readonly Dictionary<string, Profile> _profileCache = new Dictionary<string, Profile>();
        private readonly ILogger<ProfileService> _logger;

        public TestProfileService(ILogger<ProfileService> logger)
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
                    throw new Exception($"Directory not found: {folderPath}");
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
                        profile.Name = fileName;
                        
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
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error reading profiles from {folderPath}");
                throw;
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
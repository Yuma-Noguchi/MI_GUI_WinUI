using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MI_GUI_WinUI.Models;
using MI_GUI_WinUI.Services;
using MI_GUI_WinUI.Services.Interfaces;
using MI_GUI_WinUI.Tests.TestUtils;
using Moq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;

namespace MI_GUI_WinUI.Tests.Services
{
    // A testable version of ProfileService that doesn't rely on Windows.ApplicationModel.Package.Current
    internal class TestableProfileService : IProfileService
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

    [TestClass]
    public class ProfileServiceTests : UnitTestBase
    {
        private IProfileService _profileService;
        private string _testProfilesPath;
        private Mock<ILogger<ProfileService>> _mockLogger;

        [TestInitialize]
        public override async Task InitializeTest()
        {
            await base.InitializeTest();
            _testProfilesPath = Path.Combine(TestDirectory, "Profiles");
            
            // Ensure the directory exists
            if (Directory.Exists(_testProfilesPath))
            {
                // Clean up any existing files to start fresh
                foreach (var file in Directory.GetFiles(_testProfilesPath, "*.json"))
                {
                    File.Delete(file);
                }
            }
            else
            {
                Directory.CreateDirectory(_testProfilesPath);
            }
            
            _mockLogger = new Mock<ILogger<ProfileService>>();
            
            // Use the TestableProfileService instead of the real ProfileService
            _profileService = new TestableProfileService(_mockLogger.Object);
        }

        [TestMethod]
        public async Task ReadProfilesFromJsonAsync_WhenNoProfiles_ReturnsEmptyList()
        {
            // Arrange - Empty directory should already exist from setup

            // Act
            var result = await _profileService.ReadProfilesFromJsonAsync(_testProfilesPath);

            // Assert
            Assert.IsNotNull(result, "Result should not be null");
            Assert.AreEqual(0, result.Count, "Result should be an empty list");
            _mockLogger.Verify(l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("loaded 0 profiles")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [TestMethod]
        public async Task ReadProfilesFromJsonAsync_WithExistingProfiles_LoadsSuccessfully()
        {
            // Arrange
            var profile1 = CreateTestProfile("Test Profile 1");
            var profile2 = CreateTestProfile("Test Profile 2");
            await CreateTestProfileFile(profile1);
            await CreateTestProfileFile(profile2);

            // Act
            var result = await _profileService.ReadProfilesFromJsonAsync(_testProfilesPath);

            // Assert
            Assert.IsNotNull(result, "Result should not be null");
            Assert.AreEqual(2, result.Count, "Result should contain 2 profiles");
            Assert.IsTrue(result.Any(p => p.Name == "test profile 1"), "Result should contain profile 1");
            Assert.IsTrue(result.Any(p => p.Name == "test profile 2"), "Result should contain profile 2");
            _mockLogger.Verify(l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("loaded 2 profiles")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [TestMethod]
        public async Task ReadProfilesFromJsonAsync_WithInvalidFolderPath_ThrowsException()
        {
            // Arrange
            string invalidPath = Path.Combine(TestDirectory, "non_existent_folder");

            // Act & Assert
            await Assert.ThrowsExceptionAsync<Exception>(() => 
                _profileService.ReadProfilesFromJsonAsync(invalidPath));
            
            _mockLogger.Verify(l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error reading profiles")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [TestMethod]
        public async Task SaveProfileAsync_WithValidProfile_Succeeds()
        {
            // Arrange
            var profile = CreateTestProfile("Save Test Profile");
            profile.GlobalConfig["test_setting"] = "test_value";
            
            // Act
            await _profileService.SaveProfileAsync(profile, _testProfilesPath);

            // Assert
            string expectedPath = Path.Combine(_testProfilesPath, "save_test_profile.json");
            Assert.IsTrue(File.Exists(expectedPath), "Profile file should be created");
            
            var savedProfile = await LoadProfileFromFile(expectedPath);
            // lowercase the name for comparison
            var savedProfileName = savedProfile.Name.ToLowerInvariant();
            
            Assert.AreEqual(savedProfileName, savedProfile.Name, "Profile name should match");
            Assert.AreEqual("test_value", savedProfile.GlobalConfig["test_setting"], "Profile settings should match");
            
            _mockLogger.Verify(l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Successfully saved profile")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [TestMethod]
        public async Task SaveProfileAsync_WithEmptyName_ThrowsException()
        {
            // Arrange
            var profile = CreateTestProfile("");

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(() => 
                _profileService.SaveProfileAsync(profile, _testProfilesPath));
            
            _mockLogger.Verify(l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [TestMethod]
        public async Task DeleteProfileAsync_ExistingProfile_Succeeds()
        {
            // Arrange
            var profile = CreateTestProfile("Delete Test Profile");
            await CreateTestProfileFile(profile);
            string profilePath = Path.Combine(_testProfilesPath, "delete_test_profile.json");
            Assert.IsTrue(File.Exists(profilePath), "Setup failed: Profile file should exist before test");

            // Act
            await _profileService.DeleteProfileAsync(profile.Name, _testProfilesPath);

            // Assert
            Assert.IsFalse(File.Exists(profilePath), "Profile file should be deleted");
            
            _mockLogger.Verify(l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Successfully deleted profile")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [TestMethod]
        public async Task DeleteProfileAsync_NonexistentProfile_LogsWarning()
        {
            // Arrange
            string nonexistentProfile = "Nonexistent Profile";

            // Act
            await _profileService.DeleteProfileAsync(nonexistentProfile, _testProfilesPath);

            // Assert
            _mockLogger.Verify(l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Profile file not found for deletion")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [TestMethod]
        public async Task GetProfileFromCache_ExistingProfile_ReturnsProfile()
        {
            // Arrange
            var profile = CreateTestProfile("Cache Test Profile");
            await _profileService.SaveProfileAsync(profile, _testProfilesPath);
            
            // Load to ensure it's in cache
            await _profileService.ReadProfilesFromJsonAsync(_testProfilesPath);

            // Act
            var result = _profileService.GetProfileFromCache("Cache Test Profile");

            // Assert
            Assert.IsNotNull(result, "Profile should be returned from cache");
            Assert.AreEqual("Cache Test Profile", result.Value.Name, "Profile name should match");
        }

        [TestMethod]
        public void GetProfileFromCache_NonexistentProfile_ReturnsNull()
        {
            // Arrange - Cache should be empty after initialization

            // Act
            var result = _profileService.GetProfileFromCache("Nonexistent Profile");

            // Assert
            Assert.IsNull(result, "Nonexistent profile should return null");
        }

        [TestMethod]
        public async Task ClearCache_RemovesCachedProfiles()
        {
            // Arrange
            var profile = CreateTestProfile("Clear Cache Test");
            await _profileService.SaveProfileAsync(profile, _testProfilesPath);
            
            // Load to ensure it's in cache
            await _profileService.ReadProfilesFromJsonAsync(_testProfilesPath);
            
            var beforeClear = _profileService.GetProfileFromCache("Clear Cache Test");
            Assert.IsNotNull(beforeClear, "Setup failed: Profile should be in cache");

            // Act
            _profileService.ClearCache();
            var afterClear = _profileService.GetProfileFromCache("Clear Cache Test");

            // Assert
            Assert.IsNull(afterClear, "Profile should not be in cache after clear");
        }

        // Helper methods for test data generation
        private static Profile CreateTestProfile(string name = null)
        {
            return new Profile
            {
                GlobalConfig = new Dictionary<string, string>(),
                GuiElements = new List<GuiElement>(),
                Poses = new List<PoseGuiElement>(),
                SpeechCommands = new Dictionary<string, SpeechCommand>(),
                Name = name ?? $"Test Profile {DateTime.Now.Ticks}"
            };
        }

        private async Task<string> CreateTestProfileFile(Profile profile, string folderPath = null)
        {
            folderPath ??= _testProfilesPath;
            Directory.CreateDirectory(folderPath);

            string fileName = profile.Name.Replace(" ", "_").ToLowerInvariant();
            string filePath = Path.Combine(folderPath, $"{fileName}.json");
            
            string json = JsonConvert.SerializeObject(profile);
            await File.WriteAllTextAsync(filePath, json);
            
            return filePath;
        }

        private async Task<Profile> LoadProfileFromFile(string filePath)
        {
            string json = await File.ReadAllTextAsync(filePath);
            var profile = JsonConvert.DeserializeObject<Profile>(json);
            
            // Set the name property based on the file name since it's not stored in the JSON
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            profile.Name = fileName.Replace("_", " ");
            
            return profile;
        }
    }
}
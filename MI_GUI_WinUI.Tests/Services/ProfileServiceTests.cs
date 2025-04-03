// using Microsoft.VisualStudio.TestTools.UnitTesting;
// using MI_GUI_WinUI.Models;
// using MI_GUI_WinUI.Services;
// using MI_GUI_WinUI.Tests.TestUtils;
// using Microsoft.Extensions.Logging;
// using Moq;
// using System;
// using System.Collections.Generic;
// using System.IO;
// using System.Threading.Tasks;
// using Windows.Storage;

// namespace MI_GUI_WinUI.Tests.Services
// {
//     [TestClass]
//     public class ProfileServiceTests : UnitTestBase
//     {
//         private ProfileService _profileService;
//         private Mock<ILogger<ProfileService>> _mockLogger;
//         private string _testProfilesPath;

//         [TestInitialize]
//         public override async Task InitializeTest()
//         {
//             await base.InitializeTest();

//             // Setup test directory
//             _testProfilesPath = Path.Combine(TestDirectory, "Profiles");
//             Directory.CreateDirectory(_testProfilesPath);

//             // Setup mock logger
//             _mockLogger = new Mock<ILogger<ProfileService>>();
            
//             // Create profile service instance
//             _profileService = new ProfileService(_mockLogger.Object);
//         }

//         private Profile CreateTestProfile(string name = "Test Profile")
//         {
//             return new Profile
//             {
//                 Name = name,
//                 GlobalConfig = new Dictionary<string, string>
//                 {
//                     { "setting1", "value1" },
//                     { "setting2", "value2" }
//                 },
//                 GuiElements = new List<GuiElement>
//                 {
//                     new GuiElement
//                     {
//                         File = "test.png",
//                         Position = new List<int> { 100, 100 },
//                         Radius = 50,
//                         Skin = "default",
//                         TriggeredSkin = "triggered",
//                         Action = new ActionConfig
//                         {
//                             ClassName = "TestClass",
//                             MethodName = "TestMethod",
//                             Arguments = new List<object>()
//                         }
//                     }
//                 },
//                 Poses = new List<PoseGuiElement>
//                 {
//                     new PoseGuiElement
//                     {
//                         File = "pose.png",
//                         Position = new List<int> { 200, 200 },
//                         Radius = 50,
//                         Skin = "default",
//                         Action = new ActionConfig
//                         {
//                             ClassName = "PoseClass",
//                             MethodName = "PoseMethod",
//                             Arguments = new List<object>()
//                         }
//                     }
//                 },
//                 SpeechCommands = new Dictionary<string, SpeechCommand>
//                 {
//                     {
//                         "test_command",
//                         new SpeechCommand
//                         {
//                             CommandName = "Test Command",
//                             Action = new SpeechActionConfig
//                             {
//                                 ClassName = "SpeechClass",
//                                 MethodName = "SpeechMethod",
//                                 Arguments = new List<object>()
//                             }
//                         }
//                     }
//                 }
//             };
//         }

//         [TestCleanup]
//         public override void CleanupTest()
//         {
//             base.CleanupTest();
//         }

//         [TestMethod]
//         [TestCategory("Unit")]
//         public async Task ReadProfilesFromJsonAsync_EmptyDirectory_ReturnsEmptyList()
//         {
//             // Act
//             var profiles = await _profileService.ReadProfilesFromJsonAsync(_testProfilesPath);

//             // Assert
//             Assert.IsNotNull(profiles);
//             Assert.AreEqual(0, profiles.Count);
//         }

//         [TestMethod]
//         [TestCategory("Unit")]
//         public async Task SaveProfileAsync_ValidProfile_SavesSuccessfully()
//         {
//             // Arrange
//             var profile = CreateTestProfile();

//             // Act
//             await _profileService.SaveProfileAsync(profile, _testProfilesPath);

//             // Assert
//             string fileName = $"{profile.Name.Replace(" ", "_").ToLower()}.json";
//             string filePath = Path.Combine(_testProfilesPath, fileName);
//             Assert.IsTrue(File.Exists(filePath));

//             // Verify the profile can be read back
//             var profiles = await _profileService.ReadProfilesFromJsonAsync(_testProfilesPath);
//             Assert.AreEqual(1, profiles.Count);
//             Assert.AreEqual(profile.Name, profiles[0].Name);
//         }

//         [TestMethod]
//         [TestCategory("Unit")]
//         public async Task DeleteProfileAsync_ExistingProfile_DeletesSuccessfully()
//         {
//             // Arrange
//             var profile = CreateTestProfile();
//             await _profileService.SaveProfileAsync(profile, _testProfilesPath);

//             // Act
//             await _profileService.DeleteProfileAsync(profile.Name, _testProfilesPath);

//             // Assert
//             string fileName = $"{profile.Name.Replace(" ", "_").ToLower()}.json";
//             string filePath = Path.Combine(_testProfilesPath, fileName);
//             Assert.IsFalse(File.Exists(filePath));
//         }

//         [TestMethod]
//         [TestCategory("Unit")]
//         public async Task GetProfileFromCache_ExistingProfile_ReturnsCachedProfile()
//         {
//             // Arrange
//             var profile = CreateTestProfile();
//             await _profileService.SaveProfileAsync(profile, _testProfilesPath);
//             await _profileService.ReadProfilesFromJsonAsync(_testProfilesPath);

//             // Act
//             var cachedProfile = _profileService.GetProfileFromCache(profile.Name);

//             // Assert
//             Assert.IsNotNull(cachedProfile);
//             Assert.AreEqual(profile.Name, cachedProfile?.Name);
            
//             // Additional verification of profile data
//             Assert.IsTrue(cachedProfile?.GlobalConfig.ContainsKey("setting1"));
//             Assert.AreEqual("value1", cachedProfile?.GlobalConfig["setting1"]);
//         }

//         [TestMethod]
//         [TestCategory("Unit")]
//         public void ClearCache_WithCachedProfiles_ClearsAllProfiles()
//         {
//             // Arrange
//             var profile = CreateTestProfile();
//             _profileService.SaveProfileAsync(profile, _testProfilesPath).Wait();
//             _profileService.ReadProfilesFromJsonAsync(_testProfilesPath).Wait();

//             // Act
//             _profileService.ClearCache();

//             // Assert
//             var cachedProfile = _profileService.GetProfileFromCache(profile.Name);
//             Assert.IsNull(cachedProfile);
//         }

//         [TestMethod]
//         [TestCategory("Unit")]
//         public async Task ReadProfilesFromJsonAsync_InvalidProfileFile_SkipsInvalidProfile()
//         {
//             // Arrange
//             var invalidJson = "{ invalid json }";
//             string fileName = "invalid_profile.json";
//             string filePath = Path.Combine(_testProfilesPath, fileName);
//             await File.WriteAllTextAsync(filePath, invalidJson);

//             // Act
//             var profiles = await _profileService.ReadProfilesFromJsonAsync(_testProfilesPath);

//             // Assert
//             Assert.AreEqual(0, profiles.Count);
//             _mockLogger.Verify(
//                 x => x.Log(
//                     It.Is<LogLevel>(l => l == LogLevel.Error),
//                     It.IsAny<EventId>(),
//                     It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("JSON parsing error")),
//                     It.IsAny<Exception>(),
//                     It.IsAny<Func<It.IsAnyType, Exception, string>>()),
//                 Times.Once);
//         }

//         [TestMethod]
//         [TestCategory("Unit")]
//         public async Task SaveProfileAsync_InvalidFolder_CreatesDirectoryAndSaves()
//         {
//             // Arrange
//             var profile = CreateTestProfile();
//             var nestedPath = Path.Combine(_testProfilesPath, "nested", "profiles");

//             // Act
//             await _profileService.SaveProfileAsync(profile, nestedPath);

//             // Assert
//             string fileName = $"{profile.Name.Replace(" ", "_").ToLower()}.json";
//             string filePath = Path.Combine(nestedPath, fileName);
//             Assert.IsTrue(File.Exists(filePath));
//         }

//         [TestMethod]
//         [TestCategory("Unit")]
//         public async Task ReadProfilesFromJsonAsync_MultipleProfiles_LoadsAllProfiles()
//         {
//             // Arrange
//             var profile1 = CreateTestProfile("Profile 1");
//             var profile2 = CreateTestProfile("Profile 2");
//             await _profileService.SaveProfileAsync(profile1, _testProfilesPath);
//             await _profileService.SaveProfileAsync(profile2, _testProfilesPath);

//             // Act
//             var profiles = await _profileService.ReadProfilesFromJsonAsync(_testProfilesPath);

//             // Assert
//             Assert.AreEqual(2, profiles.Count);
//             Assert.IsTrue(profiles.Any(p => p.Name == "Profile 1"));
//             Assert.IsTrue(profiles.Any(p => p.Name == "Profile 2"));
//         }

//         [TestMethod]
//         [TestCategory("Unit")]
//         public async Task DeleteProfileAsync_NonexistentFile_LogsWarning()
//         {
//             // Arrange
//             string nonexistentProfile = "nonexistent_profile";

//             // Act
//             await _profileService.DeleteProfileAsync(nonexistentProfile, _testProfilesPath);

//             // Assert
//             _mockLogger.Verify(
//                 x => x.Log(
//                     It.Is<LogLevel>(l => l == LogLevel.Warning),
//                     It.IsAny<EventId>(),
//                     It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Profile file not found for deletion")),
//                     It.IsAny<Exception>(),
//                     It.IsAny<Func<It.IsAnyType, Exception, string>>()),
//                 Times.Once);
//         }

//         [TestMethod]
//         [TestCategory("Unit")]
//         public async Task SaveProfileAsync_SameProfileTwice_UpdatesExistingFile()
//         {
//             // Arrange
//             var profile = CreateTestProfile();
//             await _profileService.SaveProfileAsync(profile, _testProfilesPath);

//             // Modify profile
//             profile.GlobalConfig["setting1"] = "updated_value";
            
//             // Act
//             await _profileService.SaveProfileAsync(profile, _testProfilesPath);

//             // Assert
//             var profiles = await _profileService.ReadProfilesFromJsonAsync(_testProfilesPath);
//             Assert.AreEqual(1, profiles.Count);
//             Assert.AreEqual("updated_value", profiles[0].GlobalConfig["setting1"]);
//         }

//         [TestMethod]
//         [TestCategory("Unit")]
//         [ExpectedException(typeof(ArgumentException))]
//         public async Task SaveProfileAsync_EmptyName_ThrowsArgumentException()
//         {
//             // Arrange
//             var profile = CreateTestProfile(string.Empty);

//             // Act
//             await _profileService.SaveProfileAsync(profile, _testProfilesPath);
//         }

//         [TestMethod]
//         [TestCategory("Unit")]
//         public async Task SaveProfileAsync_EmptyGlobalConfig_SavesSuccessfully()
//         {
//             // Arrange
//             var profile = CreateTestProfile();
//             profile.GlobalConfig.Clear();

//             // Act
//             await _profileService.SaveProfileAsync(profile, _testProfilesPath);

//             // Assert
//             var profiles = await _profileService.ReadProfilesFromJsonAsync(_testProfilesPath);
//             Assert.AreEqual(1, profiles.Count);
//             Assert.AreEqual(0, profiles[0].GlobalConfig.Count);
//         }

//         [TestMethod]
//         [TestCategory("Unit")]
//         public async Task SaveProfileAsync_EmptyPoses_SavesSuccessfully()
//         {
//             // Arrange
//             var profile = CreateTestProfile();
//             profile.Poses.Clear();

//             // Act
//             await _profileService.SaveProfileAsync(profile, _testProfilesPath);

//             // Assert
//             var profiles = await _profileService.ReadProfilesFromJsonAsync(_testProfilesPath);
//             Assert.AreEqual(1, profiles.Count);
//             Assert.AreEqual(0, profiles[0].Poses.Count);
//         }

//         [TestMethod]
//         [TestCategory("Unit")]
//         public async Task SaveProfileAsync_EmptySpeechCommands_SavesSuccessfully()
//         {
//             // Arrange
//             var profile = CreateTestProfile();
//             profile.SpeechCommands.Clear();

//             // Act
//             await _profileService.SaveProfileAsync(profile, _testProfilesPath);

//             // Assert
//             var profiles = await _profileService.ReadProfilesFromJsonAsync(_testProfilesPath);
//             Assert.AreEqual(1, profiles.Count);
//             Assert.AreEqual(0, profiles[0].SpeechCommands.Count);
//         }

//         [TestMethod]
//         [TestCategory("Unit")]
//         public async Task SaveProfileAsync_VeryLongProfileName_SavesSuccessfully()
//         {
//             // Arrange
//             string longName = new string('a', 100); // 100 character name
//             var profile = CreateTestProfile(longName);

//             // Act
//             await _profileService.SaveProfileAsync(profile, _testProfilesPath);

//             // Assert
//             var profiles = await _profileService.ReadProfilesFromJsonAsync(_testProfilesPath);
//             Assert.AreEqual(1, profiles.Count);
//             Assert.AreEqual(longName, profiles[0].Name);
//         }
//     }
// }
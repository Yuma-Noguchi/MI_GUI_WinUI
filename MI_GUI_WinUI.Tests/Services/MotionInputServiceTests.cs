using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MI_GUI_WinUI.Services;
using MI_GUI_WinUI.Services.Interfaces;
using MI_GUI_WinUI.Tests.TestUtils;
using MI_GUI_WinUI.Tests.TestUtils.TestServices;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MI_GUI_WinUI.Tests.Services
{
    [TestClass]
    public class MotionInputServiceTests : UnitTestBase
    {
        private IMotionInputService _motionInputService;
        private string _testBasePath;
        private string _configFilePath;
        private string _profilesPath;
        private Mock<ILogger<TestableMotionInputService>> _mockLogger;
        private TestableMotionInputService _testableService;
        
        [TestInitialize]
        public override async Task InitializeTest()
        {
            await base.InitializeTest();
            
            // Create test directories
            _testBasePath = Path.Combine(TestDirectory, "MotionInput");
            _configFilePath = Path.Combine(_testBasePath, "data", "config.json");
            _profilesPath = Path.Combine(Path.GetDirectoryName(_configFilePath), "profiles");
            
            Directory.CreateDirectory(_profilesPath);
            
            _mockLogger = new Mock<ILogger<TestableMotionInputService>>();
            
            // Create testable service
            _testableService = new TestableMotionInputService(_mockLogger.Object, _testBasePath);
            _motionInputService = _testableService;
            
            // Create test config file
            var config = new JObject();
            config["mode"] = "default";
            Directory.CreateDirectory(Path.GetDirectoryName(_configFilePath));
            await File.WriteAllTextAsync(_configFilePath, config.ToString());
            
            // Create some test profiles
            CreateTestProfile("profile1");
            CreateTestProfile("profile2");
        }
        
        private void CreateTestProfile(string name)
        {
            var profilePath = Path.Combine(_profilesPath, $"{name}.json");
            File.WriteAllText(profilePath, "{}");
        }
        
        [TestMethod]
        public async Task GetAvailableProfilesAsync_ReturnsCorrectProfiles()
        {
            // Act
            var profiles = await _motionInputService.GetAvailableProfilesAsync();
            
            // Assert
            Assert.AreEqual(2, profiles.Count, "Should return 2 profiles");
            Assert.IsTrue(profiles.Contains("profile1"), "Should contain profile1");
            Assert.IsTrue(profiles.Contains("profile2"), "Should contain profile2");
        }
        
        [TestMethod]
        public async Task GetAvailableProfilesAsync_WithNoProfiles_ReturnsEmptyList()
        {
            // Arrange
            Directory.Delete(_profilesPath, true);
            Directory.CreateDirectory(_profilesPath);
            
            // Act
            var profiles = await _motionInputService.GetAvailableProfilesAsync();
            
            // Assert
            Assert.AreEqual(0, profiles.Count, "Should return empty list when no profiles exist");
        }
        
        [TestMethod]
        public async Task StartAsync_WritesCorrectModeToConfigFile()
        {
            // Act
            await _motionInputService.StartAsync("profile1");
            
            // Assert
            string configJson = await File.ReadAllTextAsync(_configFilePath);
            var configObj = JObject.Parse(configJson);
            Assert.AreEqual("profile1", configObj["mode"].ToString(), "Should write profile name to config file");
        }
        
        [TestMethod]
        public async Task StartAsync_ReturnsTrue_WhenSuccessful()
        {
            // Act
            var result = await _motionInputService.StartAsync("profile1");
            
            // Assert
            Assert.IsTrue(result, "Should return true when successfully started");
            Assert.IsTrue(_testableService.IsRunning(), "MotionInput process should be running");
        }
        
        [TestMethod]
        public async Task StopAsync_SetsMoitionInputToNull_WhenCalled()
        {
            // Arrange
            await _motionInputService.StartAsync("profile1");
            Assert.IsTrue(_testableService.IsRunning(), "Setup verification: MotionInput should be running");
            
            // Act
            var result = await _motionInputService.StopAsync("profile1");
            
            // Assert
            Assert.IsTrue(result, "Should return true when successfully stopped");
            Assert.IsFalse(_testableService.IsRunning(), "MotionInput process should not be running after stop");
        }
        
        [TestMethod]
        public async Task StopAsync_ReturnsFalse_WhenNotRunning()
        {
            // Act
            var result = await _motionInputService.StopAsync("profile1");
            
            // Assert
            Assert.IsFalse(result, "Should return false when no process was running");
        }
        
        [TestMethod]
        public async Task LaunchAsync_StartsMotionInput_WhenCalled()
        {
            // Act
            var result = await _motionInputService.LaunchAsync();
            
            // Assert
            Assert.IsTrue(result, "Should return true when launch is successful");
            Assert.IsTrue(_testableService.IsRunning(), "MotionInput process should be running after launch");
        }
        
        [TestMethod]
        public async Task LaunchAsync_RestartsPreviousProcess_WhenAlreadyRunning()
        {
            // Arrange
            await _motionInputService.LaunchAsync();
            Assert.IsTrue(_testableService.IsRunning(), "Setup verification: MotionInput should be running");
            
            // Act
            var result = await _motionInputService.LaunchAsync();
            
            // Assert
            Assert.IsTrue(result, "Should return true when relaunch is successful");
            Assert.IsTrue(_testableService.IsRunning(), "MotionInput process should still be running after relaunch");
        }
        
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task StartAsync_ThrowsException_WhenProfileNameIsNull()
        {
            // Act
            await _motionInputService.StartAsync(null);
            
            // Assert - Exception expected
        }
        
        [TestMethod]
        public async Task StartStop_FullLifecycle_WorksCorrectly()
        {
            // Arrange
            string testProfile = "test_profile";
            CreateTestProfile(testProfile);
            
            // Act 1 - Start profile
            var startResult = await _motionInputService.StartAsync(testProfile);
            
            // Assert 1
            Assert.IsTrue(startResult, "Should successfully start profile");
            Assert.IsTrue(_testableService.IsRunning(), "Process should be running after start");
            
            string configJson = await File.ReadAllTextAsync(_configFilePath);
            var configObj = JObject.Parse(configJson);
            Assert.AreEqual(testProfile, configObj["mode"].ToString(), "Config file should have correct profile");
            
            // Act 2 - Stop profile
            var stopResult = await _motionInputService.StopAsync(testProfile);
            
            // Assert 2
            Assert.IsTrue(stopResult, "Should successfully stop profile");
            Assert.IsFalse(_testableService.IsRunning(), "Process should not be running after stop");
        }
    }
}
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MI_GUI_WinUI.Services;
using MI_GUI_WinUI.Services.Interfaces;
using MI_GUI_WinUI.Tests.TestUtils;
using MI_GUI_WinUI.Tests.TestUtils.TestServices;
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
    public class MotionInputServiceIntegrationTests : IntegrationTestBase
    {
        private IMotionInputService _motionInputService;
        private string _testBasePath;
        private string _configFilePath;
        private string _profilesPath;
        private TestableMotionInputService _testableService;
        
        [TestInitialize]
        public override async Task InitializeTest()
        {
            await base.InitializeTest();
            
            // Create test directories
            _testBasePath = Path.Combine(TestDataPath, "MotionInput");
            _configFilePath = Path.Combine(_testBasePath, "data", "config.json");
            _profilesPath = Path.Combine(Path.GetDirectoryName(_configFilePath), "profiles");
            
            Directory.CreateDirectory(_profilesPath);
            
            // Create testable service with integration scope
            _testableService = new TestableMotionInputService(
                GetRequiredService<ILogger<TestableMotionInputService>>(),
                _testBasePath
            );
            _motionInputService = _testableService;
            
            // Create a default config file
            var config = new JObject();
            config["mode"] = "default";
            Directory.CreateDirectory(Path.GetDirectoryName(_configFilePath));
            await File.WriteAllTextAsync(_configFilePath, config.ToString());
            
            // Create some test profiles
            CreateTestProfile("gaming");
            CreateTestProfile("presentation");
            CreateTestProfile("accessibility");
        }
        
        protected override void ConfigureIntegrationServices(IServiceCollection services)
        {
            base.ConfigureIntegrationServices(services);
        }
        
        private void CreateTestProfile(string name)
        {
            var profilePath = Path.Combine(_profilesPath, $"{name}.json");
            
            // Create a realistic profile with some content
            var profileObj = new JObject();
            profileObj["name"] = name;
            profileObj["config"] = new JObject();
            profileObj["config"]["setting"] = "value";
            
            File.WriteAllText(profilePath, profileObj.ToString());
        }
        
        [TestMethod]
        [TestCategory("Integration")]
        public async Task GetAvailableProfilesAsync_ReturnsAllProfilesFromFilesystem()
        {
            // Act
            var profiles = await _motionInputService.GetAvailableProfilesAsync();
            
            // Assert
            Assert.AreEqual(3, profiles.Count, "Should return all 3 test profiles");
            Assert.IsTrue(profiles.Contains("gaming"), "Should contain gaming profile");
            Assert.IsTrue(profiles.Contains("presentation"), "Should contain presentation profile");
            Assert.IsTrue(profiles.Contains("accessibility"), "Should contain accessibility profile");
        }
        
        [TestMethod]
        [TestCategory("Integration")]
        public async Task StartAsync_UpdatesConfigFile_WithCorrectProfile()
        {
            // Arrange
            string testProfile = "gaming";
            
            // Act
            await _motionInputService.StartAsync(testProfile);
            
            // Assert
            Assert.IsTrue(File.Exists(_configFilePath), "Config file should exist");
            var configJson = await File.ReadAllTextAsync(_configFilePath);
            var configObj = JObject.Parse(configJson);
            Assert.AreEqual(testProfile, configObj["mode"].ToString(), "Config file should have profile as mode value");
        }
        
        [TestMethod]
        [TestCategory("Integration")]
        public async Task StartStopLaunch_VerifyFullLifecycle()
        {
            // Arrange
            string testProfile = "presentation";
            
            // Act 1 - Start profile
            var startResult = await _motionInputService.StartAsync(testProfile);
            
            // Assert 1
            Assert.IsTrue(startResult, "Start should return true");
            Assert.IsTrue(_testableService.IsRunning(), "Process should be running after start");
            
            // Verify config file
            string configJson = await File.ReadAllTextAsync(_configFilePath);
            var configObj = JObject.Parse(configJson);
            Assert.AreEqual(testProfile, configObj["mode"].ToString(), "Config file should have correct profile");
            
            // Act 2 - Stop profile
            var stopResult = await _motionInputService.StopAsync(testProfile);
            
            // Assert 2
            Assert.IsTrue(stopResult, "Stop should return true");
            Assert.IsFalse(_testableService.IsRunning(), "Process should not be running after stop");
            
            // Act 3 - Launch directly
            var launchResult = await _motionInputService.LaunchAsync();
            
            // Assert 3
            Assert.IsTrue(launchResult, "Launch should return true");
            Assert.IsTrue(_testableService.IsRunning(), "Process should be running after launch");
        }
        
        [TestMethod]
        [TestCategory("Integration")]
        public async Task CreateNewProfile_ThenStartIt_WorksCorrectly()
        {
            // Arrange - Create new profile
            string newProfileName = "custom_profile";
            CreateTestProfile(newProfileName);
            
            // Act 1 - Verify profile is available
            var profiles = await _motionInputService.GetAvailableProfilesAsync();
            
            // Assert 1
            Assert.IsTrue(profiles.Contains(newProfileName), "New profile should be in available profiles list");
            
            // Act 2 - Start new profile
            var startResult = await _motionInputService.StartAsync(newProfileName);
            
            // Assert 2
            Assert.IsTrue(startResult, "Should successfully start new profile");
            
            // Verify config file
            string configJson = await File.ReadAllTextAsync(_configFilePath);
            var configObj = JObject.Parse(configJson);
            Assert.AreEqual(newProfileName, configObj["mode"].ToString(), "Config file should have new profile name");
        }
        
        [TestMethod]
        [TestCategory("Integration")]
        public async Task MissingConfigDirectory_CreatesDirectoryWhenNeeded()
        {
            // Arrange - Delete config directory
            if (Directory.Exists(Path.GetDirectoryName(_configFilePath)))
            {
                Directory.Delete(Path.GetDirectoryName(_configFilePath), true);
            }
            
            // Act
            await _motionInputService.StartAsync("gaming");
            
            // Assert
            Assert.IsTrue(Directory.Exists(Path.GetDirectoryName(_configFilePath)), 
                "Config directory should be created if missing");
            Assert.IsTrue(File.Exists(_configFilePath), "Config file should be created");
        }
    }
}
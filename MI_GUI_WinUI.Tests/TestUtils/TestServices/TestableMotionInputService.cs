using Microsoft.Extensions.Logging;
using MI_GUI_WinUI.Services.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MI_GUI_WinUI.Tests.TestUtils.TestServices
{
    /// <summary>
    /// A testable version of MotionInputService that doesn't rely on Windows.ApplicationModel.Package.Current
    /// </summary>
    public class TestableMotionInputService : IMotionInputService
    {
        private readonly ILogger<TestableMotionInputService> _logger;
        private Process _motionInput;
        private string _configFilePath;
        private string _executablePath;
        private string _basePath;

        public TestableMotionInputService(ILogger<TestableMotionInputService> logger, string basePath = null)
        {
            _logger = logger;
            _basePath = basePath ?? Path.Combine(Path.GetTempPath(), "MI_GUI_WinUI_Tests", "MotionInput");
            _configFilePath = Path.Combine(_basePath, "data", "config.json");
            _executablePath = Path.Combine(_basePath, "MotionInput.exe");
            
            // Ensure directories exist
            Directory.CreateDirectory(Path.GetDirectoryName(_configFilePath));
        }

        public async Task<bool> StartAsync(string profileName)
        {
            try
            {
                WriteModeToJsonFile(_configFilePath, profileName);
                _logger.LogInformation($"Starting profile: {profileName}");
                return await LaunchAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error starting profile: {profileName}");
                throw;
            }
        }

        public async Task<bool> StopAsync(string profileName)
        {
            try
            {
                if (_motionInput != null)
                {
                    // In a test environment, we won't actually kill the process
                    // but we'll simulate it by setting _motionInput to null
                    _motionInput = null;
                    _logger.LogInformation($"Stopped profile: {profileName}");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error stopping profile: {profileName}");
                throw;
            }
        }

        public async Task<bool> LaunchAsync()
        {
            try
            {
                // In tests, we won't actually launch a process,
                // but we'll simulate it by creating a Process object
                if (_motionInput != null)
                {
                    _motionInput = null;  // Simulate killing existing process
                }

                _motionInput = new Process();
                _logger.LogInformation("Launching Motion Input (simulated in test)");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error launching Motion Input");
                throw;
            }
        }

        public async Task<List<string>> GetAvailableProfilesAsync()
        {
            try
            {
                var profilesPath = Path.Combine(
                    Path.GetDirectoryName(_configFilePath),
                    "profiles"
                );

                Directory.CreateDirectory(profilesPath);  // Ensure it exists for tests

                if (!Directory.Exists(profilesPath))
                {
                    _logger.LogWarning($"Profiles directory not found: {profilesPath}");
                    return new List<string>();
                }

                var profileFiles = Directory.GetFiles(profilesPath, "*.json");
                return profileFiles.Select(Path.GetFileNameWithoutExtension).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available profiles");
                throw;
            }
        }

        private string ReadModeFromJsonFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"JSON file not found at path: {filePath}");
            }

            try
            {
                var jsonString = File.ReadAllText(filePath);
                var jsonObject = JObject.Parse(jsonString);
                var modeValue = (string)jsonObject["mode"];

                if (string.IsNullOrEmpty(modeValue))
                {
                    throw new InvalidOperationException($"Mode value is empty in file: {filePath}");
                }

                _logger.LogInformation($"Read mode from config: {modeValue}");
                return modeValue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error reading mode from config file: {filePath}");
                throw;
            }
        }

        private void WriteModeToJsonFile(string filePath, string modeValue)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
            }
            
            if (modeValue == null)
            {
                throw new ArgumentNullException(nameof(modeValue), "Mode value cannot be null.");
            }

            try
            {
                var directory = Path.GetDirectoryName(filePath);
                Directory.CreateDirectory(directory);  // Ensure directory exists

                var jsonObject = File.Exists(filePath)
                    ? JObject.Parse(File.ReadAllText(filePath))
                    : new JObject();

                jsonObject["mode"] = modeValue;
                var jsonString = JsonConvert.SerializeObject(jsonObject, Formatting.Indented);
                
                File.WriteAllText(filePath, jsonString);
                _logger.LogInformation($"Updated mode in config file: {modeValue}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error writing mode to config file: {filePath}");
                throw;
            }
        }

        // Test helper methods
        public void SetConfigFilePath(string path)
        {
            _configFilePath = path;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
        }

        public void SetExecutablePath(string path)
        {
            _executablePath = path;
        }

        public void CreateTestProfile(string profileName)
        {
            var profilesPath = Path.Combine(
                Path.GetDirectoryName(_configFilePath),
                "profiles"
            );
            Directory.CreateDirectory(profilesPath);

            var profilePath = Path.Combine(profilesPath, $"{profileName}.json");
            File.WriteAllText(profilePath, "{}");
        }

        public bool IsRunning()
        {
            return _motionInput != null;
        }
    }
}
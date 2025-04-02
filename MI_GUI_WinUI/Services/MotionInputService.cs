﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MI_GUI_WinUI.Services.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MI_GUI_WinUI.Services
{
    /// <summary>
    /// Implementation of IMotionInputService for managing Motion Input operations
    /// </summary>
    public class MotionInputService : IMotionInputService
    {
        private readonly ILogger<MotionInputService> _logger;
        private Process? _motionInput;
        private readonly string _configFilePath;
        private readonly string _executablePath;

        public MotionInputService(ILogger<MotionInputService> logger)
        {
            _logger = logger;
            var basePath = Windows.ApplicationModel.Package.Current.InstalledLocation.Path;
            _configFilePath = Path.Combine(basePath, "MotionInput", "data", "config.json");
            _executablePath = Path.Combine(basePath, "MotionInput", "MotionInput.exe");
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
                    _motionInput.Kill();
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
                if (_motionInput != null)
                {
                    _motionInput.Kill();
                }

                _motionInput = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        UseShellExecute = true,
                        FileName = _executablePath,
                        WorkingDirectory = Path.GetDirectoryName(_executablePath)
                    }
                };

                _logger.LogInformation("Launching Motion Input");
                return _motionInput.Start();
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

        private async Task<bool> ChangeMode(string mode)
        {
            try
            {
                string configJson = await File.ReadAllTextAsync(_configFilePath);
                var configJsonObj = JObject.Parse(configJson);
                configJsonObj["mode"] = mode;
                await File.WriteAllTextAsync(_configFilePath, configJsonObj.ToString(Formatting.Indented));
                _logger.LogInformation($"Changed mode to: {mode}");
                return await LaunchAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error changing mode to: {mode}");
                throw;
            }
        }
    }
}
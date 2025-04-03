using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace MI_GUI_WinUI.Tests.TestUtils
{
    /// <summary>
    /// Helper class to initialize test data once for all tests
    /// </summary>
    public static class TestDataInitializer
    {
        private static readonly object _initLock = new object();
        
        /// <summary>
        /// Copies test data to the specified test directory
        /// </summary>
        public static void InitializeTestData(string testDirectory)
        {
            lock (_initLock)
            {
                try
                {
                    // Find the source TestData directory in the project output
                    string assemblyDir = AppDomain.CurrentDomain.BaseDirectory;
                    string sourceTestDataDir = Path.Combine(assemblyDir, "TestData");

                    // Check if the source TestData exists
                    if (!Directory.Exists(sourceTestDataDir))
                    {
                        throw new DirectoryNotFoundException($"Source test data directory not found at {sourceTestDataDir}. Make sure TestData is included in the project with CopyToOutputDirectory set to PreserveNewest.");
                    }

                    // Create the target test data directory
                    string targetTestDataDir = Path.Combine(testDirectory, "TestData");
                    Directory.CreateDirectory(targetTestDataDir);

                    // Copy all TestData contents recursively
                    CopyDirectory(sourceTestDataDir, targetTestDataDir);

                    // Create expected directories if they don't exist
                    EnsureTestDirectoriesExist(targetTestDataDir);

                    // Verify required files exist
                    VerifyRequiredFiles(targetTestDataDir);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to initialize test data: {ex.Message}", ex);
                }
            }
        }

        /// <summary>
        /// Copies a directory and all of its contents recursively
        /// </summary>
        private static void CopyDirectory(string sourceDir, string targetDir)
        {
            // Create the target directory if it doesn't exist
            Directory.CreateDirectory(targetDir);

            // Copy all files from source to target
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                string fileName = Path.GetFileName(file);
                string targetFilePath = Path.Combine(targetDir, fileName);
                File.Copy(file, targetFilePath, true);
            }

            // Copy all subdirectories recursively
            foreach (var directory in Directory.GetDirectories(sourceDir))
            {
                string dirName = Path.GetFileName(directory);
                string targetSubDir = Path.Combine(targetDir, dirName);
                CopyDirectory(directory, targetSubDir);
            }
        }

        /// <summary>
        /// Make sure all required test directories exist
        /// </summary>
        private static void EnsureTestDirectoriesExist(string testDataDir)
        {
            var requiredDirs = new[]
            {
                "Profiles",
                "Actions",
                "Config",
                "Prompts",
                "Samples",
                "Schemas"
            };

            foreach (var dir in requiredDirs)
            {
                Directory.CreateDirectory(Path.Combine(testDataDir, dir));
            }
        }

        /// <summary>
        /// Verify that essential files exist in the test data directory
        /// </summary>
        private static void VerifyRequiredFiles(string testDataDir)
        {
            // Check for schema files
            string actionSchemaPath = Path.Combine(testDataDir, "Schemas", "action-schema.json");
            string profileSchemaPath = Path.Combine(testDataDir, "Schemas", "profile-schema.json");
            
            if (!File.Exists(actionSchemaPath))
            {
                throw new FileNotFoundException($"Required schema file not found: action-schema.json");
            }
            
            if (!File.Exists(profileSchemaPath))
            {
                throw new FileNotFoundException($"Required schema file not found: profile-schema.json");
            }
            
            // Check for sample files and throw exceptions if they're missing
            string sampleActionPath = Path.Combine(testDataDir, "Actions", "sample_action.json");
            string sampleProfilePath = Path.Combine(testDataDir, "Profiles", "sample_profile.json");
            string testConfigPath = Path.Combine(testDataDir, "Config", "test_config.json");
            string testPromptsPath = Path.Combine(testDataDir, "Prompts", "test_prompts.json");
            
            if (!File.Exists(sampleActionPath))
            {
                throw new FileNotFoundException($"Required sample file not found: {sampleActionPath}");
            }
            
            if (!File.Exists(sampleProfilePath))
            {
                throw new FileNotFoundException($"Required sample file not found: {sampleProfilePath}");
            }
            
            if (!File.Exists(testConfigPath))
            {
                throw new FileNotFoundException($"Required config file not found: {testConfigPath}");
            }
            
            if (!File.Exists(testPromptsPath))
            {
                throw new FileNotFoundException($"Required prompts file not found: {testPromptsPath}");
            }
        }
    }
}
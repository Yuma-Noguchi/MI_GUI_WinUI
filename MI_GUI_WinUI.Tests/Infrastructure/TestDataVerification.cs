using Microsoft.VisualStudio.TestTools.UnitTesting;
using MI_GUI_WinUI.Tests.TestUtils;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace MI_GUI_WinUI.Tests.Infrastructure
{
    [TestClass]
    public class TestDataVerification : UnitTestBase
    {
        private string _sourceTestDataDir;

        [TestInitialize]
        public override async Task InitializeTest()
        {
            await base.InitializeTest();
            
            // Find the output directory where TestData should be
            string outputDir = AppDomain.CurrentDomain.BaseDirectory;
            _sourceTestDataDir = Path.Combine(outputDir, "TestData");

            // Verify the source directory exists
            if (!Directory.Exists(_sourceTestDataDir))
            {
                Assert.Fail($"Source test data directory not found at {_sourceTestDataDir}. Make sure TestData is included in the project with CopyToOutputDirectory set to PreserveNewest.");
            }

            // Create required test data directories
            var testDataPath = Path.Combine(TestDirectory, "TestData");
            Directory.CreateDirectory(testDataPath);
            
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
                Directory.CreateDirectory(Path.Combine(testDataPath, dir));
            }
            
            // Copy schema files
            CopyFilesFromSourceDir("Schemas", Path.Combine(testDataPath, "Schemas"));
            
            // Copy other test data files
            CopyFilesFromSourceDir("Profiles", Path.Combine(testDataPath, "Profiles"));
            CopyFilesFromSourceDir("Actions", Path.Combine(testDataPath, "Actions"));
            CopyFilesFromSourceDir("Config", Path.Combine(testDataPath, "Config"));
            CopyFilesFromSourceDir("Prompts", Path.Combine(testDataPath, "Prompts"));
            
            // Copy sample files to the Samples directory
            var samplesDir = Path.Combine(testDataPath, "Samples");
            CopyFileWithVerification(
                Path.Combine(_sourceTestDataDir, "Profiles", "sample_profile.json"),
                Path.Combine(samplesDir, "sample_profile.json"));
                
            CopyFileWithVerification(
                Path.Combine(_sourceTestDataDir, "Actions", "sample_action.json"),
                Path.Combine(samplesDir, "sample_action.json"));
        }
        
        private void CopyFilesFromSourceDir(string sourceDirName, string targetDir)
        {
            var sourceDir = Path.Combine(_sourceTestDataDir, sourceDirName);
            if (!Directory.Exists(sourceDir))
            {
                Assert.Fail($"Source directory not found: {sourceDir}");
            }
            
            var jsonFiles = Directory.GetFiles(sourceDir, "*.json");
            if (jsonFiles.Length == 0)
            {
                Assert.Fail($"No JSON files found in source directory: {sourceDir}");
            }
            
            foreach (var file in jsonFiles)
            {
                var targetFile = Path.Combine(targetDir, Path.GetFileName(file));
                CopyFileWithVerification(file, targetFile);
            }
        }
        
        private void CopyFileWithVerification(string sourcePath, string targetPath)
        {
            if (!File.Exists(sourcePath))
            {
                Assert.Fail($"Source file not found: {sourcePath}");
            }
            
            try
            {
                File.Copy(sourcePath, targetPath, true);
                
                // Verify JSON is valid
                string json = File.ReadAllText(targetPath);
                try
                {
                    JsonDocument.Parse(json);
                }
                catch (JsonException ex)
                {
                    Assert.Fail($"Invalid JSON in file {sourcePath}: {ex.Message}");
                }
            }
            catch (IOException ex)
            {
                Assert.Fail($"Failed to copy file from {sourcePath} to {targetPath}: {ex.Message}");
            }
        }

        [TestMethod]
        [DataDependentTest("Schemas")]
        [SmokeTest]
        public void VerifyTestDataSchemas()
        {
            // Verify sample profile schema
            var schemasDir = Path.Combine(TestDirectory, "TestData", "Schemas");
            var profileSchemaPath = Path.Combine(schemasDir, "profile-schema.json");
            var actionSchemaPath = Path.Combine(schemasDir, "action-schema.json");
            
            Assert.IsTrue(File.Exists(profileSchemaPath), "Profile schema file should exist");
            Assert.IsTrue(File.Exists(actionSchemaPath), "Action schema file should exist");
            
            // Validate schema files
            string profileSchemaJson = File.ReadAllText(profileSchemaPath);
            try
            {
                var profileSchema = JObject.Parse(profileSchemaJson);
                Assert.IsNotNull(profileSchema, "Profile schema should be valid JSON");
            }
            catch (Exception ex)
            {
                Assert.Fail($"Invalid profile schema: {ex.Message}");
            }
            
            string actionSchemaJson = File.ReadAllText(actionSchemaPath);
            try
            {
                var actionSchema = JObject.Parse(actionSchemaJson);
                Assert.IsNotNull(actionSchema, "Action schema should be valid JSON");
            }
            catch (Exception ex)
            {
                Assert.Fail($"Invalid action schema: {ex.Message}");
            }
        }

        [TestMethod]
        [DataDependentTest("SampleData")]
        [SmokeTest]
        public void VerifySampleData()
        {
            // Verify sample files exist
            var sampleDir = Path.Combine(TestDirectory, "TestData", "Samples");
            Assert.IsTrue(Directory.Exists(sampleDir), "Sample directory should exist");

            var profilePath = Path.Combine(sampleDir, "sample_profile.json");
            Assert.IsTrue(File.Exists(profilePath), "Sample profile should exist");

            var actionPath = Path.Combine(sampleDir, "sample_action.json");
            Assert.IsTrue(File.Exists(actionPath), "Sample action should exist");

            // Validate sample data is valid JSON
            ValidateJson(profilePath, "Sample profile");
            ValidateJson(actionPath, "Sample action");
        }

        [TestMethod]
        [DataDependentTest("TestData")]
        public void VerifyTestDataStructure()
        {
            // Verify test data directories exist
            var testDataPath = Path.Combine(TestDirectory, "TestData");
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
                var dirPath = Path.Combine(testDataPath, dir);
                Assert.IsTrue(Directory.Exists(dirPath), $"{dir} directory should exist");
                
                // Verify each directory has at least one JSON file
                var files = Directory.GetFiles(dirPath, "*.json");
                Assert.IsTrue(files.Length > 0, $"{dir} directory should contain at least one JSON file");
            }
        }

        [TestMethod]
        [DataDependentTest("Profiles")]
        public void ValidateAllProfileData()
        {
            var profilesDir = Path.Combine(TestDirectory, "TestData", "Profiles");
            var profileFiles = Directory.GetFiles(profilesDir, "*.json", SearchOption.AllDirectories);

            Assert.IsTrue(profileFiles.Length > 0, "Should have at least one profile file");

            foreach (var file in profileFiles)
            {
                ValidateJson(file, $"Profile {Path.GetFileName(file)}");
            }
        }

        [TestMethod]
        [DataDependentTest("Actions")]
        public void ValidateAllActionData()
        {
            var actionsDir = Path.Combine(TestDirectory, "TestData", "Actions");
            var actionFiles = Directory.GetFiles(actionsDir, "*.json", SearchOption.AllDirectories);

            Assert.IsTrue(actionFiles.Length > 0, "Should have at least one action file");

            foreach (var file in actionFiles)
            {
                ValidateJson(file, $"Action {Path.GetFileName(file)}");
            }
        }

        [TestMethod]
        [DataDependentTest("TestData")]
        public void ValidateTestDataReferences()
        {
            // Verify profiles don't reference non-existent actions
            var profilesDir = Path.Combine(TestDirectory, "TestData", "Profiles");
            var profileFiles = Directory.GetFiles(profilesDir, "*.json", SearchOption.AllDirectories);
            
            foreach (var profileFile in profileFiles)
            {
                ValidateJson(profileFile, $"Profile {Path.GetFileName(profileFile)}");
            }

            // Verify actions have valid references
            var actionsDir = Path.Combine(TestDirectory, "TestData", "Actions");
            var actionFiles = Directory.GetFiles(actionsDir, "*.json", SearchOption.AllDirectories);
            
            foreach (var actionFile in actionFiles)
            {
                ValidateJson(actionFile, $"Action {Path.GetFileName(actionFile)}");
            }
        }

        [TestMethod]
        [DataDependentTest("TestData")]
        public void ValidateTestDataPermissions()
        {
            var testDataPath = Path.Combine(TestDirectory, "TestData");
            
            // Verify we can read all files
            foreach (var file in Directory.GetFiles(testDataPath, "*.*", SearchOption.AllDirectories))
            {
                Assert.IsTrue(File.GetAttributes(file).HasFlag(FileAttributes.ReadOnly) == false,
                    $"File should be writable: {file}");

                try
                {
                    using (var stream = File.OpenRead(file))
                    {
                        Assert.IsTrue(stream.CanRead, $"File should be readable: {file}");
                    }
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Failed to read file {file}: {ex.Message}");
                }
            }

            // Verify we can write to directories
            foreach (var dir in Directory.GetDirectories(testDataPath, "*", SearchOption.AllDirectories))
            {
                var testFile = Path.Combine(dir, "test_write.tmp");
                try
                {
                    File.WriteAllText(testFile, "test");
                    Assert.IsTrue(File.Exists(testFile), $"Should be able to write to directory: {dir}");
                    File.Delete(testFile);
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Failed to write to directory {dir}: {ex.Message}");
                }
            }
        }
        
        private void ValidateJson(string filePath, string description)
        {
            if (!File.Exists(filePath))
            {
                Assert.Fail($"{description} file does not exist: {filePath}");
            }
            
            try
            {
                string json = File.ReadAllText(filePath);
                var jsonDoc = JsonDocument.Parse(json);
                Assert.IsNotNull(jsonDoc, $"{description} should be valid JSON");
            }
            catch (JsonException ex)
            {
                Assert.Fail($"{description} contains invalid JSON: {ex.Message}");
            }
            catch (Exception ex)
            {
                Assert.Fail($"Error validating {description}: {ex.Message}");
            }
        }
    }
}
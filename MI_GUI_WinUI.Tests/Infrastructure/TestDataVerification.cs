using Microsoft.VisualStudio.TestTools.UnitTesting;
using MI_GUI_WinUI.Tests.TestUtils;

namespace MI_GUI_WinUI.Tests.Infrastructure
{
    [TestClass]
    public class TestDataVerification : UnitTestBase
    {
        [TestMethod]
        [DataDependentTest("Schemas")]
        [SmokeTest]
        public void VerifyTestDataSchemas()
        {
            // Verify sample profile schema
            var profileSchema = SchemaValidator.GetSchema("profile-schema");
            Assert.IsNotNull(profileSchema, "Profile schema should be loaded");

            // Verify sample action schema
            var actionSchema = SchemaValidator.GetSchema("action-schema");
            Assert.IsNotNull(actionSchema, "Action schema should be loaded");
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

            // Validate sample data against schemas
            SchemaValidator.ValidateSampleData();
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
            }

            // Verify required schema files
            var schemasDir = Path.Combine(testDataPath, "Samples", "Schemas");
            Assert.IsTrue(File.Exists(Path.Combine(schemasDir, "profile-schema.json")), 
                "Profile schema should exist");
            Assert.IsTrue(File.Exists(Path.Combine(schemasDir, "action-schema.json")), 
                "Action schema should exist");
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
                SchemaValidator.AssertValidJsonFile(file, "profile-schema");
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
                SchemaValidator.AssertValidJsonFile(file, "action-schema");
            }
        }

        [TestMethod]
        [DataDependentTest("TestData")]
        public void ValidateTestDataReferences()
        {
            // Verify profiles don't reference non-existent actions
            var profilesDir = Path.Combine(TestDirectory, "TestData", "Profiles");
            var actionsDir = Path.Combine(TestDirectory, "TestData", "Actions");
            
            foreach (var profileFile in Directory.GetFiles(profilesDir, "*.json", SearchOption.AllDirectories))
            {
                var profileJson = File.ReadAllText(profileFile);
                var errors = SchemaValidator.GetValidationErrors(profileJson, "profile-schema");
                
                Assert.IsFalse(errors.Any(), 
                    $"Profile {Path.GetFileName(profileFile)} has validation errors:\n" +
                    string.Join("\n", errors));
            }

            // Verify actions have valid references
            foreach (var actionFile in Directory.GetFiles(actionsDir, "*.json", SearchOption.AllDirectories))
            {
                var actionJson = File.ReadAllText(actionFile);
                var errors = SchemaValidator.GetValidationErrors(actionJson, "action-schema");
                
                Assert.IsFalse(errors.Any(), 
                    $"Action {Path.GetFileName(actionFile)} has validation errors:\n" +
                    string.Join("\n", errors));
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

                using (var stream = File.OpenRead(file))
                {
                    Assert.IsTrue(stream.CanRead, $"File should be readable: {file}");
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
    }
}
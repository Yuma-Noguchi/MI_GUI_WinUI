using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using System.Collections.Concurrent;

namespace MI_GUI_WinUI.Tests.TestUtils
{
    /// <summary>
    /// Provides JSON schema validation for test data
    /// </summary>
    public static class SchemaValidator
    {
        private static readonly ConcurrentDictionary<string, JSchema> _schemas = new();
        
        /// <summary>
        /// Load and cache a schema from file
        /// </summary>
        public static JSchema GetSchema(string schemaName)
        {
            return _schemas.GetOrAdd(schemaName, name =>
            {
                var schemaPath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "TestData",
                    "Schemas",
                    $"{name}.json"
                );

                if (!File.Exists(schemaPath))
                {
                    throw new FileNotFoundException($"Schema file not found: {name}.json");
                }

                var schemaJson = File.ReadAllText(schemaPath);
                return JSchema.Parse(schemaJson);
            });
        }

        /// <summary>
        /// Validates JSON data against a schema
        /// </summary>
        public static void ValidateJson(string json, string schemaName)
        {
            var schema = GetSchema(schemaName);
            var jsonObject = JToken.Parse(json);
            
            IList<string> errorMessages;
            if (!jsonObject.IsValid(schema, out errorMessages))
            {
                var errorMessage = $"JSON validation failed for schema {schemaName}:\n" +
                    string.Join("\n", errorMessages);
                throw new JsonReaderException(errorMessage);
            }
        }

        /// <summary>
        /// Validates a JSON file against a schema
        /// </summary>
        public static void ValidateJsonFile(string filePath, string schemaName)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"JSON file not found: {filePath}");
            }

            var json = File.ReadAllText(filePath);
            ValidateJson(json, schemaName);
        }

        /// <summary>
        /// Asserts that JSON data is valid according to schema
        /// </summary>
        public static void AssertValidJson(string json, string schemaName)
        {
            try
            {
                ValidateJson(json, schemaName);
            }
            catch (JsonReaderException ex)
            {
                Assert.Fail($"JSON validation failed: {ex.Message}");
            }
            catch (Exception ex)
            {
                Assert.Fail($"Schema validation error: {ex.Message}");
            }
        }

        /// <summary>
        /// Asserts that a JSON file is valid according to schema
        /// </summary>
        public static void AssertValidJsonFile(string filePath, string schemaName)
        {
            try
            {
                ValidateJsonFile(filePath, schemaName);
            }
            catch (Exception ex)
            {
                Assert.Fail($"JSON file validation failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Validates test data directory against schemas
        /// </summary>
        public static void ValidateTestDataDirectory(string directory)
        {
            var errors = new List<string>();

            try
            {
                // Validate profiles
                var profileFiles = Directory.GetFiles(
                    Path.Combine(directory, "Profiles"),
                    "*.json",
                    SearchOption.AllDirectories
                );

                foreach (var file in profileFiles)
                {
                    try
                    {
                        ValidateJsonFile(file, "profile-schema");
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Profile validation failed for {file}: {ex.Message}");
                    }
                }

                // Validate actions
                var actionFiles = Directory.GetFiles(
                    Path.Combine(directory, "Actions"),
                    "*.json",
                    SearchOption.AllDirectories
                );

                foreach (var file in actionFiles)
                {
                    try
                    {
                        ValidateJsonFile(file, "action-schema");
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Action validation failed for {file}: {ex.Message}");
                    }
                }

                if (errors.Any())
                {
                    Assert.Fail($"Test data validation failed:\n{string.Join("\n", errors)}");
                }
            }
            catch (Exception ex)
            {
                Assert.Fail($"Test data directory validation failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Validates all sample test data
        /// </summary>
        public static void ValidateSampleData()
        {
            var sampleDir = Path.Combine(
                Windows.ApplicationModel.Package.Current.InstalledLocation.Path,
                "TestData",
                "Samples"
            );

            AssertValidJsonFile(
                Path.Combine(sampleDir, "sample_profile.json"),
                "profile-schema"
            );

            AssertValidJsonFile(
                Path.Combine(sampleDir, "sample_action.json"),
                "action-schema"
            );
        }

        /// <summary>
        /// Gets schema validation errors for JSON
        /// </summary>
        public static IEnumerable<string> GetValidationErrors(string json, string schemaName)
        {
            try
            {
                var schema = GetSchema(schemaName);
                var jsonObject = JToken.Parse(json);
                
                IList<string> errorMessages;
                jsonObject.IsValid(schema, out errorMessages);
                return errorMessages ?? Enumerable.Empty<string>();
            }
            catch (Exception ex)
            {
                return new[] { ex.Message };
            }
        }
    }
}
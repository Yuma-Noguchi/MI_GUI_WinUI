using MI_GUI_WinUI.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace MI_GUI_WinUI.Tests.TestUtils
{
    /// <summary>
    /// Provides methods for generating test data
    /// </summary>
    public static class TestDataGenerators
    {
        /// <summary>
        /// Creates an action with the specified name and optional parameters
        /// </summary>
        public static ActionData CreateAction(
            string name = "Test Action",
            Dictionary<string, object>? args = null)
        {
            var actionArgs = new List<Dictionary<string, object>>
            {
                args ?? new Dictionary<string, object>
                {
                    { "param1", "value1" },
                    { "param2", 42 }
                }
            };
            
            return new ActionData
            {
                Id = Guid.NewGuid().ToString(),
                Name = name,
                Args = actionArgs
            };
        }

        /// <summary>
        /// Creates a list of test actions
        /// </summary>
        public static List<ActionData> CreateActions(int count)
        {
            var actions = new List<ActionData>();
            for (int i = 0; i < count; i++)
            {
                actions.Add(CreateAction($"Test Action {i}"));
            }
            return actions;
        }

        /// <summary>
        /// Creates a test image as byte array
        /// </summary>
        public static byte[] CreateTestImage(int width = 64, int height = 64)
        {
            // Create a simple test pattern
            var imageData = new byte[width * height * 4]; // RGBA
            for (int i = 0; i < imageData.Length; i += 4)
            {
                imageData[i] = 255;     // R
                imageData[i + 1] = 0;   // G
                imageData[i + 2] = 0;   // B
                imageData[i + 3] = 255; // A
            }
            return imageData;
        }

        /// <summary>
        /// Creates a JSON string representation of actions
        /// </summary>
        public static string CreateActionsJson(List<ActionData> actions)
        {
            var actionsDict = actions.ToDictionary(a => a.Id, a => a);
            return JsonSerializer.Serialize(actionsDict, new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }

        /// <summary>
        /// Creates test prompts for image generation
        /// </summary>
        public static IEnumerable<string> CreateTestPrompts(int count = 5)
        {
            var prompts = new[]
            {
                "A simple icon of a house",
                "Abstract geometric pattern",
                "Minimalist logo design",
                "Nature-inspired symbol",
                "Modern tech icon",
                "Simple line art face",
                "Circular mandala pattern",
                "Basic weather symbol",
                "Artistic letter A",
                "Simple animal silhouette"
            };

            return prompts.Take(Math.Min(count, prompts.Length));
        }

        /// <summary>
        /// Creates a mock file path for test data
        /// </summary>
        public static string CreateTestFilePath(string basePath, string fileName)
        {
            return Path.Combine(basePath, fileName);
        }

        /// <summary>
        /// Creates test configuration data
        /// </summary>
        public static string CreateTestConfig(Dictionary<string, object> values = null)
        {
            values ??= new Dictionary<string, object>
            {
                { "useCpu", false },
                { "imageSize", 512 },
                { "numberOfImages", 1 },
                { "modelPath", "path/to/model" }
            };

            return JsonSerializer.Serialize(values, new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }

        /// <summary>
        /// Creates test error data
        /// </summary>
        public static Exception CreateTestException(string message = "Test error", Exception innerException = null)
        {
            return new Exception(message, innerException);
        }

        /// <summary>
        /// Creates test log entries
        /// </summary>
        public static List<string> CreateTestLogEntries(int count = 5)
        {
            var entries = new List<string>();
            var timestamp = DateTime.UtcNow;

            for (int i = 0; i < count; i++)
            {
                entries.Add($"{timestamp.AddSeconds(i):yyyy-MM-dd HH:mm:ss} [INFO] Test log entry {i}");
            }

            return entries;
        }

        /// <summary>
        /// Creates a large test file with the specified size in KB
        /// </summary>
        public static async Task<string> CreateLargeTestFile(string path, int sizeInKb)
        {
            var buffer = new byte[1024]; // 1KB buffer
            new Random().NextBytes(buffer);

            await using var fileStream = File.Create(path);
            for (int i = 0; i < sizeInKb; i++)
            {
                await fileStream.WriteAsync(buffer);
            }

            return path;
        }

        /// <summary>
        /// Creates test progress updates
        /// </summary>
        public static IEnumerable<(int progress, string message)> CreateProgressUpdates()
        {
            return new[]
            {
                (0, "Starting..."),
                (25, "Loading model..."),
                (50, "Processing..."),
                (75, "Generating image..."),
                (100, "Complete")
            };
        }
    }
}
// using Microsoft.Extensions.Logging;
// using MI_GUI_WinUI.Models;
// using MI_GUI_WinUI.Services;
// using System;
// using System.Drawing;
// using System.Threading.Tasks;
// using Xunit;
// using Moq;

// namespace MI_GUI_WinUI.Tests
// {
//     public class StableDiffusionTests
//     {
//         private readonly Mock<ILoggerFactory> _mockLoggerFactory;
//         private readonly Mock<ILogger<StableDiffusionService>> _mockLogger;

//         public StableDiffusionTests()
//         {
//             _mockLogger = new Mock<ILogger<StableDiffusionService>>();
//             _mockLoggerFactory = new Mock<ILoggerFactory>();
//             _mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>()))
//                             .Returns(_mockLogger.Object);
//         }

//         [Fact]
//         public async Task InitializeShouldSetIsInitializedToTrue()
//         {
//             // Arrange
//             var service = new StableDiffusionService(_mockLoggerFactory.Object);

//             // Act
//             await service.Initialize(useGpu: false);

//             // Assert
//             Assert.True(service.IsInitialized);
//         }

//         [Fact]
//         public async Task GenerateImageShouldThrowIfNotInitialized()
//         {
//             // Arrange
//             var service = new StableDiffusionService(_mockLoggerFactory.Object);
//             var settings = new IconGenerationSettings
//             {
//                 Prompt = "test prompt",
//                 ImageSize = new Size(64, 64)
//             };

//             // Act & Assert
//             await Assert.ThrowsAsync<InvalidOperationException>(
//                 () => service.GenerateImage(settings)
//             );
//         }

//         [Fact]
//         public async Task GenerateImageShouldReturnImageBytes()
//         {
//             // Arrange
//             var service = new StableDiffusionService(_mockLoggerFactory.Object);
//             await service.Initialize(useGpu: false);

//             var settings = new IconGenerationSettings
//             {
//                 Prompt = "test icon with a star",
//                 ImageSize = new Size(64, 64),
//                 NumInferenceSteps = 5,  // Small number for testing
//                 GuidanceScale = 7.5f
//             };

//             var progress = new Progress<int>();

//             // Act
//             var imageBytes = await service.GenerateImage(settings, progress);

//             // Assert
//             Assert.NotNull(imageBytes);
//             Assert.True(imageBytes.Length > 0);
//         }

//         [Fact]
//         public async Task GenerateImageShouldReportProgress()
//         {
//             // Arrange
//             var service = new StableDiffusionService(_mockLoggerFactory.Object);
//             await service.Initialize(useGpu: false);

//             var settings = new IconGenerationSettings
//             {
//                 Prompt = "test icon",
//                 ImageSize = new Size(64, 64),
//                 NumInferenceSteps = 5
//             };

//             var progressReported = false;
//             var progress = new Progress<int>(value =>
//             {
//                 progressReported = true;
//                 Assert.True(value >= 0 && value <= settings.NumInferenceSteps);
//             });

//             // Act
//             await service.GenerateImage(settings, progress);

//             // Assert
//             Assert.True(progressReported);
//         }
//     }
// }

using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media;
using System.Collections.ObjectModel;

namespace MI_GUI_WinUI.Services.Interfaces
{
    /// <summary>
    /// Service interface for Stable Diffusion image generation
    /// </summary>
    public interface IStableDiffusionService : IDisposable
    {
        /// <summary>
        /// Gets whether the service is initialized
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// Gets whether the service is using CPU fallback mode
        /// </summary>
        bool UsingCpuFallback { get; }

        /// <summary>
        /// Gets the current generation progress percentage
        /// </summary>
        double Percentage { get; }

        /// <summary>
        /// Gets the time taken for the last generation in milliseconds
        /// </summary>
        long LastTimeMilliseconds { get; }

        /// <summary>
        /// Gets the current iterations per second
        /// </summary>
        double IterationsPerSecond { get; }

        /// <summary>
        /// Gets the number of inference steps used in generation
        /// </summary>
        long NumInferenceSteps { get; }

        /// <summary>
        /// Initializes the Stable Diffusion service
        /// </summary>
        /// <param name="useGpu">Whether to use GPU acceleration</param>
        Task Initialize(bool useGpu);

        /// <summary>
        /// Generates fake image data for testing
        /// </summary>
        /// <param name="description">The image description</param>
        /// <param name="numberOfImages">Number of images to generate</param>
        /// <returns>Collection of generated images</returns>
        ObservableCollection<ImageSource> GenerateFakeData(string description, int numberOfImages);

        /// <summary>
        /// Generates images using Stable Diffusion
        /// </summary>
        /// <param name="description">The image description</param>
        /// <param name="numberOfImages">Number of images to generate</param>
        /// <param name="stepCallback">Optional callback for generation progress</param>
        /// <returns>Array of paths to generated images</returns>
        Task<string[]> GenerateImages(string description, int numberOfImages, Action<int>? stepCallback = null);
    }
}
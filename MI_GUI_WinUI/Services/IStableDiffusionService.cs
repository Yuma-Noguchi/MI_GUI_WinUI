using System;
using System.Threading;
using System.Threading.Tasks;
using MI_GUI_WinUI.Models;

namespace MI_GUI_WinUI.Services
{
    public interface IStableDiffusionService : IDisposable
    {
        bool IsInitialized { get; }
        Task Initialize(bool useGpu);
        Task<byte[]> GenerateImage(IconGenerationSettings settings, IProgress<int>? progress = null);
        Task<(bool success, string path)> GenerateIconAsync(string prompt, IProgress<GenerationProgress> progress, CancellationToken cancellationToken);
    }
}

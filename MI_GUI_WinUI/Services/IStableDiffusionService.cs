using MI_GUI_WinUI.Models;
using System;
using System.Threading.Tasks;

namespace MI_GUI_WinUI.Services
{
    public interface IStableDiffusionService
    {
        bool IsInitialized { get; }
        Task Initialize(bool useGpu);
        Task<byte[]> GenerateImage(IconGenerationSettings settings, IProgress<int>? progress = null);
    }
}

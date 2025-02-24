# Icon Studio Implementation with ONNX Runtime for WinUI 3

## Project Setup

### 1. NuGet Dependencies
```xml
<ItemGroup>
    <!-- Microsoft.Windows.SDK.BuildTools and Microsoft.WindowsAppSDK should already be in your project -->
    <PackageReference Include="Microsoft.AI.MachineLearning" Version="1.16.1" />
    <PackageReference Include="Microsoft.ML.OnnxRuntime.DirectML" Version="1.16.1" />
    <PackageReference Include="SixLabors.ImageSharp" Version="3.1.1" />
</ItemGroup>
```

### 2. Model Loading Helper

```csharp
public static class ModelHelper
{
    private static readonly string modelPath = Path.Combine(
        Windows.Storage.ApplicationData.Current.LocalFolder.Path,
        "Models",
        "stable-diffusion.onnx");

    public static async Task<LearningModel> LoadModelAsync()
    {
        StorageFile modelFile = await StorageFile.GetFileFromPathAsync(modelPath);
        LearningModel learningModel = await LearningModel.LoadFromStorageFileAsync(modelFile);
        return learningModel;
    }
}
```

## Core Components

### 1. Model Session Management

```csharp
public class StableDiffusionSession : IDisposable
{
    private LearningModel _model;
    private LearningModelSession _session;
    private LearningModelDevice _device;

    public async Task InitializeAsync()
    {
        try
        {
            // Load model
            _model = await ModelHelper.LoadModelAsync();

            // Create device object to specify DirectML execution provider
            _device = new LearningModelDevice(LearningModelDeviceKind.DirectX);

            // Create session
            _session = new LearningModelSession(_model, _device);
        }
        catch (Exception ex)
        {
            throw new StableDiffusionException("Failed to initialize model session", ex);
        }
    }

    public async Task<TensorFloat> RunInferenceAsync(ImageFeatureValue inputImage)
    {
        try
        {
            // Create binding object for input and output
            var binding = new LearningModelBinding(_session);

            // Bind input
            binding.Bind("input_tensor", inputImage);

            // Run inference
            var results = await _session.EvaluateAsync(binding, "Running inference");

            // Get output tensor
            return results.Outputs["output_tensor"] as TensorFloat;
        }
        catch (Exception ex)
        {
            throw new StableDiffusionException("Inference failed", ex);
        }
    }

    public void Dispose()
    {
        _session?.Dispose();
        _model?.Dispose();
        _device?.Dispose();
    }
}
```

### 2. Image Processing Service

```csharp
public class ImageProcessor
{
    public static async Task<VideoFrame> LoadImageFileToVideoFrameAsync(StorageFile file)
    {
        var softwareBitmap = await LoadSoftwareBitmapAsync(file);
        return VideoFrame.CreateWithSoftwareBitmap(softwareBitmap);
    }

    private static async Task<SoftwareBitmap> LoadSoftwareBitmapAsync(StorageFile file)
    {
        using (IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.Read))
        {
            // Create decoder from stream
            BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);

            // Get software bitmap
            return await decoder.GetSoftwareBitmapAsync(
                BitmapPixelFormat.Bgra8,
                BitmapAlphaMode.Premultiplied);
        }
    }

    public static async Task SaveVideoFrameAsImageFileAsync(
        VideoFrame videoFrame,
        StorageFile file)
    {
        using (IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.ReadWrite))
        {
            // Create encoder
            BitmapEncoder encoder = await BitmapEncoder.CreateAsync(
                BitmapEncoder.PngEncoderId,
                stream);

            // Set software bitmap
            encoder.SetSoftwareBitmap(videoFrame.SoftwareBitmap);

            // Save file
            await encoder.FlushAsync();
        }
    }
}
```

### 3. Stable Diffusion Service

```csharp
public class StableDiffusionService
{
    private readonly StableDiffusionSession _session;
    private readonly ILogger<StableDiffusionService> _logger;

    public StableDiffusionService(ILogger<StableDiffusionService> logger)
    {
        _logger = logger;
        _session = new StableDiffusionSession();
    }

    public async Task InitializeAsync()
    {
        await _session.InitializeAsync();
    }

    public async Task<StorageFile> GenerateIconAsync(string prompt, GenerationSettings settings)
    {
        try
        {
            // Create input tensor from prompt
            var inputTensor = await CreateInputTensorFromPrompt(prompt);
            
            // Run inference
            var outputTensor = await _session.RunInferenceAsync(inputTensor);
            
            // Convert output to image
            var videoFrame = await ProcessOutputTensor(outputTensor);
            
            // Save result
            var file = await GetOutputFile();
            await ImageProcessor.SaveVideoFrameAsImageFileAsync(videoFrame, file);
            
            return file;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate icon");
            throw;
        }
    }

    private async Task<ImageFeatureValue> CreateInputTensorFromPrompt(string prompt)
    {
        // Implement prompt to tensor conversion
        throw new NotImplementedException();
    }

    private async Task<VideoFrame> ProcessOutputTensor(TensorFloat tensor)
    {
        // Convert tensor to VideoFrame
        throw new NotImplementedException();
    }

    private async Task<StorageFile> GetOutputFile()
    {
        var folder = await ApplicationData.Current.LocalFolder
            .CreateFolderAsync("GeneratedIcons", CreationCollisionOption.OpenIfExists);
            
        return await folder.CreateFileAsync(
            $"icon_{DateTime.Now:yyyyMMddHHmmss}.png",
            CreationCollisionOption.GenerateUniqueName);
    }
}
```

### 4. ViewModel Integration

```csharp
public partial class IconStudioViewModel : ObservableObject
{
    private readonly StableDiffusionService _stableDiffusionService;
    private readonly ILogger<IconStudioViewModel> _logger;

    [ObservableProperty]
    private bool isGenerating;

    [ObservableProperty]
    private ImageSource previewImage;

    [ObservableProperty]
    private string prompt;

    public IconStudioViewModel(
        StableDiffusionService stableDiffusionService,
        ILogger<IconStudioViewModel> logger)
    {
        _stableDiffusionService = stableDiffusionService;
        _logger = logger;
    }

    [RelayCommand]
    private async Task GenerateIconAsync()
    {
        try
        {
            IsGenerating = true;

            var settings = new GenerationSettings
            {
                Width = 512,
                Height = 512
            };

            var resultFile = await _stableDiffusionService.GenerateIconAsync(
                Prompt, settings);

            // Load result into preview
            await LoadPreviewImageAsync(resultFile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate icon");
            // Show error message
        }
        finally
        {
            IsGenerating = false;
        }
    }

    private async Task LoadPreviewImageAsync(StorageFile file)
    {
        using var stream = await file.OpenAsync(FileAccessMode.Read);
        var bitmapImage = new BitmapImage();
        await bitmapImage.SetSourceAsync(stream);
        PreviewImage = bitmapImage;
    }
}
```

## Error Handling

```csharp
public class StableDiffusionException : Exception
{
    public StableDiffusionException(string message) : base(message) { }
    public StableDiffusionException(string message, Exception inner) : base(message, inner) { }
}
```

## Application Registration

```csharp
// App.xaml.cs
public App()
{
    services.AddSingleton<StableDiffusionService>();
    services.AddSingleton<IconStudioViewModel>();
}
```

## Performance Optimizations

1. **Device Selection**
```csharp
private LearningModelDevice GetOptimalDevice()
{
    try
    {
        // Try DirectX first
        return new LearningModelDevice(LearningModelDeviceKind.DirectX);
    }
    catch
    {
        // Fall back to CPU
        return new LearningModelDevice(LearningModelDeviceKind.Cpu);
    }
}
```

2. **Memory Management**
```csharp
public class StableDiffusionSession : IDisposable
{
    public async Task<TensorFloat> RunInferenceAsync(ImageFeatureValue inputImage)
    {
        using var binding = new LearningModelBinding(_session);
        // ... inference code ...
    }
}
```

This updated implementation follows the Microsoft guidelines for integrating ONNX Runtime in WinUI 3 applications, using the Windows ML API for optimal performance on Windows devices.

## Next Steps

1. Implement the model conversion pipeline to get Stable Diffusion in ONNX format
2. Set up proper input tensor creation from text prompts
3. Implement output tensor processing to generate images
4. Add proper error handling and progress reporting
5. Optimize model performance with DirectML
6. Add model caching and version management

The implementation leverages Windows ML APIs for better integration with the Windows platform while maintaining the same functionality as the previous design.
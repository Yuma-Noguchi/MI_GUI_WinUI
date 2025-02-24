# Icon Studio Implementation with ONNX Runtime DirectML

## Architecture Overview

Instead of a separate MCP server, we'll integrate Stable Diffusion directly into the WinUI application using ONNX Runtime with DirectML acceleration.

### Core Components

```plaintext
MI_GUI_WinUI/
├── Services/
│   ├── StableDiffusionService.cs    # ONNX model inference
│   ├── IconStorageService.cs        # Icon storage and management
│   └── ModelManager.cs              # Model loading and caching
├── Models/
│   └── IconGeneration/
│       ├── GenerationSettings.cs    # Generation parameters
│       └── IconMetadata.cs         # Icon metadata
└── ViewModels/
    └── IconStudioViewModel.cs       # UI logic
```

## ONNX Integration

### 1. NuGet Dependencies
```xml
<ItemGroup>
    <PackageReference Include="Microsoft.ML.OnnxRuntime.DirectML" Version="1.16.1" />
    <PackageReference Include="SixLabors.ImageSharp" Version="3.1.1" />
</ItemGroup>
```

### 2. Stable Diffusion Service

```csharp
public class StableDiffusionService
{
    private readonly ILogger<StableDiffusionService> _logger;
    private readonly InferenceSession _session;
    private readonly ModelManager _modelManager;

    public StableDiffusionService(ILogger<StableDiffusionService> logger, ModelManager modelManager)
    {
        _logger = logger;
        _modelManager = modelManager;
        
        // Initialize ONNX Session with DirectML
        var options = new SessionOptions();
        options.AppendExecutionProvider_DML(0); // Use default GPU
        options.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;
        
        _session = new InferenceSession(_modelManager.ModelPath, options);
    }

    public async Task<byte[]> GenerateImageFromText(string prompt, GenerationSettings settings)
    {
        try
        {
            // Prepare input tensors
            var inputTensors = await PrepareInputs(prompt, settings);
            
            // Run inference
            using var outputs = await Task.Run(() => 
                _session.Run(inputTensors));
            
            // Process output
            return await ProcessOutput(outputs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating image from text");
            throw;
        }
    }

    public async Task<byte[]> GenerateImageFromImage(byte[] sourceImage, string prompt, GenerationSettings settings)
    {
        try
        {
            // Prepare input tensors with image
            var inputTensors = await PrepareInputsWithImage(sourceImage, prompt, settings);
            
            // Run inference
            using var outputs = await Task.Run(() => 
                _session.Run(inputTensors));
            
            // Process output
            return await ProcessOutput(outputs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating image from image");
            throw;
        }
    }

    private async Task<Dictionary<string, OnnxTensor>> PrepareInputs(string prompt, GenerationSettings settings)
    {
        // Token encoding
        var tokens = await TokenizePrompt(prompt);
        
        // Create input tensors
        var inputs = new Dictionary<string, OnnxTensor>();
        // Add required tensors based on model inputs
        return inputs;
    }

    private async Task<byte[]> ProcessOutput(IDisposableReadOnlyCollection<OnnxValue> outputs)
    {
        // Convert output tensors to image
        // Process through ImageSharp for final output
        return Array.Empty<byte>(); // Placeholder
    }
}
```

### 3. Model Manager

```csharp
public class ModelManager
{
    private readonly ILogger<ModelManager> _logger;
    private readonly string _modelPath;
    private readonly SemaphoreSlim _modelLock = new(1, 1);

    public string ModelPath => _modelPath;

    public ModelManager(ILogger<ModelManager> logger)
    {
        _logger = logger;
        _modelPath = Path.Combine(
            ApplicationData.Current.LocalFolder.Path,
            "Models",
            "stable-diffusion.onnx");
    }

    public async Task EnsureModelAvailable()
    {
        await _modelLock.WaitAsync();
        try
        {
            if (!File.Exists(_modelPath))
            {
                await DownloadModel();
            }
        }
        finally
        {
            _modelLock.Release();
        }
    }

    private async Task DownloadModel()
    {
        // Download ONNX model from configured source
        // Verify checksum
        // Save to local storage
    }
}
```

### 4. Generation Settings

```csharp
public class GenerationSettings
{
    public int Width { get; set; } = 512;
    public int Height { get; set; } = 512;
    public int Steps { get; set; } = 50;
    public float GuidanceScale { get; set; } = 7.5f;
    public int Seed { get; set; } = -1; // Random if negative
    public float DenoisingStrength { get; set; } = 0.7f; // For img2img
}
```

## Service Registration

```csharp
// App.xaml.cs
services.AddSingleton<ModelManager>();
services.AddSingleton<StableDiffusionService>();
services.AddSingleton<IconStorageService>();
```

## Memory Management

```csharp
public class StableDiffusionService : IDisposable
{
    private bool _disposed;
    private readonly InferenceSession _session;

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _session?.Dispose();
            }
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
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

// Usage in service
private async Task<byte[]> SafeInference(Func<Task<byte[]>> operation)
{
    try
    {
        return await operation();
    }
    catch (OutOfMemoryException ex)
    {
        throw new StableDiffusionException("Not enough GPU memory", ex);
    }
    catch (Exception ex)
    {
        throw new StableDiffusionException("Generation failed", ex);
    }
}
```

## Performance Optimization

1. **Batch Processing**
```csharp
public async Task<IEnumerable<byte[]>> GenerateBatch(
    string prompt, 
    GenerationSettings settings, 
    int batchSize)
{
    // Use parallel processing for batch generation
    // Manage memory usage
}
```

2. **Memory Management**
```csharp
private void OptimizeMemory()
{
    // Clear ONNX Runtime memory
    OrtMemoryInfo.ClearAllocatedMemory();
    
    // Suggest GC if memory pressure is high
    if (GC.GetTotalMemory(false) > threshold)
    {
        GC.Collect(2, GCCollectionMode.Optimized, true);
    }
}
```

This implementation provides a cleaner, more integrated solution that leverages DirectML for GPU acceleration while maintaining all functionality within the WinUI application.

## Next Steps

1. Set up the ONNX model conversion pipeline
2. Implement the basic StableDiffusionService
3. Create integration tests
4. Set up proper error handling and logging
5. Optimize performance with DirectML
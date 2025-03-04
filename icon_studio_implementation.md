# Icon Studio Implementation Plan - Stable Diffusion Integration

## Overview
This document outlines the plan to integrate Microsoft's official Stable Diffusion implementation into our Icon Studio feature. The goal is to replace our current implementation with a more robust and tested version while maintaining our icon-specific functionality.

## Current Issues
1. Black mosaic output in generated icons
2. Incorrect tensor handling
3. Suboptimal diffusion process
4. Inconsistent image quality

## Implementation Plan

### Phase 1: Core Infrastructure
1. Helper Classes
   - Create `StableDiffusionConfig`
     - Model paths configuration
     - Execution provider settings
     - Generation parameters
     - Session configuration
   
   - Port `SchedulerBase` and `LMSDiscreteScheduler`
     - Linear multistep scheduler implementation
     - Proper noise scheduling
     - Timestep management

   - Add `TensorHelper` utilities
     - Tensor creation and manipulation
     - Dimension handling
     - Array conversions

2. Dependencies
   - Add required NuGet packages:
     - MathNet.Numerics
     - NumSharp
     - SixLabors.ImageSharp

### Phase 2: Core Pipeline Implementation
1. StableDiffusionService Rewrite
   - Follow MS pipeline architecture
   - Maintain IStableDiffusionService interface
   - Components:
     - Text tokenization
     - Text encoding
     - UNet inference
     - VAE decoding
     - Image processing

2. Model Integration
   - Model loading and verification
   - Proper tensor name handling
   - Input/output shape validation
   - Error handling and logging

3. Generation Pipeline
   - Proper latent noise generation
   - Guidance scale application
   - Scheduler integration
   - Progress reporting

### Phase 3: Icon-Specific Features
1. Image Processing
   - Proper VAE output handling
   - Color channel management
   - Size-specific adaptations
   - Circular masking

2. UI Integration
   - Maintain current ViewModel interface
   - Progress state reporting
   - GPU/CPU selection
   - Error handling and user feedback

3. Icon Output
   - Format conversion
   - Size validation
   - Quality checks
   - Save functionality

## Technical Details

### Model Requirements
- ONNX format models:
  - text_encoder.onnx
  - unet.onnx
  - vae_decoder.onnx
  - vocab.json
  - merges.txt

### Key Classes
```csharp
public class StableDiffusionConfig
{
    public ExecutionProvider ExecutionProviderTarget { get; set; }
    public string TextEncoderPath { get; set; }
    public string UnetPath { get; set; }
    public string VaeDecoderPath { get; set; }
    // ... additional configuration
}

public abstract class SchedulerBase
{
    public abstract Tensor<float> Sigmas { get; set; }
    public abstract List<int> Timesteps { get; set; }
    public abstract float InitNoiseSigma { get; set; }
    // ... scheduler interface
}

public class LMSDiscreteScheduler : SchedulerBase
{
    // Linear multistep scheduler implementation
}
```

## Testing Strategy
1. Unit Tests
   - Individual component testing
   - Tensor operations validation
   - Scheduler behavior verification

2. Integration Tests
   - Full pipeline testing
   - Model loading verification
   - Image generation validation

3. UI Tests
   - Progress reporting
   - Error handling
   - GPU/CPU switching

## Migration Steps
1. Create new classes in parallel
2. Test new implementation independently
3. Switch services in DI container
4. Remove old implementation
5. Verify icon generation

## Success Criteria
1. Generated icons are clear and accurate
2. Performance is maintained or improved
3. Proper error handling and feedback
4. Consistent quality across different prompts
5. Successful circular icon generation

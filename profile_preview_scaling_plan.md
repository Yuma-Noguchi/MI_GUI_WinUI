# Profile Preview Scaling Implementation Plan

## Current State
- SelectProfilesViewModel uses different scaling constants than ProfileEditor
- Target height is 240px which might be too small
- Scale factor is calculated based on target height rather than actual canvas size

## Required Changes

### 1. Update Constants to Match ProfileEditor
```csharp
// Change from:
private const double GAME_HEIGHT = 480.0;
private const double GAME_WIDTH = 640.0;
private const double TARGET_HEIGHT = 240.0;
private const double TARGET_WIDTH = (TARGET_HEIGHT * GAME_WIDTH) / GAME_HEIGHT;
private const double SCALE_FACTOR = TARGET_HEIGHT / GAME_HEIGHT;

// To:
private const double MOTION_INPUT_WIDTH = 640.0;
private const double MOTION_INPUT_HEIGHT = 480.0;
private const double CANVAS_WIDTH = 560.0;
private const double CANVAS_HEIGHT = 400.0;
private const double SCALE_FACTOR_X = CANVAS_WIDTH / MOTION_INPUT_WIDTH;
private const double SCALE_FACTOR_Y = CANVAS_HEIGHT / MOTION_INPUT_HEIGHT;
```

### 2. Update Preview Generation
In `GeneratePreviewForProfileAsync`:
1. Use new canvas dimensions:
```csharp
Canvas preview = new Canvas
{
    Width = CANVAS_WIDTH,
    Height = CANVAS_HEIGHT,
    ...
};
```

2. Update element positioning with separate X/Y scaling:
```csharp
Canvas.SetLeft(image, guiElement.Position[0] * SCALE_FACTOR_X - (guiElement.Radius * SCALE_FACTOR_X));
Canvas.SetTop(image, guiElement.Position[1] * SCALE_FACTOR_Y - (guiElement.Radius * SCALE_FACTOR_Y));
```

### 3. Update Image Sizing
In `LoadImageAsync`:
```csharp
double scaledWidth = element.Radius * 2 * SCALE_FACTOR_X;
double scaledHeight = element.Radius * 2 * SCALE_FACTOR_Y;

return new Image
{
    Source = bitmap,
    Width = scaledWidth,
    Height = scaledHeight,
    Stretch = Microsoft.UI.Xaml.Media.Stretch.Uniform
};
```

### 4. Update Canvas Clone Method
In the ProfilePreview class's CloneCanvas method:
```csharp
private static Canvas CloneCanvas(Canvas original)
{
    Canvas clone = new Canvas
    {
        Width = CANVAS_WIDTH,
        Height = CANVAS_HEIGHT,
        ...
    };

    foreach (UIElement child in original.Children)
    {
        if (child is Image originalImage)
        {
            Image clonedImage = new Image
            {
                Source = originalImage.Source,
                Width = originalImage.Width,
                Height = originalImage.Height,
                Stretch = originalImage.Stretch
            };

            Canvas.SetLeft(clonedImage, Canvas.GetLeft(originalImage));
            Canvas.SetTop(clonedImage, Canvas.GetTop(originalImage));

            clone.Children.Add(clonedImage);
        }
    }

    return clone;
}
```

## Expected Results
1. Profile previews will use the same dimensions as the editor (560x400)
2. Elements will be positioned consistently with the editor
3. Element sizes will scale properly
4. Preview will look identical to how it appears in the editor

## Testing Plan
1. Test with profiles containing different types of elements
2. Verify positions match between preview and editor
3. Check element sizes are consistent
4. Ensure popup view maintains proper scaling
5. Test with profiles using head tilt elements
# Profile Editor Implementation

## Overview
The Profile Editor allows users to create and customize control profiles by dragging and dropping buttons onto a canvas. Users can save and load profiles, which are compatible with the existing profile format used by the core Motion Input software.

## Key Components

### Models

#### EditorButton
- Represents a button that can be placed on the canvas
- Properties:
  - Name
  - IconPath
  - TriggeredIconPath
  - Size
  - Category
  - Action
  - IsDefault

#### ButtonPositionInfo
- Tracks a button's position and layout information on the canvas
- Properties:
  - Button (EditorButton)
  - Position
  - SnapPosition
  - Size

### Controls

#### ResizableImage
- Custom control that allows resizing through corner grips
- Features:
  - Grip visibility on hover
  - Aspect ratio preservation during resize
  - Size constraints
  - Smooth animations

### View Model (ProfileEditorViewModel)

#### Properties
- DefaultButtons: Pre-defined Xbox controller buttons
- CustomButtons: User-created buttons from IconStudio
- CanvasButtons: Buttons currently placed on the canvas
- ProfileName
- ValidationMessage
- IsGridSnapEnabled

#### Commands
- NewProfile: Clears the current profile
- SaveProfile: Saves profile to JSON file
- LoadProfile: Loads profile from JSON file
- AddButtonToCanvas: Adds button to canvas at specific position

#### Key Methods
- ConvertToGuiElement: Converts ButtonPositionInfo to GuiElement for saving
- ConvertFromGuiElement: Converts GuiElement to ButtonPositionInfo when loading
- CreateDefaultActionConfig: Creates default button press action
- UpdateButtonPosition: Updates button position with optional grid snapping

### Profile JSON Structure

```json
{
  "config": {
    "grid_snap": "true|false"
  },
  "gui": [
    {
      "file": "button_name",
      "pos": [x, y],
      "radius": 30,
      "skin": "path/to/normal/icon.png",
      "triggered_skin": "path/to/triggered/icon.png",
      "action": {
        "class": "XboxController",
        "method": "PressButton",
        "args": ["button_name"]
      }
    }
  ],
  "poses": [],
  "speech": {}
}
```

## Workflow

1. User opens Profile Editor
2. Drags buttons from sidebar onto canvas
3. Positions and resizes buttons as needed
4. Optionally enables grid snapping for precise placement
5. Enters profile name
6. Saves profile

## Implementation Notes

### Grid Snapping
- Grid size is 20x20 pixels
- When enabled, button positions snap to nearest grid intersection
- Snap positions are cached to improve performance

### Drag and Drop
- Uses WinUI drag-drop APIs
- Supports both default and custom buttons
- Validates dropped content
- Shows preview feedback during drag

### File Management
- Profiles saved to user's Documents/MotionInput/Profiles directory
- Uses Newtonsoft.Json for compatibility with existing profile format
- Maintains all required profile sections (gui, poses, speech, config)

### UI/UX Considerations
- Snap-to-grid toggle for precise placement
- Visual feedback during drag operations
- Animated button placement
- Button resizing maintains aspect ratio
- Validation messages for error states

## Future Enhancements

1. Preview mode to test button layouts
2. Undo/redo functionality
3. Custom action editor integration
4. Button categories and filtering
5. Template/preset support
6. Profile sharing capabilities

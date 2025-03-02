# Profile Editor Implementation Plan

## 1. UI Component Structure

### 1.1 Canvas Area
- **Canvas Control**
  - Drop zone for buttons
  - Grid-based layout system
  - Visual feedback for valid/invalid drop zones
  - Support for button repositioning
  - Preview of button placement during drag

### 1.2 Sidebar Components
- **Button Categories**
  ```xaml
  <TabControl>
      <TabItem Header="Default Buttons">
          <!-- Xbox controller buttons -->
      </TabItem>
      <TabItem Header="Custom Buttons">
          <!-- User-created buttons -->
      </TabItem>
  </TabControl>
  ```
- **Search & Filter Section**
  - Search box for finding buttons
  - Category filter dropdown
  - Button type filter (Default/Custom)

### 1.3 Profile Management Controls
- Save profile button
- Profile name input field
- Load existing profile button
- Home navigation button
- Info/Help button

## 2. Data Models and ViewModels

### 2.1 Extended Button Model
```csharp
public class EditorButton
{
    public string Id { get; set; }
    public string DisplayName { get; set; }
    public string IconPath { get; set; }
    public ButtonType Type { get; set; } // Default or Custom
    public string Category { get; set; }
    public Point Position { get; set; }
    public ActionConfig Action { get; set; }
}

public enum ButtonType
{
    Default,
    Custom
}
```

### 2.2 ProfileEditorViewModel
```csharp
public class ProfileEditorViewModel : ObservableObject
{
    private ObservableCollection<EditorButton> _availableButtons;
    private ObservableCollection<EditorButton> _canvasButtons;
    private string _profileName;
    private bool _isDragging;
    private EditorButton _selectedButton;

    // Button Management
    public void AddButtonToCanvas(EditorButton button, Point position);
    public void RemoveButtonFromCanvas(EditorButton button);
    public void UpdateButtonPosition(EditorButton button, Point newPosition);

    // Profile Management
    public Task SaveProfileAsync();
    public Task LoadProfileAsync(string profileName);
    
    // Search and Filter
    public void FilterButtons(string searchText, string category);
}
```

## 3. Implementation Phases

### Phase 1: Basic Canvas and Drag-Drop Implementation

1. Enable Dragging
   ```xaml
   <!-- Make buttons draggable -->
   <Button x:Name="DraggableButton" CanDrag="True" />
   ```
   - Set `CanDrag` property on button elements
   - Implement `DragStarting` event to construct data package
   - Handle `DropCompleted` event for cleanup/feedback

2. Enable Dropping
   ```xaml
   <!-- Canvas as drop target -->
   <Canvas x:Name="EditorCanvas" 
           AllowDrop="True" 
           Background="Transparent"
           DragOver="Canvas_DragOver"
           Drop="Canvas_Drop" />
   ```
   - Set `AllowDrop` property on canvas
   - Ensure canvas has non-null background
   - Set up drop zones with proper visual feedback

3. Implement Drag Events
   ```csharp
   private void Canvas_DragOver(object sender, DragEventArgs e)
   {
       // Specify supported operations
       e.AcceptedOperation = DataPackageOperation.Copy;
       
       // Customize drag UI
       e.DragUIOverride.Caption = "Drop to place button";
       e.DragUIOverride.IsCaptionVisible = true;
   }

   private async void Canvas_Drop(object sender, DragEventArgs e)
   {
       if (e.DataView.Contains(StandardDataFormats.Text))
       {
           var point = e.GetPosition(EditorCanvas);
           var buttonData = await e.DataView.GetTextAsync();
           await AddButtonToCanvas(buttonData, point);
       }
   }
   ```

4. Position Tracking
   - Implement precise pointer position tracking
   - Add grid snapping functionality (optional)
   - Support button repositioning within canvas

5. Button Removal
   - Implement drag-to-remove functionality
   - Add delete button/context menu option
   - Provide visual feedback during removal

### Phase 2: Button Management
1. Create default button templates
2. Implement button categories
3. Add search and filter functionality
4. Setup button preview system

### Phase 3: Profile Integration
1. Implement profile saving
   - Convert canvas buttons to GuiElements
   - Generate proper JSON structure
2. Add profile loading
   - Parse existing profiles
   - Recreate button layout
3. Add validation and error handling

### Phase 4: UI Polish
1. Add visual feedback
   - Drop zone highlighting
   - Invalid placement indicators
   - Button hover effects
2. Implement smooth animations
3. Add help tooltips
4. Improve accessibility

## 4. JSON Structure Integration

Convert between EditorButton and GuiElement:
```csharp
public static class ProfileConverter
{
    public static GuiElement ToGuiElement(EditorButton button)
    {
        return new GuiElement
        {
            File = button.IconPath,
            Position = new List<int> { (int)button.Position.X, (int)button.Position.Y },
            Radius = 30, // Default or configurable
            Skin = button.IconPath,
            TriggeredSkin = button.IconPath, // Could be different for pressed state
            Action = button.Action
        };
    }

    public static Profile CreateProfile(string name, List<EditorButton> buttons)
    {
        return new Profile
        {
            Name = name,
            GlobalConfig = new Dictionary<string, string>(),
            GuiElements = buttons.Select(ToGuiElement).ToList(),
            Poses = new List<PoseConfig>(),
            SpeechCommands = new Dictionary<string, SpeechCommand>()
        };
    }
}
```

## 5. Performance and UX Considerations

### 5.1 Performance Optimization
- Use virtualization for button lists in sidebar
- Implement efficient hit testing for canvas interactions
- Cache button previews and icons
- Optimize drag-and-drop operations using system handles
- Use incremental updates for canvas rendering

### 5.2 User Experience Guidelines
- Follow Windows drag-drop conventions:
  - Press-hold-and-pan for touch
  - Press-and-pan for mouse/stylus
- Provide clear visual feedback:
  ```csharp
  private void CustomizeDragUI(DragEventArgs e, string operation)
  {
      e.DragUIOverride.Caption = $"{operation} button";
      e.DragUIOverride.SetContentFromBitmapImage(buttonPreview);
      e.DragUIOverride.IsCaptionVisible = true;
      e.DragUIOverride.IsContentVisible = true;
      e.DragUIOverride.IsGlyphVisible = true;
  }
  ```
- Handle touch disambiguation:
  - Context menu appears after 500ms hold
  - Immediate drag if moved within 500ms
- Support accessibility features

## 6. Windows-Specific Drag and Drop Implementation

### 6.1 Core Components
```csharp
// Required namespaces
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;

public class DragDropManager
{
    // Data package handling
    private void InitializeDataPackage(DragStartingEventArgs e, EditorButton button)
    {
        var package = e.Data;
        package.RequestedOperation = DataPackageOperation.Copy;
        package.SetText(JsonConvert.SerializeObject(button));
        
        // Add custom properties for internal drag-drop
        package.Properties.Add("ButtonType", button.Type.ToString());
        package.Properties.Add("ButtonId", button.Id);
    }

    // Touch and pointer event handling
    private void SetupTouchHandling(UIElement element)
    {
        // 500ms threshold for touch disambiguation
        element.Holding += (s, e) => 
        {
            if (!_isDragging && e.HoldingState == HoldingState.Started)
            {
                StartDrag(s as UIElement, e.GetPosition(null));
            }
        };
    }
}
```

### 6.2 Modern Drag Drop Features
1. Multi-Device Support
   - Works on touch, pen, and mouse devices
   - Seamless app-to-app transfer
   - Desktop-to-app and app-to-desktop support

2. Visual Feedback System
   ```csharp
   private void ConfigureDragVisuals(DragEventArgs e)
   {
       var dragUI = e.DragUIOverride;
       dragUI.IsGlyphVisible = true;
       dragUI.IsContentVisible = true;
       dragUI.Caption = GetDragCaption();
       
       // Update glyph based on operation
       switch (e.AcceptedOperation)
       {
           case DataPackageOperation.Copy:
               dragUI.SetContentFromBitmapImage(GetCopyGlyph());
               break;
           case DataPackageOperation.Move:
               dragUI.SetContentFromBitmapImage(GetMoveGlyph());
               break;
       }
   }
   ```

3. Error Prevention
   - Validate drop targets
   - Handle invalid operations gracefully
   - Provide clear user feedback

## 7. Error Handling and Testing

1. Unit Tests
   - Button conversion logic
   - Profile validation
   - JSON serialization/deserialization

2. Integration Tests
   - Drag and drop functionality
   - Profile saving/loading
   - Button management operations

3. UI Tests
   - Layout responsiveness
   - User interaction flows
   - Accessibility compliance

# Profile Editor GUI Element Unification Plan

## Overview
Unify the handling of GUI elements and poses in the Profile Editor by treating them as a single type that differentiates based on the presence of landmarks.

## Current Implementation
- Two separate types: GuiElement and PoseGuiElement
- Separate collections in ProfileEditorViewModel: CanvasButtons and CanvasPoses
- Different handling for saving and loading each type

## Requirements
1. GUI Elements:
   - Default file: button
   - User-configurable: position and radius
   - Action configuration:
     * Default class: ds4_gamepad
     * Methods/args: from gamepad.py

2. Poses:
   - Default file: hit_trigger.py
   - Predefined landmarks: [LEFT_WRIST, RIGHT_WRIST, LEFT_ELBOW, RIGHT_ELBOW, LEFT_SHOULDER, RIGHT_SHOULDER]
   - Action configuration:
     * Default class: ds4_gamepad
     * Methods: same as GUI elements

## Implementation Plan

### 1. Model Changes
```csharp
public struct UnifiedGuiElement
{
    [JsonProperty("file")]
    public string File { get; set; }

    [JsonProperty("pos")]
    public List<int> Position { get; set; }

    [JsonProperty("radius")]
    public int Radius { get; set; }

    [JsonProperty("skin")]
    public string Skin { get; set; }

    [JsonProperty("action")]
    public ActionConfig Action { get; set; }

    [JsonProperty("landmarks")]
    public List<string>? Landmarks { get; set; }

    [JsonIgnore]
    public bool IsPose => Landmarks != null && Landmarks.Any();
}
```

### 2. ViewModel Changes
- Replace separate collections with unified collection:
  ```csharp
  public ObservableCollection<UnifiedPositionInfo> CanvasElements { get; }
  ```
- Update conversion methods:
  * ConvertToGuiElement -> ConvertToUnifiedElement
  * ConvertFromGuiElement -> ConvertFromUnifiedElement
- Modify save/load logic to maintain backward compatibility
- Add automatic type switching based on landmark selection

### 3. JSON Compatibility
- Maintain existing JSON structure with "gui" and "poses" arrays
- On Save:
  * Split unified collection based on IsPose property
  * Save non-pose elements to "gui" array
  * Save pose elements to "poses" array
- On Load:
  * Load both arrays
  * Combine into unified collection
  * Set landmarks appropriately

### 4. UI Changes
- Update UI to handle unified element type
- Add landmark selection UI
- Automatically switch between GUI/pose based on landmark selection
- Create new ActionConfigurationDialog control:
  ```mermaid
  classDiagram
      class ActionConfigurationDialog {
          +ObservableCollection<string> AvailableMethods
          +string SelectedClass
          +string SelectedMethod
          +ObservableCollection<string> Arguments
          +bool IsDialogOpen
          +ICommand SaveCommand
          +ICommand CancelCommand
          -void UpdateArgumentInputs()
          -void ValidateInputs()
      }
  ```

### 5. Action Configuration Control
1. Create new XAML control in Controls folder:
   - Path: `Controls/ActionConfigurationDialog.xaml`
   - Functionality:
     * Method selection dropdown
     * Dynamic argument inputs based on selected method
     * Validation of inputs
     * Preview of resulting action

2. Layout:
   ```xaml
   <Grid>
     <!-- Class Selection -->
     <ComboBox SelectedItem="{Binding SelectedClass}"/>
     
     <!-- Method Selection -->
     <ComboBox ItemsSource="{Binding AvailableMethods}"
               SelectedItem="{Binding SelectedMethod}"/>
     
     <!-- Dynamic Arguments Panel -->
     <ItemsControl ItemsSource="{Binding Arguments}">
       <!-- Different input types based on method -->
     </ItemsControl>
     
     <!-- Save/Cancel Buttons -->
     <StackPanel Orientation="Horizontal">
       <Button Command="{Binding SaveCommand}"/>
       <Button Command="{Binding CancelCommand}"/>
     </StackPanel>
   </Grid>
   ```

3. Functionality:
   - Default class: ds4_gamepad
   - Available methods from gamepad.py:
     * Button operations: down, up, hold, press, toggle
     * Joystick controls: left/right with x,y parameters
     * Trigger controls: left/right with value parameter
   - Dynamic argument inputs based on method:
     * Button selection for button operations
     * Numeric inputs for joystick/trigger values
     * Duration/interval inputs for hold/combo operations

## Migration Steps
1. Create new UnifiedGuiElement struct
2. Create new UnifiedPositionInfo class
3. Create ActionConfigurationDialog control
4. Update ProfileEditorViewModel with unified collection
5. Update save/load logic
6. Update UI elements
7. Test conversion of existing profiles
8. Add landmark selection functionality
9. Implement action configuration dialog functionality

## Testing Plan
1. Test creation of new elements
2. Test switching between GUI and pose types
3. Test saving and loading profiles
4. Test backward compatibility with existing profiles
5. Test automatic file type selection based on landmarks

## Future Considerations
- Consider migration path for existing profiles
- Consider UI improvements for landmark selection
- Consider adding validation for landmark configurations

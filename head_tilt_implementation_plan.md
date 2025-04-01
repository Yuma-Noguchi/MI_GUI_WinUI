# Head Tilt Skin Selection Implementation Plan

## UI Changes (HeadTiltConfigurationDialog.xaml)
1. Add two ComboBoxes for skin selection:
   - Left skin selector
   - Right skin selector
2. Display skin preview images
3. Add to existing XAML layout below Sensitivity/Deadzone controls

## ViewModel Changes (HeadTiltConfigurationViewModel.cs)
1. Add new properties:
```csharp
public ObservableCollection<EditorButton> AvailableButtons { get; }
public EditorButton? SelectedLeftSkin { get; set; }
public EditorButton? SelectedRightSkin { get; set; }
```

2. Update Configure method:
```csharp
public void Configure(PoseGuiElement element, IEnumerable<EditorButton> buttons, Action<PoseGuiElement> onSave)
{
    AvailableButtons.Clear();
    foreach (var button in buttons)
    {
        AvailableButtons.Add(button);
    }

    // Set selected skins based on element
    SelectedLeftSkin = AvailableButtons.FirstOrDefault(b => 
        b.FileName == Utils.FileNameHelper.ConvertToAssetsRelativePath(element.LeftSkin));
    SelectedRightSkin = AvailableButtons.FirstOrDefault(b => 
        b.FileName == Utils.FileNameHelper.ConvertToAssetsRelativePath(element.RightSkin));
    ...
}
```

3. Update Save method to include selected skins:
```csharp
var headTiltElement = new PoseGuiElement
{
    ...
    LeftSkin = SelectedLeftSkin?.FileName ?? "racing/left_arrow.png",
    RightSkin = SelectedRightSkin?.FileName ?? "racing/right_arrow.png",
    ...
};
```

## Calling Code Changes (ProfileEditorViewModel.cs)
1. Pass button collections to dialog:
```csharp
var allButtons = DefaultButtons.Concat(CustomButtons).ToList();
_headTiltConfigurationDialog.Configure(poseElement, allButtons, UpdateHeadTiltElement);
```

## Testing Plan
1. Verify default skins are selected for new head tilt
2. Test skin selection from both default and custom buttons
3. Verify skin selections are preserved when:
   - Saving and loading profiles
   - Toggling head tilt enable/disable
   - Switching between profiles
4. Test UI responsiveness and image preview updates

## Implementation Order
1. Update HeadTiltConfigurationDialog.xaml with new UI elements
2. Update HeadTiltConfigurationViewModel with skin selection support
3. Modify ProfileEditorViewModel to pass button collections
4. Test all scenarios
5. Handle edge cases (missing images, invalid selections)
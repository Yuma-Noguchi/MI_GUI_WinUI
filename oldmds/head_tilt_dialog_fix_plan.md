# Head Tilt Dialog Implementation Fix Plan

## Alignment with ActionConfiguration Pattern

### 1. Dialog State Management (HeadTiltConfigurationDialog.cs)
```csharp
public sealed partial class HeadTiltConfigurationDialog : ContentDialog
{
    private ElementTheme _currentTheme = ElementTheme.Default;

    public HeadTiltConfigurationDialog(HeadTiltConfigurationViewModel viewModel)
    {
        ViewModel = viewModel;
        this.InitializeComponent();
        this.PrimaryButtonClick += HeadTiltConfigurationDialog_PrimaryButtonClick;
    }

    public HeadTiltConfigurationViewModel ViewModel { get; }

    public new XamlRoot? XamlRoot
    {
        get => base.XamlRoot;
        set
        {
            base.XamlRoot = value;
            if (value?.Content is FrameworkElement element)
            {
                _currentTheme = element.ActualTheme;
                this.RequestedTheme = _currentTheme;
            }
        }
    }
}
```

### 2. ViewModel State Management (HeadTiltConfigurationViewModel.cs)
```csharp
public partial class HeadTiltConfigurationViewModel : ViewModelBase
{
    private PoseGuiElement _element = new();
    private Action<PoseGuiElement>? _onSave;

    [ObservableProperty]
    private bool isDialogOpen;

    [ObservableProperty]
    private string validationMessage = string.Empty;

    [ObservableProperty]
    private ObservableCollection<EditorButton> availableButtons = new();

    [ObservableProperty]
    private EditorButton? selectedLeftSkin;

    [ObservableProperty]
    private EditorButton? selectedRightSkin;

    [ObservableProperty]
    private double sensitivity = 0.75;

    [ObservableProperty]
    private double deadzone = 1.0;

    [ObservableProperty]
    private bool linear = false;

    [ObservableProperty]
    private bool isEnabled = false;

    public void Configure(PoseGuiElement? element, IEnumerable<EditorButton> buttons, Action<PoseGuiElement> onSave)
    {
        _element = element ?? new PoseGuiElement();
        _onSave = onSave ?? throw new ArgumentNullException(nameof(onSave));

        // Update available buttons
        AvailableButtons.Clear();
        foreach (var button in buttons)
        {
            AvailableButtons.Add(button);
        }

        // Load element settings
        IsEnabled = !string.IsNullOrEmpty(element?.File);
        Sensitivity = element?.Sensitivity ?? 0.75;
        Deadzone = element?.Deadzone ?? 1.0;
        Linear = element?.Linear ?? false;

        // Set selected skins
        SetupSkinSelection(element);

        ValidationMessage = string.Empty;
        IsDialogOpen = true;
    }

    [RelayCommand]
    private void Save()
    {
        string error = ValidateInputs();
        if (!string.IsNullOrEmpty(error))
        {
            ValidationMessage = error;
            return;
        }

        var updatedElement = new PoseGuiElement
        {
            File = IsEnabled ? "head_tilt_joystick.py" : string.Empty,
            LeftSkin = SelectedLeftSkin?.FileName ?? "racing/left_arrow.png",
            RightSkin = SelectedRightSkin?.FileName ?? "racing/right_arrow.png",
            Sensitivity = Sensitivity,
            Deadzone = Deadzone,
            Linear = Linear
        };

        _onSave?.Invoke(updatedElement);
        IsDialogOpen = false;
    }

    private string ValidateInputs()
    {
        if (IsEnabled)
        {
            if (Sensitivity < 0.1 || Sensitivity > 1.0)
                return "Sensitivity must be between 0.1 and 1.0";

            if (Deadzone < 0.1 || Deadzone > 5.0)
                return "Deadzone must be between 0.1 and 5.0";
        }
        return string.Empty;
    }
}
```

### 3. Changes from Original Plan
1. Simpler dialog state management
   - Removed initialization tracking
   - Using IsDialogOpen for state
   - Better theme handling

2. Better ViewModel pattern
   - Consistent property notifications
   - Clear validation logic
   - State encapsulation

3. Profile Editor Integration
   - Use IsDialogOpen for visibility
   - Handle XamlRoot at dialog level
   - Single point of state management

### 4. ProfileEditorViewModel Updates
```csharp
[RelayCommand]
private async Task ConfigureHeadTilt()
{
    await ExecuteWithErrorHandlingAsync(async () =>
    {
        if (XamlRoot == null)
        {
            throw new InvalidOperationException("XamlRoot is not available");
        }

        _headTiltConfigurationDialog.XamlRoot = XamlRoot;
        var poseElement = GetHeadTiltConfiguration();
        var availableButtons = GetAvailableButtons();

        _headTiltConfigurationDialog.Configure(poseElement, availableButtons, UpdateHeadTiltElement);
    });
}
```

### 5. Implementation Order
1. Update ViewModel with new state management
2. Update dialog with theme handling
3. Update ProfileEditorViewModel integration
4. Add validation logic
5. Test state transitions

### 6. Testing Scenarios
1. Dialog opening/closing
2. Theme consistency
3. Validation behavior
4. State preservation
5. Error handling
6. XamlRoot handling
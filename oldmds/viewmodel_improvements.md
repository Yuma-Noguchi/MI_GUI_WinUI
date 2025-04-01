# ActionStudioViewModel Improvements

## Current Issues
1. Currently inherits from ObservableObject instead of ViewModelBase
2. No window management implementation
3. No proper lifecycle management (InitializeAsync, Cleanup)
4. No standard error handling pattern through ExecuteWithErrorHandlingAsync

## Required Changes

### 1. Base Class and Constructor
```csharp
public partial class ActionStudioViewModel : ViewModelBase
{
    private readonly IActionService _actionService;
    private XamlRoot? _xamlRoot;

    public ActionStudioViewModel(
        IActionService actionService,
        ILogger<ActionStudioViewModel> logger,
        INavigationService navigationService)
        : base(logger, navigationService)
    {
        _actionService = actionService ?? throw new ArgumentNullException(nameof(actionService));
    }
}
```

### 2. Window Management
```csharp
protected override void OnWindowChanged()
{
    base.OnWindowChanged();
    if (Window != null)
    {
        _xamlRoot = Window.Content?.XamlRoot;
    }
    else
    {
        _xamlRoot = null;
    }
}
```

### 3. Initialization and Cleanup
```csharp
public override async Task InitializeAsync()
{
    await base.InitializeAsync();
    await LoadActionsAsync();
}

public override void Cleanup()
{
    try
    {
        Actions.Clear();
        SelectedAction = null;
        _xamlRoot = null;

        base.Cleanup();
        _logger.LogInformation("Cleaned up ActionStudioViewModel resources");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error during cleanup");
    }
}
```

### 4. Error Handling
```csharp
protected override async Task ShowErrorAsync(string message)
{
    ErrorMessage = message;
    if (_xamlRoot != null)
    {
        await DialogHelper.ShowError(message, _xamlRoot);
    }
}
```

### 5. Update Command Methods
All async command methods should use ExecuteWithErrorHandlingAsync. For example:

```csharp
[RelayCommand]
private async Task SaveSequence()
{
    await ExecuteWithErrorHandlingAsync(async () =>
    {
        if (SelectedAction == null)
        {
            throw new InvalidOperationException("No action selected");
        }

        if (string.IsNullOrWhiteSpace(SelectedAction.Name))
        {
            throw new InvalidOperationException("Please enter an action name");
        }

        if (!SelectedAction.Sequence.Any())
        {
            throw new InvalidOperationException("Please add at least one button to the sequence");
        }

        await _actionService.SaveActionAsync(SelectedAction);
        
        // Update or add the action in the list
        var existingIndex = Actions.ToList().FindIndex(a => a.Name == SelectedAction.Name);
        if (existingIndex >= 0)
        {
            Actions[existingIndex] = SelectedAction;
        }
        else if (!Actions.Contains(SelectedAction))
        {
            Actions.Add(SelectedAction);
        }

        if (_xamlRoot != null)
        {
            await DialogHelper.ShowMessage($"Action '{SelectedAction.Name}' saved successfully.", "Success", _xamlRoot);
        }
    }, nameof(SaveSequence));
}
```

## Implementation Steps
1. Update base class and constructor
2. Add window management code
3. Implement InitializeAsync and Cleanup methods
4. Add ShowErrorAsync override
5. Wrap all command methods with ExecuteWithErrorHandlingAsync
6. Remove direct error message assignments and replace with ShowErrorAsync calls
7. Update LoadActionsAsync to use proper initialization pattern
8. Add proper disposal of resources in Cleanup method

## Benefits
1. Consistent error handling across the application
2. Proper resource management and cleanup
3. Standardized window reference management
4. Better initialization control
5. Improved logging and error reporting
# SelectProfilesPage ViewModel Initialization Issue

## Root Cause Analysis

The initialization issue stems from several architectural problems in the navigation and dependency injection setup:

1. **Navigation Pattern Issue**
   - The NavigationService.Navigate<T>() method only navigates to the page type
   - No ViewModel is being passed as a parameter during navigation
   - SelectProfilesPage's parameterless constructor is being called directly
   - The ViewModel instance from DI is never connected to the page

2. **DI Configuration Gap**
   - SelectProfilesViewModel is registered as singleton in App.xaml.cs
   - But the page is not retrieving it from DI container in its constructor
   - The page tries to get ViewModel from navigation parameters which are always null

3. **Window Reference Issue**
   - SelectProfilesViewModel needs Window reference for some operations
   - The Window property is never being set during navigation
   - This causes potential null reference issues in various operations

## Current Flow

1. HomePage button click -> `_navigationService.Navigate<SelectProfilesPage>()`
2. NavigationService creates new SelectProfilesPage using parameterless constructor
3. SelectProfilesPage.OnNavigatedTo tries to get ViewModel from null parameters
4. No ViewModel is ever injected or initialized properly

## Solution

### 1. Update NavigationService

```csharp
public interface INavigationService
{
    bool Navigate<TPage, TViewModel>(object? parameter = null) 
        where TPage : Page
        where TViewModel : class;
}

public class NavigationService : INavigationService
{
    public bool Navigate<TPage, TViewModel>(object? parameter = null)
        where TPage : Page
        where TViewModel : class
    {
        if (_frame == null) return false;

        // Get ViewModel from DI
        var viewModel = Ioc.Default.GetRequiredService<TViewModel>();
        
        // Navigate with ViewModel as parameter
        return _frame.Navigate(typeof(TPage), viewModel);
    }
}
```

### 2. Update HomePage Navigation

```csharp
SelectProfilesButton.Click += (s, e) => 
    _navigationService.Navigate<SelectProfilesPage, SelectProfilesViewModel>();
```

### 3. Update SelectProfilesPage

```csharp
public sealed partial class SelectProfilesPage : Page
{
    private SelectProfilesViewModel? _viewModel;

    public SelectProfilesPage()
    {
        this.InitializeComponent();
        
        // Get ViewModel from DI if not provided through navigation
        if (_viewModel == null)
        {
            _viewModel = Ioc.Default.GetRequiredService<SelectProfilesViewModel>();
            SetViewModel(_viewModel);
        }
    }

    private void SetViewModel(SelectProfilesViewModel viewModel)
    {
        _viewModel = viewModel;
        _viewModel.Window = this.XamlRoot.ContentWindow;
        this.DataContext = _viewModel;
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        try
        {
            // If we got ViewModel through navigation, use it
            if (e.Parameter is SelectProfilesViewModel vm)
            {
                SetViewModel(vm);
            }

            if (_viewModel != null)
            {
                await _viewModel.InitializeAsync();
                _viewModel.GenerateGuiElementsPreviews();
            }
        }
        catch (Exception ex)
        {
            if (_viewModel != null)
            {
                _viewModel.ErrorMessage = "Error initializing page.";
            }
        }
    }
}
```

### 4. Update SelectProfilesViewModel

```csharp
public partial class SelectProfilesViewModel : ObservableObject
{
    private readonly IWindowManager _windowManager;
    private WeakReference<Window>? _windowReference;

    public Window? Window
    {
        get => _windowReference?.TryGetTarget(out var window) == true ? window : null;
        set
        {
            if (value != null)
            {
                _windowReference = new WeakReference<Window>(value);
                OnPropertyChanged(nameof(Window));
            }
        }
    }

    public SelectProfilesViewModel(
        IWindowManager windowManager,
        ProfileService profileService,
        ILogger<SelectProfilesViewModel> logger)
    {
        _windowManager = windowManager;
        _profileService = profileService;
        _logger = logger;
    }
}
```

## Implementation Steps

1. **Update Dependencies**
   - Modify NavigationService to support ViewModel injection
   - Update WindowManager to handle window references properly

2. **Update Page Initialization**
   - Ensure ViewModel is always available
   - Set Window reference correctly
   - Handle initialization errors

3. **Testing**
   - Verify navigation flow
   - Test window state persistence
   - Validate error handling

## Additional Recommendations

1. Consider implementing a proper MVVM navigation framework like Microsoft.Toolkit.Mvvm

2. Use proper lifecycle management:
   - OnNavigatedTo for initialization
   - OnNavigatedFrom for cleanup
   - Clear separation between view and ViewModel responsibilities

3. Implement proper error handling and logging throughout the navigation process

4. Add diagnostic logging to track navigation and initialization flow
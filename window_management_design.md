# Single-Window Navigation Design for WinUI MVVM Application

## Overview
This design implements a modern single-window application pattern with NavigationView control, providing a consistent and fluid navigation experience between all application sections including studio pages.

## Core Components

### 1. Required NuGet Packages
```xml
<PackageReference Include="Microsoft.WindowsAppSDK" Version="latest" />
<PackageReference Include="Microsoft.Toolkit.Mvvm" Version="latest" />
<PackageReference Include="CommunityToolkit.WinUI" Version="latest" />
```

### 2. Main Window Structure

```xaml
<!-- MainWindow.xaml -->
<Window>
    <NavigationView x:Name="NavView"
                    PaneDisplayMode="Left"
                    IsBackButtonVisible="Visible"
                    IsSettingsVisible="False">
        
        <!-- Navigation Menu Items -->
        <NavigationView.MenuItems>
            <NavigationViewItem Icon="Home" Content="Profiles" Tag="SelectProfiles"/>
            <NavigationViewItem Icon="Edit" Content="Action Studio" Tag="ActionStudio"/>
            <NavigationViewItem Icon="Pictures" Content="Icon Studio" Tag="IconStudio"/>
        </NavigationView.MenuItems>

        <!-- Main content -->
        <Frame x:Name="ContentFrame"/>
        
    </NavigationView>
</Window>
```

### 3. Navigation Service

```csharp
public interface INavigationService
{
    bool Navigate<T>(object parameter = null) where T : Page;
    bool GoBack();
    bool CanGoBack { get; }
}

public class NavigationService : INavigationService
{
    private readonly Frame _frame;
    private readonly NavigationView _navigationView;

    public NavigationService(Frame frame, NavigationView navigationView)
    {
        _frame = frame;
        _navigationView = navigationView;
        
        // Handle navigation view selection
        _navigationView.SelectionChanged += NavigationView_SelectionChanged;
        // Handle back button
        _navigationView.BackRequested += NavigationView_BackRequested;
    }

    private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItemContainer is NavigationViewItem selectedItem)
        {
            string tag = (string)selectedItem.Tag;
            Type pageType = GetPageTypeFromTag(tag);
            _frame.Navigate(pageType);
        }
    }
}
```

### 4. Page Registration

```csharp
// App.xaml.cs
public partial class App : Application
{
    public App()
    {
        Services = ConfigureServices();
        this.InitializeComponent();
    }

    public static IServiceProvider Services { get; private set; }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Register core services
        services.AddSingleton<INavigationService, NavigationService>();
        
        // Register pages
        services.AddTransient<SelectProfilesPage>();
        services.AddTransient<ActionStudioPage>();
        services.AddTransient<IconStudioPage>();
        services.AddTransient<ProfileEditorPage>();

        // Register view models
        services.AddTransient<SelectProfilesViewModel>();
        services.AddTransient<ActionStudioViewModel>();
        services.AddTransient<IconStudioViewModel>();
        services.AddTransient<ProfileEditorViewModel>();

        return services.BuildServiceProvider();
    }
}
```

### 5. View Model Implementation

```csharp
public class MainWindowViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;
    private string _headerText;

    public string HeaderText
    {
        get => _headerText;
        set => SetProperty(ref _headerText, value);
    }

    public MainWindowViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
    }

    // Navigation commands if needed
    public ICommand NavigateToProfilesCommand => new RelayCommand(() => 
        _navigationService.Navigate<SelectProfilesPage>());
}
```

## Benefits

1. **Consistent User Experience**
   - Single window interface
   - Familiar navigation pattern
   - Smooth transitions between sections

2. **Efficient Resource Management**
   - No multiple windows to manage
   - Built-in page caching
   - Optimized memory usage

3. **Modern UI/UX**
   - Left navigation pane
   - Back navigation support
   - Clean, professional look

4. **Developer-Friendly**
   - Simple navigation logic
   - MVVM compliance
   - Easy to maintain

## Implementation Steps

1. **Update MainWindow**
   - Add NavigationView control
   - Configure navigation items
   - Set up content frame

2. **Configure Navigation**
   - Implement navigation service
   - Set up page routing
   - Handle back navigation

3. **Update Pages**
   - Ensure all pages work with frame navigation
   - Implement proper page caching
   - Handle page state management

4. **Implement ViewModels**
   - Add navigation logic
   - Handle page parameters
   - Manage state between navigations

## Best Practices

1. **Page Caching**
```csharp
// In page constructor
public ActionStudioPage()
{
    this.NavigationCacheMode = NavigationCacheMode.Enabled;
}
```

2. **State Management**
```csharp
// Handle suspension and resume
protected override void OnNavigatedFrom(NavigationEventArgs e)
{
    // Save page state
}

protected override void OnNavigatedTo(NavigationEventArgs e)
{
    // Restore page state
}
```

3. **Error Handling**
```csharp
public class NavigationService : INavigationService
{
    public bool Navigate<T>(object parameter = null) where T : Page
    {
        try
        {
            return _frame.Navigate(typeof(T), parameter);
        }
        catch (Exception ex)
        {
            // Log error and handle gracefully
            return false;
        }
    }
}
```

## Conclusion

This single-window design with NavigationView provides:
- Clean, modern user interface
- Efficient navigation between all application sections
- Proper state management
- Easy maintenance and extensibility
- Consistent user experience across the application
# Profile Editor Implementation Plan

## Phase 1: Basic Structure Implementation

### ProfileEditorViewModel

```csharp
public partial class ProfileEditorViewModel : ViewModelBase
{
    private readonly ILogger<ProfileEditorViewModel> _logger;
    private readonly ProfileService _profileService;
    private readonly WindowManager _windowManager;
    
    // Observable Properties
    [ObservableProperty]
    private string _title = "Profile Editor";
    
    [ObservableProperty]
    private Profile? _currentProfile;
    
    [ObservableProperty]
    private ObservableCollection<ButtonTemplate> _defaultButtons;
    
    [ObservableProperty]
    private ObservableCollection<ButtonTemplate> _customButtons;
    
    [ObservableProperty]
    private string? _errorMessage;
    
    [ObservableProperty]
    private bool _isLoading;
    
    [ObservableProperty]
    private string _searchText = string.Empty;
    
    // Canvas Properties
    [ObservableProperty]
    private Canvas _previewCanvas;
    
    [ObservableProperty]
    private bool _showGrid;
    
    [ObservableProperty]
    private bool _snapToGrid;
    
    // Constructor injection following DI pattern
    public ProfileEditorViewModel(
        ILogger<ProfileEditorViewModel> logger,
        INavigationService navigationService,
        ProfileService profileService,
        WindowManager windowManager) : base(logger, navigationService)
    {
        _logger = logger;
        _profileService = profileService;
        _windowManager = windowManager;
    }
    
    // Initialize pattern from architecture
    public override async Task InitializeAsync()
    {
        try
        {
            IsLoading = true;
            await LoadButtonTemplatesAsync();
            InitializeCanvas();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing ProfileEditor");
            ErrorMessage = "Failed to initialize editor.";
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    // Cleanup pattern from architecture
    public override void Cleanup()
    {
        // Clear collections
        DefaultButtons.Clear();
        CustomButtons.Clear();
        
        // Clear canvas
        PreviewCanvas.Children.Clear();
        
        // Reset state
        CurrentProfile = null;
        ErrorMessage = null;
    }
}
```

### Page XAML Structure

```xaml
<Page x:Class="MI_GUI_WinUI.Pages.ProfileEditorPage"
      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
      xmlns:controls="using:MI_GUI_WinUI.Controls">

    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>
        
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="300"/> <!-- Button sidebar -->
            <ColumnDefinition Width="*"/>   <!-- Canvas area -->
            <ColumnDefinition Width="250"/> <!-- Properties panel -->
        </Grid.ColumnDefinitions>
        
        <!-- Header -->
        <controls:PageHeader Grid.Row="0" Grid.ColumnSpan="3" 
                           Title="{x:Bind ViewModel.Title, Mode=OneWay}"/>
        
        <!-- Button Sidebar -->
        <Grid Grid.Row="1" Grid.Column="0">
            <!-- Implementation in Phase 2 -->
        </Grid>
        
        <!-- Canvas Area -->
        <Grid Grid.Row="1" Grid.Column="1">
            <!-- Implementation in Phase 2 -->
        </Grid>
        
        <!-- Properties Panel -->
        <Grid Grid.Row="1" Grid.Column="2">
            <!-- Implementation in Phase 2 -->
        </Grid>
        
        <!-- Loading Overlay -->
        <Grid Grid.RowSpan="2" Grid.ColumnSpan="3" 
              Visibility="{x:Bind ViewModel.IsLoading, Mode=OneWay}">
            <ProgressRing IsActive="{x:Bind ViewModel.IsLoading, Mode=OneWay}"/>
        </Grid>
        
        <!-- Error Message -->
        <InfoBar Grid.RowSpan="2" Grid.ColumnSpan="3"
                IsOpen="{x:Bind ViewModel.ErrorMessage, Mode=OneWay, Converter={StaticResource StringToBoolConverter}}"
                Message="{x:Bind ViewModel.ErrorMessage, Mode=OneWay}"
                Severity="Error"/>
    </Grid>
</Page>
```

### Page Code-Behind

```csharp
public sealed partial class ProfileEditorPage : Page
{
    private ProfileEditorViewModel? _viewModel;
    
    public ProfileEditorPage()
    {
        this.InitializeComponent();
        
        // Get ViewModel from DI
        if (_viewModel == null)
        {
            _viewModel = Ioc.Default.GetRequiredService<ProfileEditorViewModel>();
            SetViewModel(_viewModel);
        }
    }
    
    private void SetViewModel(ProfileEditorViewModel viewModel)
    {
        _viewModel = viewModel;
        if (Application.Current is App app)
        {
            var windowManager = app.Services.GetRequiredService<WindowManager>();
            _viewModel.Window = windowManager.MainWindow;
        }
        this.DataContext = _viewModel;
    }
    
    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        
        try
        {
            if (_viewModel != null)
            {
                await _viewModel.InitializeAsync();
            }
        }
        catch (Exception ex)
        {
            if (_viewModel != null)
            {
                _viewModel.ErrorMessage = $"Error initializing page: {ex.Message}";
            }
        }
    }
    
    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        _viewModel?.Cleanup();
        _viewModel = null;
    }
}
```

## Next Steps

After implementing this basic structure, we'll proceed with:

1. Button Templates Model Implementation
2. Drag-Drop Infrastructure
3. Canvas Interaction Logic
4. Properties Panel Implementation

Each phase will follow the same architectural patterns:
- ViewModels inherit from ViewModelBase
- Use ObservableObject and ObservableProperty attributes
- Implement proper error handling and logging
- Follow resource management guidelines
- Use dependency injection for services

Once this implementation plan is approved, we can switch to Code mode to begin the implementation.
# Implementation Guide for Architecture Improvements

This guide provides detailed implementation steps and code examples for each improvement area identified in the architecture improvement plan.

## 1. Dependency Injection Setup

### Step 1: Install Required Packages
```bash
dotnet add package Microsoft.Extensions.DependencyInjection
dotnet add package Microsoft.Extensions.Configuration
```

### Step 2: Configure Services
```csharp
// App.xaml.cs
public partial class App : Application
{
    private IServiceProvider _serviceProvider;

    public App()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Core Services
        services.AddSingleton<ILoggingService, LoggingService>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IWindowManager, WindowManager>();

        // Business Services
        services.AddSingleton<IMotionInputService, MotionInputService>();
        services.AddSingleton<IActionService, ActionService>();
        services.AddSingleton<IProfileService, ProfileService>();

        // Repositories
        services.AddSingleton<IProfileRepository, FileSystemProfileRepository>();
        services.AddSingleton<IActionRepository, FileSystemActionRepository>();
        services.AddSingleton<IUnitOfWork, FileSystemUnitOfWork>();

        // ViewModels
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<ProfileEditorViewModel>();
        services.AddTransient<SelectProfilesViewModel>();
    }
}
```

## 2. Repository Pattern Implementation

### Step 1: Create Base Repository Interface
```csharp
// Interfaces/IRepository.cs
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(string id);
    Task<IEnumerable<T>> GetAllAsync();
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(string id);
}
```

### Step 2: Implement Profile Repository
```csharp
// Repositories/FileSystemProfileRepository.cs
public class FileSystemProfileRepository : IProfileRepository
{
    private readonly string _basePath;
    private readonly ILoggingService _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public FileSystemProfileRepository(
        IConfiguration config,
        ILoggingService logger)
    {
        _basePath = config["Profiles:BasePath"];
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<Profile?> GetByNameAsync(string name)
    {
        var path = Path.Combine(_basePath, $"{name}.json");
        if (!File.Exists(path))
            return null;

        try
        {
            var json = await File.ReadAllTextAsync(path);
            return JsonSerializer.Deserialize<Profile>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error loading profile {name}", ex);
            throw;
        }
    }

    public async Task SaveAsync(Profile profile)
    {
        var path = Path.Combine(_basePath, $"{profile.Name}.json");
        try
        {
            var json = JsonSerializer.Serialize(profile, _jsonOptions);
            await File.WriteAllTextAsync(path, json);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error saving profile {profile.Name}", ex);
            throw;
        }
    }
}
```

## 3. Unit of Work Pattern Implementation

### Step 1: Create Unit of Work Interface
```csharp
// Interfaces/IUnitOfWork.cs
public interface IUnitOfWork : IDisposable
{
    IProfileRepository Profiles { get; }
    IActionRepository Actions { get; }
    Task BeginTransactionAsync();
    Task CommitAsync();
    Task RollbackAsync();
}
```

### Step 2: Implement File System Unit of Work
```csharp
// Data/FileSystemUnitOfWork.cs
public class FileSystemUnitOfWork : IUnitOfWork
{
    private readonly IProfileRepository _profiles;
    private readonly IActionRepository _actions;
    private readonly ILoggingService _logger;
    private bool _isTransaction;

    public FileSystemUnitOfWork(
        IProfileRepository profiles,
        IActionRepository actions,
        ILoggingService logger)
    {
        _profiles = profiles;
        _actions = actions;
        _logger = logger;
    }

    public IProfileRepository Profiles => _profiles;
    public IActionRepository Actions => _actions;

    public async Task BeginTransactionAsync()
    {
        if (_isTransaction)
            throw new InvalidOperationException("Transaction already in progress");

        _isTransaction = true;
        // Create backup copies of files being modified
    }

    public async Task CommitAsync()
    {
        if (!_isTransaction)
            throw new InvalidOperationException("No transaction in progress");

        try
        {
            // Remove backup files
            _isTransaction = false;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error committing transaction", ex);
            await RollbackAsync();
            throw;
        }
    }

    public async Task RollbackAsync()
    {
        if (!_isTransaction)
            return;

        // Restore from backup files
        _isTransaction = false;
    }
}
```

## 4. ViewModel Refactoring Example

### Step 1: Create Base ViewModel
```csharp
// ViewModels/ViewModelBase.cs
public abstract class ViewModelBase : ObservableObject
{
    protected readonly ILoggingService Logger;
    protected readonly INavigationService Navigation;

    protected ViewModelBase(
        ILoggingService logger,
        INavigationService navigation)
    {
        Logger = logger;
        Navigation = navigation;
    }

    public virtual Task InitializeAsync()
    {
        return Task.CompletedTask;
    }
}
```

### Step 2: Split SelectProfilesViewModel
```csharp
// ViewModels/Profiles/ProfileListViewModel.cs
public class ProfileListViewModel : ViewModelBase
{
    private readonly IProfileService _profileService;
    private ObservableCollection<ProfilePreview> _profiles;
    
    [ObservableProperty]
    private string _searchText;

    partial void OnSearchTextChanged(string value)
    {
        UpdateFilteredProfiles();
    }

    private void UpdateFilteredProfiles()
    {
        // Filter implementation
    }
}

// ViewModels/Profiles/ProfilePreviewViewModel.cs
public class ProfilePreviewViewModel : ViewModelBase
{
    private readonly IProfileService _profileService;
    
    [ObservableProperty]
    private ProfilePreview _selectedProfile;

    [RelayCommand]
    private async Task GeneratePreviewAsync()
    {
        // Preview generation implementation
    }
}
```

## 5. Validation Implementation

### Step 1: Install FluentValidation
```bash
dotnet add package FluentValidation
```

### Step 2: Create Validators
```csharp
// Validators/ProfileValidator.cs
public class ProfileValidator : AbstractValidator<Profile>
{
    public ProfileValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(50)
            .Must(ProfileNameHelper.IsValidProfileName)
            .WithMessage("Profile name contains invalid characters");

        RuleFor(x => x.Elements)
            .NotNull()
            .Must(x => x.Count > 0)
            .WithMessage("Profile must contain at least one element");

        RuleForEach(x => x.Elements)
            .SetValidator(new GuiElementValidator());
    }
}

// Validators/GuiElementValidator.cs
public class GuiElementValidator : AbstractValidator<UnifiedGuiElement>
{
    public GuiElementValidator()
    {
        RuleFor(x => x.Radius)
            .GreaterThan(0)
            .LessThanOrEqualTo(100);

        RuleFor(x => x.Action)
            .SetValidator(new ActionConfigValidator())
            .When(x => x.Action != null);
    }
}
```

### Step 3: Integrate Validation
```csharp
// Services/ValidationService.cs
public interface IValidationService
{
    Task<ValidationResult> ValidateAsync<T>(T entity) where T : class;
}

public class ValidationService : IValidationService
{
    private readonly IServiceProvider _serviceProvider;

    public ValidationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<ValidationResult> ValidateAsync<T>(T entity) where T : class
    {
        var validator = _serviceProvider.GetRequiredService<IValidator<T>>();
        return await validator.ValidateAsync(entity);
    }
}
```

## 6. Testing Setup

### Step 1: Create Test Project
```bash
dotnet new xunit -n MI_GUI_WinUI.Tests.Unit
dotnet add MI_GUI_WinUI.Tests.Unit package Moq
dotnet add MI_GUI_WinUI.Tests.Unit package FluentAssertions
```

### Step 2: Implement Test Classes
```csharp
// Tests/ViewModels/ProfileEditorViewModelTests.cs
public class ProfileEditorViewModelTests : ViewModelTestBase
{
    private readonly Mock<IProfileService> _profileService;
    private readonly Mock<IValidationService> _validationService;
    private readonly ProfileEditorViewModel _viewModel;

    public ProfileEditorViewModelTests()
    {
        _profileService = new Mock<IProfileService>();
        _validationService = new Mock<IValidationService>();

        _viewModel = new ProfileEditorViewModel(
            _profileService.Object,
            NavigationService.Object,
            LoggingService.Object,
            _validationService.Object);
    }

    [Fact]
    public async Task SaveProfile_WithValidProfile_ShouldSucceed()
    {
        // Arrange
        var profile = new Profile { Name = "Test" };
        _validationService
            .Setup(x => x.ValidateAsync(It.IsAny<Profile>()))
            .ReturnsAsync(new ValidationResult());

        // Act
        await _viewModel.SaveProfileCommand.ExecuteAsync(null);

        // Assert
        _profileService.Verify(
            x => x.SaveProfileAsync(It.IsAny<Profile>()),
            Times.Once);
    }
}
```

## Migration Strategy

1. Create backup of existing codebase
2. Implement dependency injection infrastructure
3. Create and test repositories one at a time
4. Refactor ViewModels incrementally
5. Add validation layer
6. Create unit tests for new components
7. Run integration tests
8. Deploy changes in phases

## Notes

- Maintain backward compatibility during migration
- Add logging for all critical operations
- Document all new patterns and practices
- Create migration scripts for existing data
- Monitor performance metrics during migration
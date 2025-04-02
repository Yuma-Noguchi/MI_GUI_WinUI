# Navigation Service Technical Specification

## Overview
This document outlines the changes required to enhance the Navigation Service with proper ViewModel lifecycle management and window handling capabilities.

## Current Implementation
The current INavigationService interface provides basic navigation functionality:
- Frame initialization
- Basic page navigation
- Back navigation
- Navigation event handling

## Required Changes

### 1. Interface Updates

The INavigationService interface needs to be updated to include:

```csharp
public interface INavigationService
{
    // Existing members
    bool CanGoBack { get; }
    void Initialize(Frame frame);
    bool Navigate<T>(object? parameter = null) where T : Page;
    bool GoBack();
    event EventHandler<string> NavigationChanged;

    // New members
    bool Navigate<TPage, TViewModel>(Window window, object? parameter = null)
        where TPage : Page
        where TViewModel : ViewModelBase;
    
    Task<bool> NavigateAsync<TPage, TViewModel>(Window window, object? parameter = null)
        where TPage : Page
        where TViewModel : ViewModelBase;
        
    void RegisterWindow(Window window);
    void UnregisterWindow(Window window);
}
```

### 2. Implementation Changes

The NavigationService implementation should:

1. Handle Window Management:
   - Track active windows
   - Associate ViewModels with their respective windows
   - Clean up resources when windows are closed

2. Manage ViewModel Lifecycle:
   - Initialize ViewModels asynchronously
   - Handle cleanup when navigating away
   - Support window-specific ViewModels

3. Error Handling:
   - Log navigation failures
   - Provide detailed error information
   - Support graceful fallback

## Implementation Steps

1. Update INavigationService interface
2. Create ViewModelBase class
3. Modify NavigationService implementation
4. Update window registration in WindowManager
5. Implement proper cleanup in navigation events

## Dependencies

- Microsoft.UI.Xaml
- Microsoft.Extensions.Logging
- CommunityToolkit.Mvvm

## Error Handling

The service should handle:
- Failed navigation attempts
- ViewModel initialization failures
- Window registration/unregistration errors
- Resource cleanup failures

## Migration Strategy

1. Create new interface and implementation
2. Update existing ViewModels gradually
3. Test each migration step
4. Roll out changes incrementally

## Testing Requirements

- Test window lifecycle management
- Verify ViewModel initialization
- Test memory cleanup
- Validate error handling
# Profile Editor Implementation Plan

## 1. Overview

The Profile Editor allows users to create and customize control profiles by dragging and dropping buttons onto a canvas. This document outlines the implementation approach and key components.

## 2. Architecture

### 2.1 Core Components

- **ProfileEditorPage (View)**: Main UI with drag-drop canvas and button sidebar
- **ProfileEditorViewModel**: Manages UI state and profile operations
- **Models**:
  - `Profile`: Core data model for serialization
  - `GuiElement`: Button configuration for JSON serialization
  - `DraggableGuiElement`: UI wrapper for interactive elements
  - `ActionConfig`: Button action configuration

### 2.2 Design Patterns

- MVVM pattern for separation of concerns
- Command pattern for user actions
- Repository pattern for profile storage

## 3. Implementation Details

### 3.1 Model Layer

#### Profile Model
- Represents the complete profile configuration
- Contains collections of GUI elements, poses, and speech commands
- Handles JSON serialization/deserialization
- Maps to Motion Input's profile format

#### GuiElement
- Represents a button's properties for serialization
- Properties:
  - File: Button image file
  - Position: X,Y coordinates
  - Radius: Button size
  - Skin: Visual appearance
  - Action: Associated command

#### DraggableGuiElement
- Wraps GuiElement with UI functionality
- Manages visual representation
- Handles drag-drop operations
- Maintains UI state (dragging, selected, etc.)

### 3.2 View Layer

#### Main Layout
- Three-column design:
  1. Button sidebar (left)
  2. Canvas area (center)
  3. Properties panel (right)

#### Interactive Features
- Drag and drop from sidebar to canvas
- Grid system with snap-to-grid option
- Canvas zooming and panning
- Button repositioning
- Visual feedback during interactions

### 3.3 ViewModel Layer

#### State Management
- Tracks active profile
- Manages button collections:
  - Default buttons
  - Custom buttons
  - Canvas elements
- Handles loading/saving operations

#### User Operations
- Profile saving
- Button placement
- Grid toggling
- Canvas clearing
- Element deletion

## 4. Data Flow

1. **Loading**
   - Initialize empty canvas
   - Load button templates
   - Set up drag-drop handlers

2. **Button Creation**
   - User drags from sidebar
   - Create DraggableGuiElement
   - Add to canvas collection
   - Update visual representation

3. **Profile Saving**
   - Collect canvas elements
   - Convert to GuiElement format
   - Create Profile object
   - Serialize to JSON
   - Save to file system

## 5. User Interaction Flow

1. **Creating a Profile**
   - Enter profile name
   - Drag buttons from sidebar
   - Position on canvas
   - Configure properties
   - Save profile

2. **Editing Elements**
   - Select element on canvas
   - Adjust properties
   - Move/resize as needed
   - Delete if required

## 6. File Structure

```
MI_GUI_WinUI/
├── Pages/
│   ├── ProfileEditorPage.xaml
│   └── ProfileEditorPage.xaml.cs
├── ViewModels/
│   └── ProfileEditorViewModel.cs
└── Models/
    ├── Profile.cs
    ├── GuiElement.cs
    └── DraggableGuiElement.cs
```

## 7. Future Enhancements

1. **Element Properties**
   - Advanced configuration panel
   - Action studio integration
   - Custom skins support

2. **Canvas Features**
   - Multi-select
   - Alignment tools
   - Undo/redo support
   - Copy/paste functionality

3. **Profile Management**
   - Profile templates
   - Import/export
   - Version control
   - Backup/restore

## 8. Testing Strategy

1. **Unit Tests**
   - Model serialization
   - ViewModel logic
   - Command execution

2. **Integration Tests**
   - Profile saving/loading
   - UI interaction flows
   - Data persistence

3. **UI Tests**
   - Drag-drop operations
   - Visual feedback
   - Layout responsiveness

## 9. Dependencies

- WinUI 3 for UI framework
- CommunityToolkit.Mvvm for MVVM support
- System.Text.Json for serialization
- Microsoft.Extensions.Logging for logging

This implementation plan serves as a living document and will be updated as the project evolves.
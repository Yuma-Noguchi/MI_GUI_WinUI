# Icon Studio UI/UX Specification

## Overview
The Icon Studio provides an intuitive interface for users to generate and manage icons using AI, with both text-to-image and image-to-image capabilities.

## Layout Structure

### Main Layout
```
+------------------------------------------+
|                 Header                    |
+------------------------------------------+
|  +----------------+  +------------------+ |
|  |    Input       |  |    Preview      | |
|  |    Section     |  |    Section      | |
|  |                |  |                 | |
|  +----------------+  +------------------+ |
|                                          |
|  +----------------------------------------
|  |           Generation History          | |
|  |                                      | |
|  +----------------------------------------
+------------------------------------------+
```

## Component Details

### 1. Header Section
```xaml
<controls:PageHeader>
    <Grid>
        <TextBlock Text="Icon Studio" Style="{StaticResource TitleTextBlockStyle}"/>
        <CommandBar DefaultLabelPosition="Right">
            <AppBarButton Icon="Save" Label="Save All"/>
            <AppBarButton Icon="Delete" Label="Clear History"/>
            <AppBarButton Icon="Help" Label="Tutorial"/>
        </CommandBar>
    </Grid>
</controls:PageHeader>
```

### 2. Input Section
```xaml
<StackPanel Spacing="16">
    <!-- Mode Selection -->
    <ToggleSwitch Header="Input Mode" 
                  OnContent="Text to Icon" 
                  OffContent="Image to Icon"/>
    
    <!-- Text Input -->
    <TextBox x:Name="PromptInput"
             Header="Describe your icon"
             PlaceholderText="E.g., A minimalist blue star with soft glow"
             TextWrapping="Wrap"
             AcceptsReturn="True"
             Height="100"/>
    
    <!-- Image Input (Collapsed when in text mode) -->
    <Grid x:Name="ImageInputSection">
        <Button Content="Upload Image"
                Style="{StaticResource AccentButtonStyle}"/>
        <Image x:Name="SourceImage"
               MaxHeight="200"/>
    </Grid>
    
    <!-- Generation Settings -->
    <Expander Header="Advanced Settings">
        <StackPanel Spacing="8">
            <NumberBox Header="Size"
                      Value="512"
                      SpinButtonPlacementMode="Compact"/>
            <Slider Header="Style Strength"
                    Minimum="1"
                    Maximum="20"
                    Value="7"/>
            <ComboBox Header="Style"
                     PlaceholderText="Select style">
                <ComboBoxItem Content="Minimalist"/>
                <ComboBoxItem Content="Realistic"/>
                <ComboBoxItem Content="Cartoon"/>
                <ComboBoxItem Content="Sketch"/>
            </ComboBox>
        </StackPanel>
    </Expander>
    
    <!-- Generate Button -->
    <Button Content="Generate Icon"
            Style="{StaticResource AccentButtonStyle}"
            HorizontalAlignment="Stretch"/>
</StackPanel>
```

### 3. Preview Section
```xaml
<Grid>
    <!-- Loading State -->
    <ProgressRing IsActive="{Binding IsGenerating}"
                  Width="64" Height="64"/>
    
    <!-- Preview Display -->
    <Grid Visibility="{Binding HasResult}">
        <Image Source="{Binding PreviewImage}"
               MaxHeight="400"
               MaxWidth="400"/>
        
        <!-- Action Buttons -->
        <StackPanel VerticalAlignment="Bottom"
                   HorizontalAlignment="Center"
                   Orientation="Horizontal"
                   Spacing="8">
            <Button Content="Save"
                    Style="{StaticResource AccentButtonStyle}"/>
            <Button Content="Improve"/>
            <Button Content="Discard"/>
        </StackPanel>
    </Grid>
</Grid>
```

### 4. Generation History
```xaml
<GridView ItemsSource="{Binding GeneratedIcons}"
          SelectionMode="Single">
    <GridView.ItemTemplate>
        <DataTemplate>
            <Grid Width="150" Height="150">
                <Image Source="{Binding ImageSource}"
                       Stretch="Uniform"/>
                <StackPanel VerticalAlignment="Bottom"
                          Background="{ThemeResource CardBackgroundFillColorDefaultBrush}"
                          Padding="8">
                    <TextBlock Text="{Binding Name}"
                             Style="{StaticResource CaptionTextBlockStyle}"/>
                </StackPanel>
            </Grid>
        </DataTemplate>
    </GridView.ItemTemplate>
</GridView>
```

## Interaction States

### 1. Initial State
- Empty preview area
- Input mode defaulted to "Text to Icon"
- Generate button disabled until input provided
- Empty generation history

### 2. Input State
- Generate button enables when:
  - Text input has content (Text mode)
  - Image is uploaded (Image mode)
- Real-time validation of input
- Dynamic prompt suggestions

### 3. Generating State
```xaml
<OverlayPanel IsOpen="{Binding IsGenerating}">
    <StackPanel>
        <ProgressRing IsActive="True"/>
        <TextBlock Text="{Binding GenerationStatus}"
                   Style="{StaticResource BodyTextBlockStyle}"/>
    </StackPanel>
</OverlayPanel>
```

### 4. Preview State
- Generated icon displayed
- Action buttons enabled
- Save dialog appears on Save
- Improve opens prompt enhancement dialog
- Discard returns to input state

## Dialogs

### 1. Save Dialog
```xaml
<ContentDialog Title="Save Icon">
    <StackPanel>
        <TextBox Header="Icon Name"
                 Text="{Binding IconName}"/>
        <TextBox Header="Tags (optional)"
                 PlaceholderText="Enter tags separated by commas"/>
    </StackPanel>
</ContentDialog>
```

### 2. Improve Dialog
```xaml
<ContentDialog Title="Improve Icon">
    <StackPanel>
        <TextBox Header="Additional Details"
                 PlaceholderText="What would you like to change?"/>
        <Slider Header="Change Strength"
                Minimum="0.1"
                Maximum="1.0"
                Value="0.5"/>
    </StackPanel>
</ContentDialog>
```

## Animations

### 1. Generation Transitions
```xaml
<Storyboard x:Name="GenerationTransition">
    <DoubleAnimation
        Storyboard.TargetName="PreviewSection"
        Storyboard.TargetProperty="Opacity"
        From="0" To="1" Duration="0:0:0.3"/>
</Storyboard>
```

### 2. History Updates
```xaml
<GridView.ItemContainerTransitions>
    <TransitionCollection>
        <AddDeleteThemeTransition/>
        <ReorderThemeTransition/>
    </TransitionCollection>
</GridView.ItemContainerTransitions>
```

## Error States

### 1. Generation Error
```xaml
<InfoBar Title="Generation Failed"
         IsOpen="{Binding HasError}"
         Message="{Binding ErrorMessage}"
         Severity="Error">
    <InfoBar.ActionButton>
        <Button Content="Retry"/>
    </InfoBar.ActionButton>
</InfoBar>
```

### 2. Input Validation
```xaml
<TextBox.ContextFlyout>
    <Flyout>
        <TextBlock Text="{Binding ValidationMessage}"
                   Foreground="{ThemeResource SystemFillColorCriticalBrush}"/>
    </Flyout>
</TextBox.ContextFlyout>
```

## Accessibility

1. **Keyboard Navigation**
   - Tab order follows logical flow
   - Keyboard shortcuts for common actions
   - Focus indicators for all interactive elements

2. **Screen Readers**
   - AutomationProperties.Name for all controls
   - Live region updates for generation status
   - Descriptive error messages

3. **High Contrast**
   - Theme-aware colors
   - Sufficient contrast ratios
   - Visual indicators preserved in high contrast

## Responsive Design

1. **Compact View** (< 640px)
   - Stack input and preview vertically
   - Collapse advanced settings
   - Reduce history items per row

2. **Medium View** (640px - 1007px)
   - Side-by-side input and preview
   - Expanded settings panel
   - 3 history items per row

3. **Wide View** (≥ 1008px)
   - Full layout with spacious margins
   - Always visible settings
   - 4+ history items per row

This UI specification provides a comprehensive guide for implementing the Icon Studio interface, ensuring consistency, usability, and accessibility across all features.
# UI Polish Plan

## Overview
This document outlines the comprehensive UI polish plan for the Motion Input Configuration application. The goal is to establish consistent styling, improve visual hierarchy, and enhance user experience across all pages while following best UI design principles.

## 1. Consistent Layout

### Page Margins
- Standard margin of 36 pixels (L) on all pages
- Consistent grid spacing of 24 pixels (M)
- Remove inconsistent margins (e.g., 20px currently used in ActionStudioPage)

### Grid System
- Implement standardized Grid.RowDefinitions and Grid.ColumnDefinitions
- Use consistent spacing:
  ```xaml
  <Grid RowSpacing="24" ColumnSpacing="24">
  ```
- Maintain proper content alignment across sections

## 2. Visual Hierarchy

### Typography Scale
- Page Titles: `DisplayTextBlockStyle`
  ```xaml
  <TextBlock Text="Page Title" Style="{StaticResource DisplayTextBlockStyle}"/>
  ```
- Section Headers: `SubtitleTextBlockStyle`
  ```xaml
  <TextBlock Text="Section Header" Style="{StaticResource SubtitleTextBlockStyle}"/>
  ```
- Body Text: `BodyTextBlockStyle`
  ```xaml
  <TextBlock Text="Content" Style="{StaticResource BodyTextBlockStyle}"/>
  ```

### Content Grouping
- Use Borders for content sections with consistent styling:
  ```xaml
  <Border BorderBrush="{ThemeResource CardStrokeColorDefaultBrush}"
          BorderThickness="1"
          CornerRadius="4"
          Padding="16">
  ```
- Group related controls with proper spacing

## 3. Component Consistency

### Button Styles
- Primary Actions: `AccentButtonStyle`
  ```xaml
  <Button Style="{StaticResource AccentButtonStyle}"/>
  ```
- Secondary Actions: Default button style
- Icon Buttons: `TextBlockButtonStyle`
  ```xaml
  <Button Style="{StaticResource TextBlockButtonStyle}">
      <FontIcon FontFamily="{StaticResource SymbolThemeFontFamily}" Glyph="&#xE74D;"/>
  </Button>
  ```

### List Items
- Consistent ListViewItem style:
  ```xaml
  <Style TargetType="ListViewItem">
      <Setter Property="Margin" Value="0,4"/>
      <Setter Property="Padding" Value="12,8"/>
      <Setter Property="BorderThickness" Value="1"/>
      <Setter Property="BorderBrush" Value="{ThemeResource CardStrokeColorDefaultBrush}"/>
      <Setter Property="Background" Value="{ThemeResource CardBackgroundFillColorDefaultBrush}"/>
      <Setter Property="CornerRadius" Value="4"/>
  </Style>
  ```

### Form Controls
- TextBox styling with consistent margins:
  ```xaml
  <TextBox Margin="0,0,0,20"/>
  ```
- ComboBox width standardization:
  ```xaml
  <ComboBox Width="200"/>
  ```

## 4. Spacing System

### Standard Spacing Scale
- XS: 8px (small component spacing)
- S: 16px (internal padding)
- M: 24px (section spacing)
- L: 36px (page margins)

### Implementation
```xaml
<!-- Component Spacing -->
<StackPanel Spacing="8"/>  <!-- XS -->
<Border Padding="16"/>    <!-- S -->
<Grid RowSpacing="24"/>   <!-- M -->
<Grid Margin="36"/>       <!-- L -->
```

## 5. Responsive Design

### Grid System
- Use star-sized columns for flexibility:
  ```xaml
  <Grid.ColumnDefinitions>
      <ColumnDefinition Width="300"/> <!-- Fixed sidebar -->
      <ColumnDefinition Width="*"/>   <!-- Flexible content -->
  </Grid.ColumnDefinitions>
  ```

### Adaptive Layouts
- Ensure components scale appropriately:
  ```xaml
  <Button HorizontalAlignment="Stretch"/>
  ```
- Use relative sizing where appropriate:
  ```xaml
  <RowDefinition Height="Auto"/>
  <RowDefinition Height="*"/>
  ```

## Implementation Strategy

1. Create Shared Resources
   - Define common styles in App.xaml
   - Implement spacing as resources

2. Page Updates Priority
   - HomePage.xaml
   - ActionStudioPage.xaml
   - IconStudioPage.xaml
   - ProfileEditorPage.xaml
   - SelectProfilesPage.xaml

3. Testing
   - Verify consistency across all pages
   - Test responsiveness at different window sizes
   - Ensure proper spacing and alignment

## Next Steps

1. Switch to Code mode to implement the shared resource dictionary
2. Apply consistent styling across all pages
3. Test and refine the implementation
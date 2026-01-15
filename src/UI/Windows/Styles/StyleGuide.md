# OSDP Bench UI Style Guide

## Overview
This style guide provides a comprehensive design system for OSDP Bench, ensuring visual consistency and maintainability across the application.

## Design System Structure

### 1. Design Tokens (`DesignTokens.xaml`)
Foundation values for spacing, typography, colors, and sizing.

#### Spacing System (8px grid)
```xml
{StaticResource Spacing.XSmall}    <!-- 4px -->
{StaticResource Spacing.Small}     <!-- 8px -->
{StaticResource Spacing.Medium}    <!-- 16px -->
{StaticResource Spacing.Large}     <!-- 24px -->
{StaticResource Spacing.XLarge}    <!-- 32px -->
{StaticResource Spacing.XXLarge}   <!-- 48px -->
```

#### Standard Margins & Padding
```xml
{StaticResource Margin.Card}       <!-- 10,5 -->
{StaticResource Margin.Control}    <!-- 0,0,16,0 -->
{StaticResource Margin.Button}     <!-- 8,4 -->
{StaticResource Padding.Card}      <!-- 16 -->
{StaticResource Padding.Control}   <!-- 8,4 -->
```

#### Typography Scale
```xml
{StaticResource FontSize.Caption}  <!-- 12px -->
{StaticResource FontSize.Body}     <!-- 14px -->
{StaticResource FontSize.BodyLarge} <!-- 16px -->
{StaticResource FontSize.Subtitle} <!-- 20px -->
{StaticResource FontSize.Title}    <!-- 24px -->
{StaticResource FontSize.Headline} <!-- 32px -->
{StaticResource FontSize.Display}  <!-- 36px -->
```

### 2. Component Styles (`ComponentStyles.xaml`)
Reusable styles for UI components.

#### Typography Usage
```xml
<!-- Page titles -->
<TextBlock Style="{StaticResource Page.Title}" Text="Page Name"/>

<!-- Section headers -->
<TextBlock Style="{StaticResource Text.Title}" Text="Section"/>

<!-- Body text -->
<TextBlock Style="{StaticResource Text.Body}" Text="Content"/>

<!-- Status messages -->
<TextBlock Style="{StaticResource Text.Error}" Text="Error message"/>
<TextBlock Style="{StaticResource Text.Warning}" Text="Warning"/>
<TextBlock Style="{StaticResource Text.Success}" Text="Success"/>
```

#### Form Controls
```xml
<!-- Labels -->
<Label Style="{StaticResource Label.Standard}" Content="Field Name"/>

<!-- Text boxes -->
<TextBox Style="{StaticResource TextBox.Standard}"/>

<!-- Read-only text boxes (includes gray background, copy button, disabled context menu) -->
<TextBox IsReadOnly="True" Style="{StaticResource TextBox.Standard}"/>

<!-- Combo boxes -->
<ComboBox Style="{StaticResource ComboBox.Standard}"/>

<!-- Number boxes -->
<ui:NumberBox Style="{StaticResource NumberBox.Standard}"/>
```

#### Buttons
```xml
<!-- Primary actions -->
<ui:Button Style="{StaticResource Button.Primary}" Content="Save"/>

<!-- Secondary actions -->
<ui:Button Style="{StaticResource Button.Secondary}" Content="Cancel"/>

<!-- Transparent/subtle actions -->
<ui:Button Style="{StaticResource Button.Transparent}" Content="Browse"/>
```

#### Badges
Status badges for displaying security states and status indicators.

```xml
<!-- Filled badges (white text on colored background) -->
<Border Style="{StaticResource Badge.Success}">
    <TextBlock Text="Encrypted" Style="{StaticResource Badge.Text}"/>
</Border>

<Border Style="{StaticResource Badge.Info}">
    <TextBlock Text="Monitor" Style="{StaticResource Badge.Text}"/>
</Border>

<Border Style="{StaticResource Badge.Warning}">
    <TextBlock Text="No Decryption" Style="{StaticResource Badge.Text}"/>
</Border>

<!-- Outlined badges (colored border/text on light background - better readability) -->
<Border Style="{StaticResource Badge.Error.Outlined}">
    <TextBlock Text="Clear Text" Style="{StaticResource Badge.Text.Error}"/>
</Border>

<!-- Small variants for data grids -->
<Border Style="{StaticResource Badge.Success.Small}">
    <TextBlock Text="Encrypted" Style="{StaticResource Badge.Text.Small}"/>
</Border>

<Border Style="{StaticResource Badge.Error.Outlined.Small}">
    <TextBlock Text="Clear Text" Style="{StaticResource Badge.Text.Error.Small}"/>
</Border>
```

**Badge Styles:**
- `Badge.Success` / `Badge.Success.Small` - Green filled (secure/positive states)
- `Badge.Info` / `Badge.Info.Small` - Blue filled (informational states)
- `Badge.Warning` / `Badge.Warning.Small` - Orange filled (warning states)
- `Badge.Error.Outlined` / `Badge.Error.Outlined.Small` - Red outlined (error/insecure states)

**Badge Text Styles:**
- `Badge.Text` / `Badge.Text.Small` - White text for filled badges
- `Badge.Text.Error` / `Badge.Text.Error.Small` - Red text for outlined error badges

### 3. Layout Templates (`LayoutTemplates.xaml`)
Templates for common layout patterns.

#### Page Structure
```xml
<!-- Page container -->
<StackPanel Style="{StaticResource Page.Container}">
    <!-- Page header with activity indicators -->
    <ContentControl Content="Page Title" 
                    Template="{StaticResource Template.PageHeader}"/>
    
    <!-- Content cards -->
    <ui:Card Style="{StaticResource Card.Standard}">
        <StackPanel Style="{StaticResource Card.Content}">
            <!-- Card content -->
        </StackPanel>
    </ui:Card>
</StackPanel>
```

#### Data Grids
```xml
<!-- Standard data grid -->
<DataGrid Style="{StaticResource DataGrid.Standard}">
    <!-- Columns -->
</DataGrid>

<!-- Monitor page data grid -->
<DataGrid Style="{StaticResource DataGrid.Monitor}">
    <!-- Columns -->
</DataGrid>
```

## Implementation Guidelines

### 1. Migration Strategy
1. **Immediate adoption**: Use new styles for all new components
2. **Gradual migration**: Update existing components when making changes
3. **Legacy support**: Existing `PageTitleStyle` remains functional

### 2. Best Practices

#### ✅ Do
- Use design tokens for spacing: `Margin="{StaticResource Margin.Card}"`
- Apply semantic color styles: `Style="{StaticResource Text.Error}"`
- Follow typography hierarchy: Display → Headline → Title → Subtitle → Body
- Use standard component styles for consistency
- Leverage layout templates for common patterns

#### ❌ Don't
- Use hardcoded margins/padding values
- Mix explicit FontSize with typography styles
- Use hardcoded colors (Red, Orange, etc.)
- Create one-off styles without considering reusability
- Override TextBox MinHeight in normal layouts (use design tokens instead)
- Use StaticResource syntax within Thickness strings (e.g., `"0,0,0,{StaticResource Spacing.Medium}"`)

#### Special Cases
```xml
<!-- Height-sensitive containers (DockPanel) - override TextBox MinHeight -->
<TextBox Style="{StaticResource TextBox.Standard}"
         MinHeight="0" Height="Auto" VerticalAlignment="Center"/>

<!-- DataGrid with invisible selection -->
<ui:DataGrid CanUserSortColumns="False">
    <ui:DataGrid.RowStyle>
        <Style TargetType="DataGridRow">
            <Style.Triggers>
                <Trigger Property="IsSelected" Value="True">
                    <Setter Property="Background" Value="Transparent"/>
                    <Setter Property="BorderBrush" Value="Transparent"/>
                </Trigger>
            </Style.Triggers>
        </Style>
    </ui:DataGrid.RowStyle>
    <ui:DataGrid.CellStyle>
        <Style TargetType="DataGridCell">
            <Style.Triggers>
                <Trigger Property="IsSelected" Value="True">
                    <Setter Property="Background" Value="Transparent"/>
                    <Setter Property="BorderBrush" Value="Transparent"/>
                    <Setter Property="Foreground" Value="{DynamicResource TextFillColorPrimaryBrush}"/>
                </Trigger>
            </Style.Triggers>
        </Style>
    </ui:DataGrid.CellStyle>
</ui:DataGrid>
```

### 3. Common Patterns

#### Form Layout
```xml
<StackPanel Style="{StaticResource Section.Container}">
    <ui:TextBlock Style="{StaticResource Section.Header}" 
                  Text="{markup:Localize Section_Title}"/>
    
    <!-- Form fields -->
    <ContentControl Tag="{markup:Localize Field_Label}"
                    Template="{StaticResource Template.FormField}">
        <TextBox Style="{StaticResource TextBox.Standard}"/>
    </ContentControl>
</StackPanel>
```

#### Status Messages
```xml
<TextBlock Text="{Binding StatusMessage}">
    <TextBlock.Style>
        <Style TargetType="TextBlock" BasedOn="{StaticResource Text.Body}">
            <Style.Triggers>
                <DataTrigger Binding="{Binding StatusLevel}" Value="Error">
                    <Setter Property="Foreground" Value="{StaticResource Brush.Error}"/>
                </DataTrigger>
                <!-- Additional triggers -->
            </Style.Triggers>
        </Style>
    </TextBlock.Style>
</TextBlock>
```

### 4. Color System

#### Semantic Colors
- `{StaticResource Brush.Success}` - Green (#107C10)
- `{StaticResource Brush.Warning}` - Orange (#FF8C00)  
- `{StaticResource Brush.Error}` - Red (#D13438)
- `{StaticResource Brush.Info}` - Blue (#0078D4)

#### Activity Colors
- `{StaticResource Brush.Activity.Tx}` - Transmit activity
- `{StaticResource Brush.Activity.Rx}` - Receive activity
- `{StaticResource Brush.Activity.Inactive}` - Inactive state

#### Theme-Aware Colors (Recommended)
- `{DynamicResource TextFillColorPrimaryBrush}` - Primary text
- `{DynamicResource TextFillColorSecondaryBrush}` - Secondary text
- `{DynamicResource ControlFillColorDefaultBrush}` - Control backgrounds

## Future Enhancements

### Planned Additions
1. **Animation system** - Consistent transitions and micro-interactions
2. **Responsive breakpoints** - Adaptive layouts for different window sizes
3. **Dark mode optimizations** - Enhanced dark theme color palette
4. **Accessibility styles** - High contrast and screen reader optimizations

### Maintenance
- Review and update design tokens quarterly
- Add new component styles as needed
- Maintain backwards compatibility for existing styles
- Document any breaking changes in component updates

## Resources
- [WPF UI Documentation](https://wpfui.lepo.co/)
- [Microsoft Fluent Design](https://www.microsoft.com/design/fluent/)
- [Material Design System](https://material.io/design/introduction)
# Windows UI Updates to Replicate in MAUI

This document outlines changes made to the Windows WPF UI implementation that need to be replicated in the MAUI mobile application.

**Date:** November 7, 2025
**Commits:** `eac362e` through `62a0a6c` (4 commits ahead of origin/develop)

---

## Summary of Changes

1. Fixed disconnect button visibility for invalid security key error
2. Fixed Monitor page activation during invalid security key error state
3. Allowed Monitor page access after disconnect
4. Added packet statistics to Monitor page

---

## 1. Disconnect Button Visibility Fix

**Commit:** `eac362e` - "Fix disconnect button not visible for invalid security key error"

### Core Changes (Already Applied - Shared)
**File:** `src/Core/ViewModels/Pages/ConnectViewModel.cs`

The `DisconnectButtonVisible` property now returns `true` for error states:

```csharp
public bool DisconnectButtonVisible =>
    ConnectionStatus is ConnectionStatus.Connected or ConnectionStatus.InvalidSecurityKey;
```

**Previously:** Only visible when `ConnectionStatus.Connected`
**Now:** Visible for both `Connected` and `InvalidSecurityKey` states

### MAUI Implementation Notes
- This ViewModel change is in the shared Core library, so MAUI automatically gets this fix
- Ensure MAUI Connect page binds disconnect button visibility to `DisconnectButtonVisible`
- Test that disconnect button appears when invalid security key error occurs

### Updated Tests
**File:** `test/Core.Tests/ViewModels/ConnectViewModelTests.cs`

Added test case:
```csharp
[Fact]
public void DisconnectButton_ShouldBeVisible_WhenInvalidSecurityKey()
```

---

## 2. Monitor Page Error State Fix

**Commit:** `19576bb` - "Fix Monitor page not active during invalid security key error state"

### Core Changes (Already Applied - Shared)
**File:** `src/Core/ViewModels/Pages/MonitorViewModel.cs`

Updated `OnDeviceManagementServiceOnConnectionStatusChange` to handle `InvalidSecurityKey`:

```csharp
private void OnDeviceManagementServiceOnConnectionStatusChange(object? _, ConnectionStatus connectionStatus)
{
    if (connectionStatus == ConnectionStatus.Connected) InitializePollingMetrics();

    UpdateConnectionInfo();

    switch (connectionStatus)
    {
        case ConnectionStatus.Connected:
            StatusLevel = StatusLevel.Connected;
            break;
        case ConnectionStatus.InvalidSecurityKey:
            StatusLevel = StatusLevel.Error;  // NEW
            break;
        default:
            StatusLevel = StatusLevel.Disconnected;
            break;
    }
}
```

**Key Change:** Invalid security key now sets `StatusLevel.Error` instead of `StatusLevel.Disconnected`

### MAUI Implementation Notes
- This ViewModel change is in shared Core library, automatically available in MAUI
- Ensure MAUI Monitor page shows content when `StatusLevel` is `Error` (not just `Connected`)
- The page should be active during error states to allow user interaction

### Windows XAML Reference
**File:** `src/UI/Windows/Views/Pages/MonitorPage.xaml`

The visibility binding was updated to show content for non-secure-channel cases:
```xaml
<Border Visibility="{Binding Path=ViewModel.UsingSecureChannel,
                     Converter={StaticResource BooleanToVisibilityConverter},
                     ConverterParameter=Invert}">
```

**MAUI Equivalent:** Bind visibility to `!ViewModel.UsingSecureChannel`

### Updated Tests
**File:** `test/Core.Tests/ViewModels/MonitorViewModelTests.cs`

Added test case:
```csharp
[Fact]
public void StatusLevel_ShouldBeError_WhenInvalidSecurityKey()
```

---

## 3. Allow Monitor Page Access After Disconnect

**Commit:** `6eb7d43` - "Allow Monitor page access after disconnect"

### Core Changes
None - this was a UI-only change

### Windows XAML Changes
**File:** `src/UI/Windows/Views/Pages/MonitorPage.xaml`

**Removed:**
- "Device not connected" warning message
- Visibility constraint that only showed content when Connected/Error status

**Before:**
```xaml
<Border Visibility="{Binding Path=ViewModel.StatusLevel,
                     Converter={StaticResource StatusLevelToVisibilityConverter}}">
```

**After:**
```xaml
<!-- Content always visible, regardless of connection status -->
```

### MAUI Implementation Notes
- Remove any connection status checks that prevent Monitor page access
- Allow users to view trace history even when disconnected
- Monitor page should be accessible in all navigation states
- The secure channel warning should still be shown when applicable

### User Experience Benefits
- Users can review captured trace data after disconnecting
- Better workflow for analyzing communication sessions
- Consistent with standard monitoring tool behavior

---

## 4. Add Packet Statistics to Monitor Page

**Commit:** `62a0a6c` - "Add packet statistics to Monitor page"

### Core Changes (Already Applied - Shared)
**File:** `src/Core/ViewModels/Pages/MonitorViewModel.cs`

#### New Properties
```csharp
// Packet Statistics
[ObservableProperty] private int _commandsSent;
[ObservableProperty] private int _repliesReceived;
[ObservableProperty] private int _polls;
[ObservableProperty] private int _naks;
```

#### Line Quality Percentage Calculation
```csharp
/// <summary>
/// Line quality percentage based on commands sent vs replies received
/// Accounts for 2 in-flight commands to prevent jumping during normal operation
/// </summary>
public double LineQualityPercentage
{
    get
    {
        if (CommandsSent == 0) return 100.0;

        // Allow for 2 commands to be in-flight without penalizing quality
        int inFlight = CommandsSent - RepliesReceived;

        // If we have more than 2 commands without a reply, count the excess as failures
        int missedReplies = Math.Max(0, inFlight - 2);
        int effectiveCommandsSent = CommandsSent - Math.Min(inFlight, 2);

        if (effectiveCommandsSent == 0) return 100.0;

        int successfulCommands = RepliesReceived;
        return (successfulCommands / (double)(successfulCommands + missedReplies)) * 100.0;
    }
}

partial void OnCommandsSentChanged(int value)
{
    OnPropertyChanged(nameof(LineQualityPercentage));
}

partial void OnRepliesReceivedChanged(int value)
{
    OnPropertyChanged(nameof(LineQualityPercentage));
}
```

**Algorithm Explanation:**
- Allows up to 2 commands to be "in-flight" (sent but not yet replied to) without affecting quality
- Only penalizes quality when more than 2 commands are waiting for replies
- Prevents percentage from jumping on every command send during normal operation
- Formula: `Quality = (RepliesReceived / (RepliesReceived + MissedReplies)) * 100`

#### Statistics Tracking
Updated `OnDeviceManagementServiceOnTraceEntryReceived`:

```csharp
// Update statistics
if (traceEntry.Direction == Output)
{
    CommandsSent++;
    if (packetTraceEntry.Packet.CommandType == CommandType.Poll)
    {
        Polls++;
    }
}
else if (traceEntry.Direction == Input)
{
    RepliesReceived++;
    if (packetTraceEntry.Packet.ReplyType == ReplyType.Nak)
    {
        Naks++;
    }
}
```

#### Statistics Reset on Reconnect
Updated `InitializePollingMetrics`:

```csharp
private void InitializePollingMetrics()
{
    TraceEntriesView.Clear();
    _lastPacketEntry = null;

    // Reset statistics
    CommandsSent = 0;
    RepliesReceived = 0;
    Polls = 0;
    Naks = 0;
}
```

### Resource Strings Added
**File:** `src/Core/Resources/Resources.resx`

```xml
<data name="Monitor_CommandsSent" xml:space="preserve">
  <value>Commands Sent</value>
  <comment>Label for commands sent statistic</comment>
</data>
<data name="Monitor_RepliesReceived" xml:space="preserve">
  <value>Replies Received</value>
  <comment>Label for replies received statistic</comment>
</data>
<data name="Monitor_Polls" xml:space="preserve">
  <value>Polls</value>
  <comment>Label for poll packets statistic</comment>
</data>
<data name="Monitor_Naks" xml:space="preserve">
  <value>NAKs</value>
  <comment>Label for NAK replies statistic</comment>
</data>
<data name="Monitor_LineQuality" xml:space="preserve">
  <value>Line Quality</value>
  <comment>Label for line quality percentage statistic</comment>
</data>
```

### Windows UI Implementation
**File:** `src/UI/Windows/Views/Pages/MonitorPage.xaml`

#### Statistics Panel Layout

```xaml
<!-- Statistics Panel -->
<Border Style="{StaticResource Card.Bordered}" Margin="10 5">
    <StackPanel Orientation="Vertical" Margin="15 10">
        <!-- Line Quality Display -->
        <StackPanel Orientation="Vertical" HorizontalAlignment="Center" Margin="0 5 0 15">
            <TextBlock Text="{markup:Localize Monitor_LineQuality}"
                       Style="{StaticResource Text.Body}"
                       HorizontalAlignment="Center"
                       FontSize="{StaticResource FontSize.Body}"/>
            <TextBlock HorizontalAlignment="Center" Margin="0 5 0 0">
                <Run Text="{Binding ViewModel.LineQualityPercentage, StringFormat={}{0:F1}, Mode=OneWay}"
                     FontSize="28"
                     FontWeight="Bold"/>
                <Run Text="%"
                     FontSize="20"
                     FontWeight="Bold"/>
            </TextBlock>
        </StackPanel>

        <!-- Statistics Grid -->
        <Grid>
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>

            <!-- Commands Sent -->
            <StackPanel Grid.Column="0" Orientation="Vertical" Margin="5 0">
                <TextBlock Text="{markup:Localize Monitor_CommandsSent}"
                           Style="{StaticResource Text.Body}"
                           HorizontalAlignment="Center"
                           FontSize="{StaticResource FontSize.Caption}"
                           TextWrapping="Wrap"
                           TextAlignment="Center"/>
                <TextBlock Text="{Binding ViewModel.CommandsSent}"
                           Style="{StaticResource Text.Body}"
                           HorizontalAlignment="Center"
                           FontSize="{StaticResource FontSize.Headline}"
                           FontWeight="Bold"
                           Margin="0 5 0 0"/>
            </StackPanel>

            <!-- Replies Received -->
            <StackPanel Grid.Column="1" Orientation="Vertical" Margin="5 0">
                <TextBlock Text="{markup:Localize Monitor_RepliesReceived}"
                           Style="{StaticResource Text.Body}"
                           HorizontalAlignment="Center"
                           FontSize="{StaticResource FontSize.Caption}"
                           TextWrapping="Wrap"
                           TextAlignment="Center"/>
                <TextBlock Text="{Binding ViewModel.RepliesReceived}"
                           Style="{StaticResource Text.Body}"
                           HorizontalAlignment="Center"
                           FontSize="{StaticResource FontSize.Headline}"
                           FontWeight="Bold"
                           Margin="0 5 0 0"/>
            </StackPanel>

            <!-- Polls -->
            <StackPanel Grid.Column="2" Orientation="Vertical" Margin="5 0">
                <TextBlock Text="{markup:Localize Monitor_Polls}"
                           Style="{StaticResource Text.Body}"
                           HorizontalAlignment="Center"
                           FontSize="{StaticResource FontSize.Caption}"
                           TextWrapping="Wrap"
                           TextAlignment="Center"/>
                <TextBlock Text="{Binding ViewModel.Polls}"
                           Style="{StaticResource Text.Body}"
                           HorizontalAlignment="Center"
                           FontSize="{StaticResource FontSize.Headline}"
                           FontWeight="Bold"
                           Margin="0 5 0 0"/>
            </StackPanel>

            <!-- NAKs -->
            <StackPanel Grid.Column="3" Orientation="Vertical" Margin="5 0">
                <TextBlock Text="{markup:Localize Monitor_Naks}"
                           Style="{StaticResource Text.Body}"
                           HorizontalAlignment="Center"
                           FontSize="{StaticResource FontSize.Caption}"
                           TextWrapping="Wrap"
                           TextAlignment="Center"/>
                <TextBlock Text="{Binding ViewModel.Naks}"
                           Style="{StaticResource Text.Body}"
                           HorizontalAlignment="Center"
                           FontSize="{StaticResource FontSize.Headline}"
                           FontWeight="Bold"
                           Margin="0 5 0 0"/>
            </StackPanel>
        </Grid>
    </StackPanel>
</Border>
```

### MAUI Implementation Guidelines

#### 1. Layout Structure
Create a statistics panel above the trace entries list with:
- **Top Section:** Line quality percentage (large, prominent display)
- **Bottom Section:** 4-column grid with packet statistics

#### 2. Line Quality Display
- Label: "Line Quality" (centered, normal size)
- Value: Large font size (28-32pt), bold, with decimal formatting (`{0:F1}`)
- Percent symbol: Slightly smaller (20-24pt), bold

#### 3. Statistics Grid
Use a Grid with 4 equal-width columns:
- **Column 0:** Commands Sent
- **Column 1:** Replies Received
- **Column 2:** Polls
- **Column 3:** NAKs

Each statistic cell should have:
- Label text (small, wrapped, centered)
- Value (large, bold, centered, margin above)

#### 4. MAUI XAML Example Structure

```xaml
<Border StyleClass="Card">
    <VerticalStackLayout Padding="15,10">
        <!-- Line Quality -->
        <VerticalStackLayout HorizontalOptions="Center" Margin="0,5,0,15">
            <Label Text="{x:Static resources:Resources.Monitor_LineQuality}"
                   HorizontalOptions="Center"
                   FontSize="14"/>
            <HorizontalStackLayout HorizontalOptions="Center" Margin="0,5,0,0">
                <Label Text="{Binding LineQualityPercentage, StringFormat='{0:F1}'}"
                       FontSize="28"
                       FontAttributes="Bold"/>
                <Label Text="%"
                       FontSize="20"
                       FontAttributes="Bold"/>
            </HorizontalStackLayout>
        </VerticalStackLayout>

        <!-- Statistics Grid -->
        <Grid ColumnDefinitions="*,*,*,*">
            <!-- Commands Sent -->
            <VerticalStackLayout Grid.Column="0" Margin="5,0">
                <Label Text="{x:Static resources:Resources.Monitor_CommandsSent}"
                       HorizontalOptions="Center"
                       FontSize="12"
                       HorizontalTextAlignment="Center"
                       LineBreakMode="WordWrap"/>
                <Label Text="{Binding CommandsSent}"
                       HorizontalOptions="Center"
                       FontSize="18"
                       FontAttributes="Bold"
                       Margin="0,5,0,0"/>
            </VerticalStackLayout>

            <!-- Replies Received -->
            <VerticalStackLayout Grid.Column="1" Margin="5,0">
                <Label Text="{x:Static resources:Resources.Monitor_RepliesReceived}"
                       HorizontalOptions="Center"
                       FontSize="12"
                       HorizontalTextAlignment="Center"
                       LineBreakMode="WordWrap"/>
                <Label Text="{Binding RepliesReceived}"
                       HorizontalOptions="Center"
                       FontSize="18"
                       FontAttributes="Bold"
                       Margin="0,5,0,0"/>
            </VerticalStackLayout>

            <!-- Polls -->
            <VerticalStackLayout Grid.Column="2" Margin="5,0">
                <Label Text="{x:Static resources:Resources.Monitor_Polls}"
                       HorizontalOptions="Center"
                       FontSize="12"
                       HorizontalTextAlignment="Center"
                       LineBreakMode="WordWrap"/>
                <Label Text="{Binding Polls}"
                       HorizontalOptions="Center"
                       FontSize="18"
                       FontAttributes="Bold"
                       Margin="0,5,0,0"/>
            </VerticalStackLayout>

            <!-- NAKs -->
            <VerticalStackLayout Grid.Column="3" Margin="5,0">
                <Label Text="{x:Static resources:Resources.Monitor_Naks}"
                       HorizontalOptions="Center"
                       FontSize="12"
                       HorizontalTextAlignment="Center"
                       LineBreakMode="WordWrap"/>
                <Label Text="{Binding Naks}"
                       HorizontalOptions="Center"
                       FontSize="18"
                       FontAttributes="Bold"
                       Margin="0,5,0,0"/>
            </VerticalStackLayout>
        </Grid>
    </VerticalStackLayout>
</Border>
```

#### 5. Data Bindings Required
All bindings are to the `MonitorViewModel`:
- `LineQualityPercentage` - computed property (read-only)
- `CommandsSent` - observable property
- `RepliesReceived` - observable property
- `Polls` - observable property
- `Naks` - observable property

#### 6. Responsive Considerations
- On smaller screens, consider:
  - Reducing font sizes slightly
  - Reducing margins/padding
  - Potentially stacking statistics in 2x2 grid instead of 1x4
  - Ensuring labels wrap properly in narrow columns

#### 7. Testing Checklist
- [ ] Statistics panel displays correctly on Monitor page
- [ ] Line quality shows 100.0% initially
- [ ] Commands sent increments when packets are sent
- [ ] Replies received increments when responses arrive
- [ ] Polls counter tracks poll packets
- [ ] NAKs counter tracks NAK replies
- [ ] Line quality percentage updates correctly
- [ ] Line quality remains stable (doesn't jump) during normal operation
- [ ] Statistics reset to zero on reconnection
- [ ] Layout works on various screen sizes (phone/tablet)
- [ ] All resource strings are localized

---

## Testing Notes

### Shared Core Tests
All Core ViewModel changes have corresponding unit tests in `test/Core.Tests/ViewModels/`:
- `ConnectViewModelTests.cs` - Disconnect button visibility
- `MonitorViewModelTests.cs` - Error state handling

### Manual Testing Scenarios

#### 1. Invalid Security Key Error
1. Configure device with incorrect security key
2. Attempt connection
3. Verify:
   - Disconnect button is visible on Connect page
   - Monitor page shows content (not blocked)
   - StatusLevel is set to Error

#### 2. Disconnect Access
1. Connect to device
2. Navigate to Monitor page
3. View trace entries
4. Disconnect from device
5. Verify:
   - Monitor page remains accessible
   - Previous trace history still visible
   - Can review captured data

#### 3. Packet Statistics
1. Connect to device
2. Navigate to Monitor page
3. Send various commands (polls, identify, etc.)
4. Verify:
   - Commands sent counter increments
   - Replies received counter increments
   - Polls counter tracks poll packets
   - Line quality shows near 100% during normal operation
5. Trigger NAK response
6. Verify:
   - NAKs counter increments
7. Disconnect and reconnect
8. Verify:
   - All statistics reset to zero

#### 4. Line Quality Stability
1. Connect to device with normal polling
2. Observe line quality percentage
3. Verify:
   - Percentage doesn't jump/flicker during normal operation
   - Stays at or near 100% with good connection
   - Only drops when actual communication failures occur (>2 missed replies)

---

## Migration Priority

**High Priority:**
1. Packet statistics to Monitor page (most significant user-facing feature)
2. Allow Monitor page access after disconnect (UX improvement)

**Medium Priority:**
3. Monitor page error state fix (edge case handling)
4. Disconnect button visibility fix (edge case handling)

---

## Additional Notes

### Shared Core Library
Most changes are in the shared `Core` library (`src/Core`), which means MAUI automatically inherits:
- ViewModel property changes
- Statistics tracking logic
- Line quality calculation
- Status level handling
- Resource strings

### Platform-Specific Implementation
Only the XAML/UI layer needs to be replicated in MAUI:
- Statistics panel layout
- Visibility bindings
- UI styling and formatting

### Resource Localization
The 5 new resource strings in `Resources.resx` are shared, so they'll be available in MAUI through the same localization system.

---

## Questions or Issues?

For questions about implementing these changes in MAUI, refer to:
- Windows XAML files as UI reference
- Core ViewModels for binding requirements
- Unit tests for expected behavior
- This document for implementation details
# Async Initialization Pattern in ConnectViewModel

## Problem
The `ConnectViewModel` performs asynchronous initialization of serial ports in its constructor using `Task.Run`. This created testing challenges because:
- Tests had to use unreliable `Task.Delay` to wait for initialization
- No way to know when initialization completed
- Race conditions could cause flaky tests

## Solution
We implemented a `TaskCompletionSource` pattern to track initialization completion:

### 1. Added Initialization Tracking
```csharp
private readonly TaskCompletionSource<bool> _initializationComplete = new();

/// <summary>
/// Gets a task that completes when the initial serial port scan is finished.
/// </summary>
public Task InitializationComplete => _initializationComplete.Task;
```

### 2. Signal Completion in InitializeSerialPorts
```csharp
private async Task InitializeSerialPorts()
{
    try
    {
        // ... initialization logic ...
        _initializationComplete.SetResult(true);
    }
    catch (Exception ex)
    {
        // ... error handling ...
        _initializationComplete.SetException(ex);
    }
}
```

### 3. Use in Tests
```csharp
[Test]
public async Task ConnectViewModel_InitializesSerialPortsOnStartup()
{
    // Arrange
    var availablePorts = CreateTestSerialPorts();
    SetupSerialPortMockWithPorts(availablePorts);
    
    // Act
    var newViewModel = new ConnectViewModel(...);
    
    // Wait for initialization to complete
    await newViewModel.InitializationComplete;
    
    // Assert
    Assert.That(newViewModel.AvailableSerialPorts.Count, Is.GreaterThan(0));
}
```

## Benefits
1. **Deterministic**: Tests wait exactly as long as needed
2. **Reliable**: No race conditions or timing issues
3. **Fast**: No unnecessary delays
4. **Error handling**: Exceptions during initialization are properly propagated
5. **Testable**: Different initialization scenarios can be tested (success, failure, no ports)

## Alternative Patterns Considered
1. **Factory pattern with async initialization**: Would require changing how ViewModels are created
2. **Lazy initialization**: Would complicate the ViewModel usage
3. **Synchronous initialization**: Would block the UI thread

The `TaskCompletionSource` pattern provides the best balance of simplicity, testability, and maintaining the existing architecture.
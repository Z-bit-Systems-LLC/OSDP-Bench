using System;
using System.Threading.Tasks;
using NUnit.Framework;
using OSDPBench.Core.Actions;

namespace OSDPBench.Core.Tests.Actions;

[TestFixture(TestOf = typeof(MonitoringAction))]
public class MonitoringActionTests
{
    [Test]
    public void Constructor_WithCardReadsType_SetsCorrectName()
    {
        // Arrange & Act
        var action = new MonitoringAction(MonitoringType.CardReads);
        
        // Assert
        Assert.That(action.Name, Is.EqualTo("Monitor Card Reads"));
        Assert.That(action.MonitoringType, Is.EqualTo(MonitoringType.CardReads));
    }
    
    [Test]
    public void Constructor_WithKeypadReadsType_SetsCorrectName()
    {
        // Arrange & Act
        var action = new MonitoringAction(MonitoringType.KeypadReads);
        
        // Assert
        Assert.That(action.Name, Is.EqualTo("Monitor Keypad Reads"));
        Assert.That(action.MonitoringType, Is.EqualTo(MonitoringType.KeypadReads));
    }
    
    [Test]
    public void PerformActionName_IsEmpty()
    {
        // Arrange
        var action = new MonitoringAction(MonitoringType.CardReads);
        
        // Act & Assert
        Assert.That(action.PerformActionName, Is.Empty);
    }
    
    [Test]
    public async Task PerformAction_ReturnsTrue()
    {
        // Arrange
        var action = new MonitoringAction(MonitoringType.CardReads);
        
        // Creating an actual ControlPanel would require network connections and hardware
        // Since this is just a stub method returning a static value, we can pass null
        
        // Act
        var result = await action.PerformAction(null!, Guid.NewGuid(), 1, null);
        
        // Assert
        Assert.That(result, Is.EqualTo(true));
    }
    
    [Test]
    public void IsCardReadsMonitor_WithCardReadsMonitoringAction_ReturnsTrue()
    {
        // Arrange
        var action = new MonitoringAction(MonitoringType.CardReads);
        
        // Act & Assert
        Assert.That(action.IsCardReadsMonitor(), Is.True);
        Assert.That(action.IsKeypadReadsMonitor(), Is.False);
    }
    
    [Test]
    public void IsKeypadReadsMonitor_WithKeypadReadsMonitoringAction_ReturnsTrue()
    {
        // Arrange
        var action = new MonitoringAction(MonitoringType.KeypadReads);
        
        // Act & Assert
        Assert.That(action.IsCardReadsMonitor(), Is.False);
        Assert.That(action.IsKeypadReadsMonitor(), Is.True);
    }
    
    [Test]
    public void IsMonitoringAction_WithCorrectType_ReturnsTrue()
    {
        // Arrange
        var action = new MonitoringAction(MonitoringType.CardReads);
        
        // Act & Assert
        Assert.That(action.IsMonitoringAction(MonitoringType.CardReads), Is.True);
        Assert.That(action.IsMonitoringAction(MonitoringType.KeypadReads), Is.False);
    }
    
    [Test]
    public void IsMonitoringAction_WithOtherDeviceAction_ReturnsFalse()
    {
        // Arrange
        var controlBuzzerAction = new ControlBuzzerAction();
        
        // Act & Assert
        Assert.That(controlBuzzerAction.IsMonitoringAction(MonitoringType.CardReads), Is.False);
        Assert.That(controlBuzzerAction.IsMonitoringAction(MonitoringType.KeypadReads), Is.False);
    }
}
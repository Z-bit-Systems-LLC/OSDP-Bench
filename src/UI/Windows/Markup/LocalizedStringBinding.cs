using System.ComponentModel;
using System.Windows;
using System.Windows.Data;

namespace OSDPBench.Windows.Markup;

/// <summary>
/// Provides a binding that automatically updates when the culture changes
/// </summary>
public class LocalizedStringBinding : INotifyPropertyChanged
{
    private readonly string _key;
    
    public LocalizedStringBinding(string key)
    {
        _key = key;
        
        // Subscribe to culture changes
        OSDPBench.Core.Resources.Resources.PropertyChanged += OnResourcesPropertyChanged;
    }
    
    public string Value => OSDPBench.Core.Resources.Resources.GetString(_key);
    
    private void OnResourcesPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // When culture changes, notify that our Value property has changed
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;
    
    ~LocalizedStringBinding()
    {
        OSDPBench.Core.Resources.Resources.PropertyChanged -= OnResourcesPropertyChanged;
    }
}
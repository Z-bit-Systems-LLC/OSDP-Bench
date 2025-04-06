using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using Wpf.Ui.Controls;

namespace OSDPBench.Windows.Views.Controls;

public sealed partial class SetCommunicationControl : INotifyPropertyChanged
{
    public SetCommunicationControl(int[] availableBaudRates, uint connectedBaudRate, byte connectedAddress)
    {
        DataContext = this;

        AvailableBaudRates = availableBaudRates;
        SelectedBaudRate = (int)connectedBaudRate;
        SelectedAddress = connectedAddress;

        InitializeComponent();
    }
    
    public int[] AvailableBaudRates { get; }

    private int _selectedBaudRate;
    public int SelectedBaudRate
    {
        get => _selectedBaudRate;
        set => SetField(ref _selectedBaudRate, value);
    }

    private double _selectedAddress;
    public double SelectedAddress
    {
        get => _selectedAddress;
        set => SetField(ref _selectedAddress, value);
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        OnPropertyChanged(propertyName);
    }

    private void AddressNumberBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        SelectedAddress = AddressNumberBox.Value ?? 0;
    }

    private void AddressNumberBox_OnValueChanged(object sender, NumberBoxValueChangedEventArgs args)
    {
        SelectedAddress = AddressNumberBox.Value ?? 0;
    }
}
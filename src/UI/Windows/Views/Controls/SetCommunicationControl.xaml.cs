﻿using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace OSDPBench.Windows.Views.Controls;

public sealed partial class SetCommunicationControl : INotifyPropertyChanged
{
    public SetCommunicationControl(int[] availableBaudRates, uint connectedBaudRate, byte connectedAddress)
    {
        DataContext = this;

        AvailableBaudRates = availableBaudRates;
        _selectedBaudRate = (int)connectedBaudRate;
        _selectedAddress = connectedAddress;

        InitializeComponent();
    }
    
    public int[] AvailableBaudRates { get; }

    private int _selectedBaudRate;
    public int SelectedBaudRate
    {
        get => _selectedBaudRate;
        set => SetField(ref _selectedBaudRate, value);
    }

    private byte _selectedAddress;
    public byte SelectedAddress
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
}
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Controls;
using OSDP.Net.Model.CommandData;
using OSDPBench.Core.Models;
using Wpf.Ui.Controls;

namespace OSDPBench.Windows.Views.Controls;

public partial class ControlBuzzerControl
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private ToneCode _selectedToneCode = ToneCode.Default;
    private byte _readerNumber;
    private byte _onTime = 1;
    private byte _offTime = 1;
    private byte _count = 3;

    public ObservableCollection<ToneCode> AvailableToneCodes { get; } =
    [
        ToneCode.Default,
        ToneCode.Off
    ];

    public ToneCode SelectedToneCode
    {
        get => _selectedToneCode;
        set
        {
            if (_selectedToneCode == value) return;
            _selectedToneCode = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedToneCode)));
            NotifyValidationChanged();
        }
    }

    public byte ReaderNumber
    {
        get => _readerNumber;
        set
        {
            if (_readerNumber == value) return;
            _readerNumber = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ReaderNumber)));
        }
    }

    public byte OnTime
    {
        get => _onTime;
        set
        {
            if (_onTime == value) return;
            _onTime = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(OnTime)));
            NotifyValidationChanged();
        }
    }

    public byte OffTime
    {
        get => _offTime;
        set
        {
            if (_offTime == value) return;
            _offTime = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(OffTime)));
        }
    }

    public bool IsOnTimeInvalid => GetBuzzerParameters().IsOnTimeInvalid;

    public bool HasErrors => IsOnTimeInvalid;

    private void NotifyValidationChanged()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsOnTimeInvalid)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasErrors)));
    }

    public byte Count
    {
        get => _count;
        set
        {
            if (_count == value) return;
            _count = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
        }
    }

    /// <summary>
    /// Gets the current buzzer parameters based on user selections.
    /// </summary>
    public BuzzerParameters GetBuzzerParameters()
    {
        return new BuzzerParameters
        {
            ReaderNumber = ReaderNumber,
            ToneCode = SelectedToneCode,
            OnTime = OnTime,
            OffTime = OffTime,
            Count = Count
        };
    }

    public ControlBuzzerControl()
    {
        InitializeComponent();

        DataContext = this;
    }

    private void ReaderNumberBox_OnValueChanged(object sender, NumberBoxValueChangedEventArgs args)
    {
        ReaderNumber = (byte)(ReaderNumberBox.Value ?? 0);
    }

    private void ReaderNumberBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        ReaderNumber = (byte)(ReaderNumberBox.Value ?? 0);
    }

    private void OnTimeNumberBox_OnValueChanged(object sender, NumberBoxValueChangedEventArgs args)
    {
        OnTime = (byte)(OnTimeNumberBox.Value ?? 1);
    }

    private void OnTimeNumberBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        OnTime = (byte)(OnTimeNumberBox.Value ?? 1);
    }

    private void OffTimeNumberBox_OnValueChanged(object sender, NumberBoxValueChangedEventArgs args)
    {
        OffTime = (byte)(OffTimeNumberBox.Value ?? 1);
    }

    private void OffTimeNumberBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        OffTime = (byte)(OffTimeNumberBox.Value ?? 1);
    }

    private void CountNumberBox_OnValueChanged(object sender, NumberBoxValueChangedEventArgs args)
    {
        Count = (byte)(CountNumberBox.Value ?? 3);
    }

    private void CountNumberBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        Count = (byte)(CountNumberBox.Value ?? 3);
    }
}

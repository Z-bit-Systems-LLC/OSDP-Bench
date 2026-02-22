using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Controls;
using OSDP.Net.Model.CommandData;
using OSDPBench.Core.Models;
using Wpf.Ui.Controls;

namespace OSDPBench.Windows.Views.Controls;

public partial class SetReaderLedControl : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private byte _readerNumber;
    private byte _ledNumber;
    private KeyValuePair<string, TemporaryReaderControlCode> _selectedTemporaryMode;
    private byte _temporaryOnTime = 10;
    private byte _temporaryOffTime = 10;
    private KeyValuePair<string, LedColor> _selectedTemporaryOnColor;
    private KeyValuePair<string, LedColor> _selectedTemporaryOffColor;
    private ushort _temporaryTimer = 50;
    private KeyValuePair<string, PermanentReaderControlCode> _selectedPermanentMode;
    private byte _permanentOnTime;
    private byte _permanentOffTime;
    private KeyValuePair<string, LedColor> _selectedPermanentOnColor;
    private KeyValuePair<string, LedColor> _selectedPermanentOffColor;

    public ObservableCollection<KeyValuePair<string, LedColor>> AvailableColors { get; } =
    [
        new("Black", LedColor.Black),
        new("Red", LedColor.Red),
        new("Green", LedColor.Green),
        new("Amber", LedColor.Amber),
        new("Blue", LedColor.Blue),
        new("Magenta", LedColor.Magenta),
        new("Cyan", LedColor.Cyan),
        new("White", LedColor.White)
    ];

    public ObservableCollection<KeyValuePair<string, TemporaryReaderControlCode>> AvailableTemporaryModes { get; } =
    [
        new("NOP - Do not alter", TemporaryReaderControlCode.Nop),
        new("Cancel temporary, show permanent", TemporaryReaderControlCode.CancelAnyTemporaryAndDisplayPermanent),
        new("Set temporary and start timer", TemporaryReaderControlCode.SetTemporaryAndStartTimer)
    ];

    public ObservableCollection<KeyValuePair<string, PermanentReaderControlCode>> AvailablePermanentModes { get; } =
    [
        new("NOP - Do not alter", PermanentReaderControlCode.Nop),
        new("Set permanent state", PermanentReaderControlCode.SetPermanentState)
    ];

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

    public byte LedNumber
    {
        get => _ledNumber;
        set
        {
            if (_ledNumber == value) return;
            _ledNumber = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LedNumber)));
        }
    }

    public KeyValuePair<string, TemporaryReaderControlCode> SelectedTemporaryMode
    {
        get => _selectedTemporaryMode;
        set
        {
            if (_selectedTemporaryMode.Key == value.Key) return;
            _selectedTemporaryMode = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedTemporaryMode)));
            NotifyTemporaryTimingValidationChanged();
        }
    }

    public byte TemporaryOnTime
    {
        get => _temporaryOnTime;
        set
        {
            if (_temporaryOnTime == value) return;
            _temporaryOnTime = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TemporaryOnTime)));
            NotifyTemporaryTimingValidationChanged();
        }
    }

    public byte TemporaryOffTime
    {
        get => _temporaryOffTime;
        set
        {
            if (_temporaryOffTime == value) return;
            _temporaryOffTime = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TemporaryOffTime)));
            NotifyTemporaryTimingValidationChanged();
        }
    }

    public KeyValuePair<string, LedColor> SelectedTemporaryOnColor
    {
        get => _selectedTemporaryOnColor;
        set
        {
            if (_selectedTemporaryOnColor.Key == value.Key) return;
            _selectedTemporaryOnColor = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedTemporaryOnColor)));
        }
    }

    public KeyValuePair<string, LedColor> SelectedTemporaryOffColor
    {
        get => _selectedTemporaryOffColor;
        set
        {
            if (_selectedTemporaryOffColor.Key == value.Key) return;
            _selectedTemporaryOffColor = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedTemporaryOffColor)));
        }
    }

    public ushort TemporaryTimer
    {
        get => _temporaryTimer;
        set
        {
            if (_temporaryTimer == value) return;
            _temporaryTimer = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TemporaryTimer)));
        }
    }

    public KeyValuePair<string, PermanentReaderControlCode> SelectedPermanentMode
    {
        get => _selectedPermanentMode;
        set
        {
            if (_selectedPermanentMode.Key == value.Key) return;
            _selectedPermanentMode = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedPermanentMode)));
            NotifyPermanentTimingValidationChanged();
        }
    }

    public byte PermanentOnTime
    {
        get => _permanentOnTime;
        set
        {
            if (_permanentOnTime == value) return;
            _permanentOnTime = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PermanentOnTime)));
            NotifyPermanentTimingValidationChanged();
        }
    }

    public byte PermanentOffTime
    {
        get => _permanentOffTime;
        set
        {
            if (_permanentOffTime == value) return;
            _permanentOffTime = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PermanentOffTime)));
            NotifyPermanentTimingValidationChanged();
        }
    }

    public KeyValuePair<string, LedColor> SelectedPermanentOnColor
    {
        get => _selectedPermanentOnColor;
        set
        {
            if (_selectedPermanentOnColor.Key == value.Key) return;
            _selectedPermanentOnColor = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedPermanentOnColor)));
        }
    }

    public KeyValuePair<string, LedColor> SelectedPermanentOffColor
    {
        get => _selectedPermanentOffColor;
        set
        {
            if (_selectedPermanentOffColor.Key == value.Key) return;
            _selectedPermanentOffColor = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedPermanentOffColor)));
        }
    }

    public bool IsTemporaryTimingInvalid => GetLedParameters().IsTemporaryTimingInvalid;

    public bool IsPermanentTimingInvalid => GetLedParameters().IsPermanentTimingInvalid;

    public bool HasErrors => IsTemporaryTimingInvalid || IsPermanentTimingInvalid;

    private void NotifyTemporaryTimingValidationChanged()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsTemporaryTimingInvalid)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasErrors)));
    }

    private void NotifyPermanentTimingValidationChanged()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsPermanentTimingInvalid)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasErrors)));
    }

    /// <summary>
    /// Gets the current LED parameters based on user selections.
    /// </summary>
    public LedParameters GetLedParameters()
    {
        return new LedParameters
        {
            ReaderNumber = ReaderNumber,
            LedNumber = LedNumber,
            TemporaryMode = SelectedTemporaryMode.Value,
            TemporaryOnTime = TemporaryOnTime,
            TemporaryOffTime = TemporaryOffTime,
            TemporaryOnColor = SelectedTemporaryOnColor.Value,
            TemporaryOffColor = SelectedTemporaryOffColor.Value,
            TemporaryTimer = TemporaryTimer,
            PermanentMode = SelectedPermanentMode.Value,
            PermanentOnTime = PermanentOnTime,
            PermanentOffTime = PermanentOffTime,
            PermanentOnColor = SelectedPermanentOnColor.Value,
            PermanentOffColor = SelectedPermanentOffColor.Value
        };
    }

    public SetReaderLedControl()
    {
        InitializeComponent();

        DataContext = this;

        _selectedTemporaryMode = AvailableTemporaryModes[2];
        _selectedTemporaryOnColor = AvailableColors[1];
        _selectedTemporaryOffColor = AvailableColors[0];
        _selectedPermanentMode = AvailablePermanentModes[0];
        _selectedPermanentOnColor = AvailableColors[0];
        _selectedPermanentOffColor = AvailableColors[0];
    }

    private void ReaderNumberBox_OnValueChanged(object sender, NumberBoxValueChangedEventArgs args)
    {
        ReaderNumber = (byte)(ReaderNumberBox.Value ?? 0);
    }

    private void ReaderNumberBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        ReaderNumber = (byte)(ReaderNumberBox.Value ?? 0);
    }

    private void LedNumberBox_OnValueChanged(object sender, NumberBoxValueChangedEventArgs args)
    {
        LedNumber = (byte)(LedNumberBox.Value ?? 0);
    }

    private void LedNumberBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        LedNumber = (byte)(LedNumberBox.Value ?? 0);
    }

    private void TemporaryOnTimeBox_OnValueChanged(object sender, NumberBoxValueChangedEventArgs args)
    {
        TemporaryOnTime = (byte)(TemporaryOnTimeBox.Value ?? 10);
    }

    private void TemporaryOnTimeBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        TemporaryOnTime = (byte)(TemporaryOnTimeBox.Value ?? 10);
    }

    private void TemporaryOffTimeBox_OnValueChanged(object sender, NumberBoxValueChangedEventArgs args)
    {
        TemporaryOffTime = (byte)(TemporaryOffTimeBox.Value ?? 10);
    }

    private void TemporaryOffTimeBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        TemporaryOffTime = (byte)(TemporaryOffTimeBox.Value ?? 10);
    }

    private void TemporaryTimerBox_OnValueChanged(object sender, NumberBoxValueChangedEventArgs args)
    {
        TemporaryTimer = (ushort)(TemporaryTimerBox.Value ?? 50);
    }

    private void TemporaryTimerBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        TemporaryTimer = (ushort)(TemporaryTimerBox.Value ?? 50);
    }

    private void PermanentOnTimeBox_OnValueChanged(object sender, NumberBoxValueChangedEventArgs args)
    {
        PermanentOnTime = (byte)(PermanentOnTimeBox.Value ?? 0);
    }

    private void PermanentOnTimeBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        PermanentOnTime = (byte)(PermanentOnTimeBox.Value ?? 0);
    }

    private void PermanentOffTimeBox_OnValueChanged(object sender, NumberBoxValueChangedEventArgs args)
    {
        PermanentOffTime = (byte)(PermanentOffTimeBox.Value ?? 0);
    }

    private void PermanentOffTimeBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        PermanentOffTime = (byte)(PermanentOffTimeBox.Value ?? 0);
    }
}

using System.Collections.ObjectModel;
using System.ComponentModel;
using OSDP.Net.Model.CommandData;
using OSDPBench.Core.Models;

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
}

using System.Collections.ObjectModel;
using System.ComponentModel;

namespace OSDPBench.Windows.Views.Controls;

public partial class SetReaderLedControl
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private string _selectedColor = "Red";

    public ObservableCollection<string> AvailableColors { get; } = new(
        new[] { "Red", "Green", "Amber", "Blue" }
    );

    public string SelectedColor
    {
        get => _selectedColor;
        set
        {
            if (_selectedColor == value) return;
            _selectedColor = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedColor)));
        }
    }

    public SetReaderLedControl()
    {
        InitializeComponent();

        DataContext = this;
        SelectedColor = AvailableColors.First();
    }
}
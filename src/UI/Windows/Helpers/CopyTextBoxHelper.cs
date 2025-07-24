using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace OSDPBench.Windows.Helpers;

public static class CopyTextBoxHelper
{
    public static readonly DependencyProperty CopyCommandProperty =
        DependencyProperty.RegisterAttached(
            "CopyCommand",
            typeof(ICommand),
            typeof(CopyTextBoxHelper),
            new PropertyMetadata(null));

    public static ICommand GetCopyCommand(DependencyObject obj)
    {
        return (ICommand)obj.GetValue(CopyCommandProperty);
    }

    public static void SetCopyCommand(DependencyObject obj, ICommand value)
    {
        obj.SetValue(CopyCommandProperty, value);
    }

    public static readonly ICommand DefaultCopyCommand = new RelayCommand(
        parameter =>
        {
            if (parameter is TextBox textBox && !string.IsNullOrEmpty(textBox.Text))
            {
                try
                {
                    Clipboard.SetText(textBox.Text);
                }
                catch (System.Runtime.InteropServices.COMException)
                {
                    // Clipboard is locked by another application, ignore silently
                    // This is a common Windows issue and shouldn't crash the app
                }
            }
        });
}

public class RelayCommand(Action<object> execute, Func<object, bool>? canExecute = null)
    : ICommand
{
    private readonly Action<object> _execute = execute ?? throw new ArgumentNullException(nameof(execute));

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter)
    {
        return parameter != null && (canExecute?.Invoke(parameter) ?? true);
    }

    public void Execute(object? parameter)
    {
        if (parameter != null) _execute(parameter);
    }
}
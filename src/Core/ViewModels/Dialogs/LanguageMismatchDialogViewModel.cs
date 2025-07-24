using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace OSDPBench.Core.ViewModels.Dialogs;

/// <summary>
/// ViewModel for the language mismatch dialog
/// </summary>
public partial class LanguageMismatchDialogViewModel : ObservableObject
{
    private readonly Action<bool, bool> _closeCallback;
    
    [ObservableProperty]
    private string _message;
    
    [ObservableProperty]
    private bool _dontAskAgain;
    
    /// <summary>
    /// Initializes a new instance of the LanguageMismatchDialogViewModel
    /// </summary>
    /// <param name="message">The message to display</param>
    /// <param name="closeCallback">Callback when dialog closes (userChoice, dontAskAgain)</param>
    public LanguageMismatchDialogViewModel(string message, Action<bool, bool> closeCallback)
    {
        _message = message;
        _closeCallback = closeCallback ?? throw new ArgumentNullException(nameof(closeCallback));
    }
    
    [RelayCommand]
    private void Yes()
    {
        _closeCallback(true, DontAskAgain);
    }
    
    [RelayCommand]
    private void No()
    {
        _closeCallback(false, DontAskAgain);
    }
}
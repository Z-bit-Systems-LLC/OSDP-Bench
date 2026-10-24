using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OSDPBench.Core.Models;
using OSDPBench.Core.Services.Analysis;

namespace OSDPBench.Core.ViewModels.Windows;

/// <summary>
/// Drives the capture analysis view: signing in to the analysis service, analyzing a capture and asking follow-up
/// questions. It works with any <see cref="ICaptureAnalysisService"/>.
/// </summary>
public partial class CaptureAnalysisViewModel : ObservableObject, IDisposable
{
    private readonly ICaptureAnalysisService _analysisService;
    private CaptureForAnalysis? _capture;
    private CancellationTokenSource? _cancellation;

    /// <summary>
    /// Initializes a new instance of the <see cref="CaptureAnalysisViewModel"/> class.
    /// </summary>
    /// <param name="analysisService">The service that analyzes captures.</param>
    public CaptureAnalysisViewModel(ICaptureAnalysisService analysisService)
    {
        _analysisService = analysisService ?? throw new ArgumentNullException(nameof(analysisService));
        Exchanges.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasExchanges));
    }

    /// <summary>
    /// Gets the name of the analysis service.
    /// </summary>
    public string ServiceName => _analysisService.DisplayName;

    /// <summary>
    /// Gets a value indicating whether the user has to sign in and isn't signed in yet.
    /// </summary>
    public bool NeedsSignIn => _analysisService.RequiresSignIn && Account == null;

    /// <summary>
    /// Gets a value indicating whether the user is signed in to a service that has accounts.
    /// </summary>
    public bool IsSignedIn => _analysisService.RequiresSignIn && Account != null;

    /// <summary>
    /// Gets a value indicating whether to offer signing in: the user needs to and isn't already doing so.
    /// </summary>
    public bool ShowSignInPrompt => NeedsSignIn && !IsSigningIn;

    /// <summary>
    /// Gets a value indicating whether an analysis or answer is in progress. Signing in shows its own progress.
    /// </summary>
    public bool IsWorking => IsBusy && !IsSigningIn;

    /// <summary>
    /// Gets the text identifying the signed-in account.
    /// </summary>
    public string AccountText => Account == null
        ? string.Empty
        : Resources.Resources.GetString("CaptureAnalysis_SignedInAs").Replace("{0}", Account.DisplayName);

    /// <summary>
    /// Gets the text describing how many analyses are left, or an empty string when the service has no limit.
    /// </summary>
    public string QuotaText => Quota == null
        ? string.Empty
        : Resources.Resources.GetString("CaptureAnalysis_QuotaRemaining")
            .Replace("{0}", Quota.Remaining.ToString())
            .Replace("{1}", Quota.Limit.ToString());

    /// <summary>
    /// Gets a value indicating whether follow-up questions can be asked about the current analysis.
    /// </summary>
    public bool CanAskFollowUp => _analysisService.SupportsFollowUpQuestions && AnalysisId != null;

    /// <summary>
    /// Gets the text describing the analyzed capture, e.g. "1,234 packets".
    /// </summary>
    public string SummaryText => Summary == null
        ? string.Empty
        : Resources.Resources.GetString("CaptureAnalysis_PacketCount").Replace("{0}", Summary.Records.ToString("N0"));

    /// <summary>
    /// Gets the text listing the automatic checks that found problems, or that they all passed.
    /// </summary>
    public string ChecksText
    {
        get
        {
            if (Summary == null || Summary.Checks.Count == 0) return string.Empty;

            var found = Summary.Checks.Where(check => check.Count > 0)
                .Select(check => $"{check.Name} ({check.Count:N0})")
                .ToArray();
            return found.Length == 0
                ? Resources.Resources.GetString("CaptureAnalysis_ChecksPassed")
                : Resources.Resources.GetString("CaptureAnalysis_ChecksFound").Replace("{0}", string.Join(", ", found));
        }
    }

    /// <summary>
    /// Gets a value indicating whether there is a summary of the analyzed capture to show.
    /// </summary>
    public bool HasSummary => Summary != null;

    /// <summary>
    /// Gets a value indicating whether the last operation failed.
    /// </summary>
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    /// <summary>
    /// Gets a value indicating whether the capture was too long to analyze in full.
    /// </summary>
    public bool IsCaptureTruncated => Summary?.Truncated == true;

    /// <summary>
    /// Gets a value indicating whether there is a report to show.
    /// </summary>
    public bool HasExchanges => Exchanges.Count > 0;

    /// <summary>
    /// Gets the report and the follow-up questions and answers, in order.
    /// </summary>
    public ObservableCollection<CaptureAnalysisExchange> Exchanges { get; } = [];

    /// <summary>
    /// Gets the signed-in account, or <see langword="null"/> when signed out.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SignInCommand), nameof(SignOutCommand), nameof(AnalyzeCommand))]
    private CaptureAnalysisAccount? _account;

    /// <summary>
    /// Gets how many analyses are left, or <see langword="null"/> when unknown or unlimited.
    /// </summary>
    [ObservableProperty] private CaptureAnalysisQuota? _quota;

    /// <summary>
    /// Gets the file name of the capture being analyzed.
    /// </summary>
    [ObservableProperty] private string _fileName = string.Empty;

    /// <summary>
    /// Gets a value indicating whether the capture came with a Secure Channel key that could be sent.
    /// </summary>
    [ObservableProperty] private bool _hasSecureChannelKey;

    /// <summary>
    /// Gets or sets a value indicating whether to send the Secure Channel key so encrypted traffic can be decoded.
    /// </summary>
    [ObservableProperty] private bool _includeSecureChannelKey;

    /// <summary>
    /// Gets or sets a value indicating whether card numbers and keypad entries may be included in the analysis.
    /// </summary>
    [ObservableProperty] private bool _includeCredentials;

    /// <summary>
    /// Gets or sets an optional question to focus the analysis on.
    /// </summary>
    [ObservableProperty] private string _question = string.Empty;

    /// <summary>
    /// Gets or sets the follow-up question to ask next.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AskCommand))]
    private string _followUpQuestion = string.Empty;

    /// <summary>
    /// Gets a value indicating whether a sign-in, analysis or answer is in progress.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsWorking))]
    [NotifyCanExecuteChangedFor(nameof(SignInCommand), nameof(SignOutCommand), nameof(AnalyzeCommand),
        nameof(AskCommand), nameof(StopCommand))]
    private bool _isBusy;

    /// <summary>
    /// Gets a value indicating whether the sign-in page is open and the user hasn't finished signing in yet.
    /// <see cref="StopCommand"/> cancels it.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowSignInPrompt), nameof(IsWorking))]
    private bool _isSigningIn;

    /// <summary>
    /// Gets the message for the last failure, or <see langword="null"/>.
    /// </summary>
    [ObservableProperty] private string? _errorMessage;

    /// <summary>
    /// Gets what the analyzer read from the capture.
    /// </summary>
    [ObservableProperty] private CaptureAnalysisSummary? _summary;

    /// <summary>
    /// Gets the identifier to ask follow-up questions with, or <see langword="null"/> when none can be asked.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AskCommand))]
    private string? _analysisId;

    /// <summary>
    /// Sets the capture to analyze.
    /// </summary>
    /// <param name="capture">The capture to analyze.</param>
    public void Load(CaptureForAnalysis capture)
    {
        _capture = capture ?? throw new ArgumentNullException(nameof(capture));
        FileName = capture.FileName;
        HasSecureChannelKey = capture.SecureChannelKey != null;
        IncludeSecureChannelKey = false;
        AnalyzeCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Restores a saved sign-in and reads the quota.
    /// </summary>
    [RelayCommand]
    private async Task InitializeAsync()
    {
        await RunAsync(async cancellationToken =>
        {
            Account = await _analysisService.GetAccountAsync(cancellationToken);
            if (!NeedsSignIn)
            {
                Quota = await _analysisService.GetQuotaAsync(cancellationToken);
            }
        });
    }

    private bool CanSignIn() => !IsBusy && NeedsSignIn;

    [RelayCommand(CanExecute = nameof(CanSignIn))]
    private async Task SignInAsync()
    {
        await RunAsync(async cancellationToken =>
        {
            IsSigningIn = true;
            try
            {
                Account = await _analysisService.SignInAsync(cancellationToken);
            }
            finally
            {
                IsSigningIn = false;
            }

            Quota = await _analysisService.GetQuotaAsync(cancellationToken);
        });
    }

    private bool CanSignOut() => !IsBusy && IsSignedIn;

    [RelayCommand(CanExecute = nameof(CanSignOut))]
    private async Task SignOutAsync()
    {
        await RunAsync(async cancellationToken =>
        {
            await _analysisService.SignOutAsync(cancellationToken);
            Account = null;
            Quota = null;
            AnalysisId = null;
        });
    }

    private bool CanAnalyze() => !IsBusy && !NeedsSignIn && _capture != null;

    [RelayCommand(CanExecute = nameof(CanAnalyze))]
    private async Task AnalyzeAsync()
    {
        if (_capture == null) return;

        var question = Question.Trim();
        var request = new CaptureAnalysisRequest(_capture.FileName, _capture.Capture)
        {
            Question = question.Length > 0 ? question : null,
            SecureChannelKey = IncludeSecureChannelKey ? _capture.SecureChannelKey : null,
            IncludeCredentials = IncludeCredentials
        };

        Exchanges.Clear();
        Summary = null;
        AnalysisId = null;

        await StreamAnswerAsync(new CaptureAnalysisExchange(request.Question),
            cancellationToken => _analysisService.AnalyzeAsync(request, cancellationToken),
            () => { });
    }

    private bool CanAsk() => !IsBusy && CanAskFollowUp && !string.IsNullOrWhiteSpace(FollowUpQuestion);

    [RelayCommand(CanExecute = nameof(CanAsk))]
    private async Task AskAsync()
    {
        if (AnalysisId is not { } analysisId) return;

        var question = FollowUpQuestion.Trim();
        FollowUpQuestion = string.Empty;

        await StreamAnswerAsync(new CaptureAnalysisExchange(question),
            cancellationToken => _analysisService.AskAsync(analysisId, question, cancellationToken),
            () => FollowUpQuestion = question);
    }

    /// <summary>
    /// Stops the sign-in, analysis or answer in progress.
    /// </summary>
    [RelayCommand(CanExecute = nameof(IsBusy))]
    private void Stop()
    {
        _cancellation?.Cancel();
    }

    /// <summary>
    /// Streams an answer into a new exchange. An exchange that failed or was stopped before any text arrived is taken
    /// back off, and <paramref name="restore"/> puts the question back so it can be asked again.
    /// </summary>
    private async Task StreamAnswerAsync(CaptureAnalysisExchange exchange,
        Func<CancellationToken, IAsyncEnumerable<CaptureAnalysisEvent>> start, Action restore)
    {
        Exchanges.Add(exchange);
        var cancelled = false;
        try
        {
            await RunAsync(async cancellationToken =>
            {
                try
                {
                    await foreach (var analysisEvent in start(cancellationToken))
                    {
                        Apply(exchange, analysisEvent);
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    cancelled = true;
                    throw;
                }
            });
        }
        finally
        {
            exchange.IsInProgress = false;
            if (exchange.Answer.Length == 0)
            {
                Exchanges.Remove(exchange);
                restore();
            }
            else
            {
                exchange.WasStopped = cancelled;
            }
        }
    }

    private void Apply(CaptureAnalysisExchange exchange, CaptureAnalysisEvent analysisEvent)
    {
        switch (analysisEvent)
        {
            case CaptureAnalysisEvent.Started started:
                if (started.AnalysisId != null && _analysisService.SupportsFollowUpQuestions)
                {
                    AnalysisId = started.AnalysisId;
                }

                if (started.Summary != null)
                {
                    Summary = started.Summary;
                }

                break;
            case CaptureAnalysisEvent.TextReceived text:
                exchange.Append(text.Text);
                break;
            case CaptureAnalysisEvent.Completed completed:
                exchange.IsTruncated = completed.Truncated;
                if (completed.Quota != null)
                {
                    Quota = completed.Quota;
                }

                break;
        }
    }

    /// <summary>
    /// Runs an operation with the busy state, cancellation and error reporting every command shares.
    /// </summary>
    private async Task RunAsync(Func<CancellationToken, Task> operation)
    {
        ErrorMessage = null;
        using var cancellation = new CancellationTokenSource();
        _cancellation = cancellation;
        IsBusy = true;
        try
        {
            await operation(cancellation.Token);
        }
        catch (OperationCanceledException)
        {
            // Stopped by the user.
        }
        catch (CaptureAnalysisException exception)
        {
            OnFailure(exception);
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
        }
        finally
        {
            _cancellation = null;
            IsBusy = false;
        }
    }

    private void OnFailure(CaptureAnalysisException exception)
    {
        ErrorMessage = exception.Message;
        if (exception.Quota != null)
        {
            Quota = exception.Quota;
        }

        switch (exception.Kind)
        {
            case CaptureAnalysisErrorKind.NotSignedIn:
                Account = null;
                Quota = null;
                AnalysisId = null;
                break;
            case CaptureAnalysisErrorKind.AnalysisExpired:
            case CaptureAnalysisErrorKind.FollowUpLimitReached:
                AnalysisId = null;
                break;
        }
    }

    partial void OnAccountChanged(CaptureAnalysisAccount? value)
    {
        _ = value; // Required by source generator signature
        OnPropertyChanged(nameof(NeedsSignIn));
        OnPropertyChanged(nameof(IsSignedIn));
        OnPropertyChanged(nameof(ShowSignInPrompt));
        OnPropertyChanged(nameof(AccountText));
    }

    partial void OnQuotaChanged(CaptureAnalysisQuota? value)
    {
        _ = value; // Required by source generator signature
        OnPropertyChanged(nameof(QuotaText));
    }

    partial void OnSummaryChanged(CaptureAnalysisSummary? value)
    {
        _ = value; // Required by source generator signature
        OnPropertyChanged(nameof(HasSummary));
        OnPropertyChanged(nameof(SummaryText));
        OnPropertyChanged(nameof(ChecksText));
        OnPropertyChanged(nameof(IsCaptureTruncated));
    }

    partial void OnErrorMessageChanged(string? value)
    {
        _ = value; // Required by source generator signature
        OnPropertyChanged(nameof(HasError));
    }

    partial void OnAnalysisIdChanged(string? value)
    {
        _ = value; // Required by source generator signature
        OnPropertyChanged(nameof(CanAskFollowUp));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _cancellation?.Cancel();
        GC.SuppressFinalize(this);
    }
}

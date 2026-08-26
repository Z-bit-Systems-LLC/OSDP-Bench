using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OSDP.Net.LineQuality;
using OSDPBench.Core.Models;
using OSDPBench.Core.Services;

namespace OSDPBench.Core.ViewModels.Pages;

/// <summary>
/// View model for the Line Quality page, which runs the OSDP Line Quality Test Procedure in
/// either role: the controller that measures a line, or the responder that answers on the far end.
/// </summary>
/// <remarks>
/// Both roles take exclusive ownership of the serial port, outside <c>ControlPanel</c> and its
/// polling bus. Anything already connected on that port has to be shut down first, which is why
/// the page asks before it starts rather than failing on a port that is already open.
/// </remarks>
public partial class LineQualityViewModel : ObservableObject, IDisposable
{
    private readonly IDialogService _dialogService;
    private readonly ILineQualityService _lineQualityService;
    private readonly ISerialPortConnectionService _serialPortConnectionService;
    private readonly IDeviceManagementService _deviceManagementService;
    private readonly IUsbDeviceMonitorService? _usbDeviceMonitorService;
    private readonly IUserSettingsService? _userSettingsService;

    private readonly TaskCompletionSource<bool> _initializationComplete = new();
    private LineQualityReport? _report;
    private bool _isDisposed;

    /// <summary>
    /// Where the last report was saved, handed back to the save dialog so a job's reports land in
    /// one folder without walking the same path back on every drop.
    /// </summary>
    private string? _reportDestination;

    /// <summary>
    /// Initializes the view model.
    /// </summary>
    /// <param name="dialogService">Used to report failures and to prompt before taking the port.</param>
    /// <param name="lineQualityService">Runs the controller and responder roles.</param>
    /// <param name="serialPortConnectionService">Enumerates the available serial ports.</param>
    /// <param name="deviceManagementService">Consulted to find out whether the port is already in use.</param>
    /// <param name="usbDeviceMonitorService">Optional monitor that keeps the port list current.</param>
    /// <param name="userSettingsService">Optional settings used to preselect the last port.</param>
    public LineQualityViewModel(IDialogService dialogService, ILineQualityService lineQualityService,
        ISerialPortConnectionService serialPortConnectionService,
        IDeviceManagementService deviceManagementService,
        IUsbDeviceMonitorService? usbDeviceMonitorService = null,
        IUserSettingsService? userSettingsService = null)
    {
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _lineQualityService = lineQualityService ?? throw new ArgumentNullException(nameof(lineQualityService));
        _serialPortConnectionService = serialPortConnectionService ??
                                       throw new ArgumentNullException(nameof(serialPortConnectionService));
        _deviceManagementService = deviceManagementService ??
                                   throw new ArgumentNullException(nameof(deviceManagementService));
        _usbDeviceMonitorService = usbDeviceMonitorService;
        _userSettingsService = userSettingsService;

        // Applied before the options are subscribed to, so restoring a saved selection does not
        // read as the user changing it.
        ApplySavedSettings();

        foreach (var option in BaudRateOptions)
        {
            option.PropertyChanged += OnBaudRateOptionChanged;
        }

        _lineQualityService.ResponderExchangeCompleted += OnResponderExchangeCompleted;
        _lineQualityService.ResponderBaudRateChanged += OnResponderBaudRateChanged;
        _lineQualityService.ResponderStopped += OnResponderStopped;

        if (_usbDeviceMonitorService != null)
        {
            _usbDeviceMonitorService.UsbDeviceChanged += OnUsbDeviceChanged;
        }

        Task.Run(async () => await InitializeSerialPorts());
    }

    /// <summary>
    /// Gets a task that completes when the initial serial port scan is finished.
    /// </summary>
    public Task InitializationComplete => _initializationComplete.Task;

    #region Configuration

    /// <summary>Gets the serial ports the test can run on.</summary>
    [ObservableProperty] private ObservableCollection<AvailableSerialPort> _availableSerialPorts = [];

    /// <summary>Gets or sets the serial port the test runs on.</summary>
    [ObservableProperty] private AvailableSerialPort? _selectedSerialPort;

    /// <summary>Gets the profiles the run can use.</summary>
    [ObservableProperty] private ObservableCollection<LineQualityProfileOption> _availableProfiles =
        new(Enum.GetValues<TestProfile>().Select(profile => new LineQualityProfileOption(profile)));

    /// <summary>Gets or sets the profile the run uses.</summary>
    [ObservableProperty] private LineQualityProfileOption _selectedProfile =
        new(TestProfile.Screening);

    /// <summary>Gets the baud rates offered for the sweep, and which are included.</summary>
    [ObservableProperty] private ObservableCollection<LineQualityBaudRateOption> _baudRateOptions =
        new(LineQualityProtocol.DefaultBaudRates.Select(rate => new LineQualityBaudRateOption(rate)));

    /// <summary>Gets or sets the responder address the test talks to.</summary>
    [ObservableProperty] private byte _address = LineQualityProtocol.TestAddress;

    /// <summary>
    /// The highest address OSDP allows a device to be assigned, 126 (0x7E).
    /// </summary>
    private const double MaximumAddress = 0x7E;

    /// <summary>
    /// Gets or sets the responder address in the form a numeric entry control uses.
    /// </summary>
    /// <remarks>
    /// A NumberBox reports a nullable double and produces null the moment its text is cleared,
    /// which cannot be bound straight to a byte without the binding silently failing and leaving
    /// the address stale. Coercing here keeps the address valid whatever is typed.
    /// </remarks>
    public double? AddressValue
    {
        get => Address;
        set => Address = (byte)Math.Clamp(value ?? 0, 0, MaximumAddress);
    }

    partial void OnAddressChanged(byte value)
    {
        _ = value;
        OnPropertyChanged(nameof(AddressValue));
    }

    /// <summary>Gets or sets whether the run is in controller mode rather than responder mode.</summary>
    [ObservableProperty] private bool _isControllerMode = true;

    partial void OnSelectedSerialPortChanged(AvailableSerialPort? value)
    {
        _ = value;
        NotifyCommandsChanged();
    }

    partial void OnSelectedProfileChanged(LineQualityProfileOption value)
    {
        _ = value;
        OnPropertyChanged(nameof(ProfileDetail));
    }

    partial void OnIsControllerModeChanged(bool value)
    {
        _ = value;
        OnPropertyChanged(nameof(IsResponderMode));
        NotifyCommandsChanged();
    }

    /// <summary>Gets a value indicating whether the page is in responder mode.</summary>
    public bool IsResponderMode => !IsControllerMode;

    /// <summary>
    /// Gets a value indicating whether either role currently owns the serial port, and so the
    /// configuration controls must not be changed underneath it.
    /// </summary>
    public bool IsBusy => IsTestRunning || IsResponderRunning;

    /// <summary>Gets a description of what the selected profile costs and what it proves.</summary>
    public string ProfileDetail => SelectedProfile.Description;

    private void OnBaudRateOptionChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs args)
    {
        _ = sender;
        _ = args;
        NotifyCommandsChanged();
    }

    #endregion

    #region Controller

    /// <summary>Gets a value indicating whether a test is currently running.</summary>
    [ObservableProperty] private bool _isTestRunning;

    /// <summary>Gets the most recent progress message from a running test.</summary>
    [ObservableProperty] private string _progressMessage = string.Empty;

    /// <summary>Gets how far through the run the test is, as a percentage.</summary>
    [ObservableProperty] private double _progressPercent;

    /// <summary>Gets the baud rate the test is currently exercising.</summary>
    [ObservableProperty] private int _currentBaudRate;

    /// <summary>Gets the results for each baud rate the last run exercised.</summary>
    [ObservableProperty] private ObservableCollection<BaudRateResult> _results = [];

    /// <summary>Gets the overall verdict of the last run.</summary>
    [ObservableProperty] private LineQualityVerdict _overallVerdict = LineQualityVerdict.Untested;

    /// <summary>Gets the highest baud rate that passed, or null when none did.</summary>
    [ObservableProperty] private int? _recommendedBaudRate;

    /// <summary>Gets how long the last run took.</summary>
    [ObservableProperty] private TimeSpan _testDuration;

    /// <summary>Gets a value indicating whether a completed run is available to report on.</summary>
    public bool HasResults => _report != null;

    /// <summary>
    /// Gets the highest baud rate that passed, or a localized "none" when no rate did.
    /// </summary>
    public string RecommendedBaudRateText => RecommendedBaudRate?.ToString() ??
                                             Resources.Resources.GetString("LineQuality_NoRecommendedBaudRate");

    /// <summary>Gets how long the last run took, formatted for display.</summary>
    public string TestDurationText => TestDuration.ToString(@"hh\:mm\:ss");

    /// <summary>
    /// Gets a value indicating whether a test can start: a port is selected, at least one baud
    /// rate is included, and nothing else is using the port.
    /// </summary>
    public bool CanStartTest =>
        IsControllerMode &&
        !IsTestRunning &&
        !IsResponderRunning &&
        SelectedSerialPort != null &&
        BaudRateOptions.Any(option => option.IsSelected);

    [RelayCommand(CanExecute = nameof(CanStartTest), IncludeCancelCommand = true)]
    private async Task StartTest(CancellationToken token)
    {
        string portName = SelectedSerialPort?.Name ?? string.Empty;
        if (!await PrepareForExclusiveUse(portName)) return;

        await PersistSettings();

        var options = new LineQualityOptions
        {
            Profile = SelectedProfile.Profile,
            Address = Address,
            BaudRates = BaudRateOptions.Where(option => option.IsSelected)
                .Select(option => option.BaudRate).ToArray(),
            Progress = new Progress<LineQualityProgress>(OnTestProgress)
        };

        ClearResults();
        ProgressPercent = 0;
        ProgressMessage = Resources.Resources.GetString("LineQuality_Starting");
        IsTestRunning = true;

        try
        {
            var report = await _lineQualityService.RunTestAsync(portName, options, token);
            ApplyReport(report);
        }
        catch (OperationCanceledException)
        {
            ProgressMessage = Resources.Resources.GetString("LineQuality_Cancelled");
        }
        catch (Exception exception)
        {
            ProgressMessage = Resources.Resources.GetString("LineQuality_Failed");
            await _dialogService.ShowExceptionDialog(
                Resources.Resources.GetString("LineQuality_Title"), exception);
        }
        finally
        {
            IsTestRunning = false;
            ProgressPercent = 0;
            CurrentBaudRate = 0;
        }
    }

    /// <summary>
    /// Discards the previous run's results, so a run in progress cannot leave the page showing a
    /// verdict and a Save button that belong to an earlier measurement.
    /// </summary>
    private void ClearResults()
    {
        _report = null;
        Results.Clear();
        OverallVerdict = LineQualityVerdict.Untested;
        RecommendedBaudRate = null;
        TestDuration = TimeSpan.Zero;

        // The notes describe the measurement that was just discarded, so carrying them into the
        // next run would attach one line's observation to another line's numbers. The location and
        // cable are left alone: the next drop on the same job is usually described the same way,
        // and the page offers an explicit way to clear them.
        Notes = string.Empty;

        OnPropertyChanged(nameof(HasResults));
        SaveReportCommand.NotifyCanExecuteChanged();
    }

    private void OnTestProgress(LineQualityProgress progress)
    {
        ProgressMessage = progress.Message;
        CurrentBaudRate = progress.BaudRate;

        if (progress.TotalBaudRates <= 0)
        {
            ProgressPercent = 0;
            return;
        }

        double withinRate = progress.TotalPacketsAtRate <= 0
            ? 0.0
            : (double)progress.PacketsSentAtRate / progress.TotalPacketsAtRate;

        ProgressPercent = 100.0 * (progress.CompletedBaudRates + withinRate) / progress.TotalBaudRates;
    }

    private void ApplyReport(LineQualityReport report)
    {
        _report = report;

        Results.Clear();
        foreach (var result in report.BaudRates)
        {
            Results.Add(result);
        }

        OverallVerdict = report.OverallVerdict;
        RecommendedBaudRate = report.RecommendedBaudRate;
        TestDuration = report.Duration;
        ProgressMessage = Resources.Resources.GetString("LineQuality_Complete");

        OnPropertyChanged(nameof(HasResults));
        SaveReportCommand.NotifyCanExecuteChanged();
    }

    #endregion

    #region Responder

    /// <summary>Gets a value indicating whether the responder is answering on the port.</summary>
    [ObservableProperty] private bool _isResponderRunning;

    /// <summary>Gets the baud rate the responder is currently answering at.</summary>
    [ObservableProperty] private int _responderBaudRate;

    /// <summary>Gets how many exchanges the responder has answered since it started.</summary>
    [ObservableProperty] private int _responderExchangeCount;

    /// <summary>Gets a description of what the responder is doing.</summary>
    [ObservableProperty] private string _responderStatus = string.Empty;

    /// <summary>Gets a value indicating whether the responder can be started.</summary>
    public bool CanStartResponder =>
        IsResponderMode && !IsResponderRunning && !IsTestRunning && SelectedSerialPort != null;

    [RelayCommand(CanExecute = nameof(CanStartResponder))]
    private async Task StartResponder()
    {
        string portName = SelectedSerialPort?.Name ?? string.Empty;
        if (!await PrepareForExclusiveUse(portName)) return;

        await PersistSettings();

        try
        {
            await _lineQualityService.StartResponderAsync(portName, Address);

            ResponderExchangeCount = 0;
            ResponderBaudRate = _lineQualityService.ResponderBaudRate;
            IsResponderRunning = true;
            ResponderStatus = Resources.Resources.GetString("LineQuality_ResponderListening");
        }
        catch (Exception exception)
        {
            ResponderStatus = Resources.Resources.GetString("LineQuality_Failed");
            await _dialogService.ShowExceptionDialog(
                Resources.Resources.GetString("LineQuality_Title"), exception);
        }
        finally
        {
            NotifyCommandsChanged();
        }
    }

    [RelayCommand(CanExecute = nameof(IsResponderRunning))]
    private async Task StopResponder()
    {
        await _lineQualityService.StopResponderAsync();

        IsResponderRunning = false;
        ResponderBaudRate = 0;
        ResponderStatus = Resources.Resources.GetString("LineQuality_ResponderStopped");
    }

    private void OnResponderExchangeCompleted(object? sender, LineQualityExchangeEventArgs args)
    {
        ResponderExchangeCount++;
        ResponderBaudRate = args.BaudRate;
    }

    private void OnResponderBaudRateChanged(object? sender, LineQualityBaudRateChangedEventArgs args)
    {
        ResponderBaudRate = args.BaudRate;
        ResponderStatus = args.WasAutoRevert
            ? Resources.Resources.GetString("LineQuality_ResponderReverted")
                .Replace("{0}", args.BaudRate.ToString())
            : Resources.Resources.GetString("LineQuality_ResponderRetuned")
                .Replace("{0}", args.BaudRate.ToString());
    }

    private void OnResponderStopped(object? sender, Exception? failure)
    {
        IsResponderRunning = false;
        ResponderBaudRate = 0;
        ResponderStatus = failure == null
            ? Resources.Resources.GetString("LineQuality_ResponderStopped")
            : Resources.Resources.GetString("LineQuality_ResponderFailed").Replace("{0}", failure.Message);
    }

    #endregion

    #region Tester and equipment

    // These describe the rig rather than the line: the same technician, laptop, adapter and pair of
    // devices measure every drop on a job. They are carried across launches so they are filled in
    // once, and they are shown before the run rather than with the results so they can be entered
    // while the first sweep is still going.

    /// <summary>Gets or sets who ran the test, for the report header.</summary>
    [ObservableProperty] private string _testerName = string.Empty;

    /// <summary>Gets or sets the controller-side model and firmware, for the report header.</summary>
    [ObservableProperty] private string _acuDescription = string.Empty;

    /// <summary>Gets or sets the responder-side model and firmware, for the report header.</summary>
    [ObservableProperty] private string _pdDescription = string.Empty;

    /// <summary>Gets or sets the host platform and serial adapter, for the report header.</summary>
    [ObservableProperty] private string _adapterDescription = string.Empty;

    /// <summary>
    /// Gets or sets whether the adapter's latency timer was lowered before the run.
    /// </summary>
    /// <remarks>
    /// Recorded because it decides whether the response times mean anything. A USB adapter left at
    /// its default 16 ms latency timer dominates the measurement at every rate above about 19200,
    /// so a report that does not say either way cannot be read as a timing result.
    /// </remarks>
    [ObservableProperty] private bool _adapterLatencyTimerAdjusted;

    #endregion

    #region Line details

    // These describe the one line that was just measured, and are the fields that have to change
    // between drops. They stay filled in after a run so the next drop can be described as a small
    // edit of the last one, and the page offers an explicit way to empty them.

    /// <summary>Gets or sets where the installation is, for the report header.</summary>
    [ObservableProperty] private string _installationLocation = string.Empty;

    /// <summary>Gets or sets the cable type and length, for the report header.</summary>
    [ObservableProperty] private string _cableDescription = string.Empty;

    /// <summary>Gets or sets free-form notes to include in the report.</summary>
    [ObservableProperty] private string _notes = string.Empty;

    /// <summary>
    /// Gets a value indicating whether any line detail is filled in and so can be cleared.
    /// </summary>
    public bool HasLineDetails => !string.IsNullOrWhiteSpace(InstallationLocation) ||
                                  !string.IsNullOrWhiteSpace(CableDescription) ||
                                  !string.IsNullOrWhiteSpace(Notes);

    /// <summary>
    /// Empties the details of the line just measured, for a technician moving to a drop that is
    /// not a variation on the last one.
    /// </summary>
    [RelayCommand(CanExecute = nameof(HasLineDetails))]
    private void ClearLineDetails()
    {
        InstallationLocation = string.Empty;
        CableDescription = string.Empty;
        Notes = string.Empty;
    }

    partial void OnInstallationLocationChanged(string value)
    {
        _ = value;
        NotifyLineDetailsChanged();
    }

    partial void OnCableDescriptionChanged(string value)
    {
        _ = value;
        NotifyLineDetailsChanged();
    }

    partial void OnNotesChanged(string value)
    {
        _ = value;
        NotifyLineDetailsChanged();
    }

    private void NotifyLineDetailsChanged()
    {
        OnPropertyChanged(nameof(HasLineDetails));
        ClearLineDetailsCommand.NotifyCanExecuteChanged();
    }

    #endregion

    #region Report

    [RelayCommand(CanExecute = nameof(HasResults))]
    private async Task SaveReport()
    {
        if (_report == null) return;

        await ExceptionHelper.ExecuteSafelyAsync(
            _dialogService,
            Resources.Resources.GetString("LineQuality_SaveReport"),
            async () =>
            {
                string markdown = LineQualityMarkdownReport.Render(_report, BuildMetadata());
                string fileName = LineQualityReportFileName.Build(InstallationLocation, DateTime.Now);

                string? destination = await _dialogService.SaveFilesWithDataAsync(
                    Resources.Resources.GetString("LineQuality_SelectReportDestination"),
                    [(fileName, Encoding.UTF8.GetBytes(markdown))],
                    _reportDestination);

                if (destination == null) return;

                // The metadata has just been committed to a report, which is the strongest signal
                // that it is worth carrying to the next drop, and the destination is where the rest
                // of the job's reports belong.
                _reportDestination = destination;
                await PersistSettings();

                await _dialogService.ShowMessageDialog(
                    Resources.Resources.GetString("LineQuality_SaveReport"),
                    Resources.Resources.GetString("LineQuality_ReportSaved").Replace("{0}", fileName),
                    MessageIcon.Information);
            });
    }

    private LineQualityReportMetadata BuildMetadata() => new()
    {
        TesterName = NullIfBlank(TesterName),
        InstallationLocation = NullIfBlank(InstallationLocation),
        CableDescription = NullIfBlank(CableDescription),
        AcuDescription = NullIfBlank(AcuDescription),
        PdDescription = NullIfBlank(PdDescription),
        AdapterDescription = NullIfBlank(AdapterDescription),
        AdapterLatencyTimerAdjusted = AdapterLatencyTimerAdjusted,
        Notes = NullIfBlank(Notes)
    };

    /// <summary>
    /// Leaves a field the technician did not fill in as a blank in the report, rather than an
    /// empty string that reads as an answered question.
    /// </summary>
    private static string? NullIfBlank(string value) => string.IsNullOrWhiteSpace(value) ? null : value;

    #endregion

    private async Task<bool> PrepareForExclusiveUse(string portName)
    {
        if (string.IsNullOrWhiteSpace(portName)) return false;

        if (!_lineQualityService.IsSupported(portName))
        {
            await _dialogService.ShowMessageDialog(
                Resources.Resources.GetString("LineQuality_Title"),
                Resources.Resources.GetString("LineQuality_NotSupported"),
                MessageIcon.Error);
            return false;
        }

        // The test drives the port directly, so the polling bus cannot be running on it. Ask
        // rather than tearing down a connection the user may still want.
        if (!_deviceManagementService.IsPortInUse)
        {
            return true;
        }

        bool confirmed = await _dialogService.ShowConfirmationDialog(
            Resources.Resources.GetString("LineQuality_Title"),
            Resources.Resources.GetString("LineQuality_DisconnectRequired"),
            MessageIcon.Warning);

        if (!confirmed) return false;

        if (_deviceManagementService.IsPassiveMonitoring)
        {
            await _deviceManagementService.StopPassiveMonitoring();
        }

        await _deviceManagementService.Shutdown();
        return true;
    }

    #region Persistence

    /// <summary>
    /// Restores the settings carried over from the last session, so a technician working through a
    /// run of drops does not set the page up again on every launch.
    /// </summary>
    private void ApplySavedSettings()
    {
        var saved = _userSettingsService?.LineQualitySettings;

        SelectedProfile = (saved != null && Enum.TryParse<TestProfile>(saved.Profile, out var profile)
                              ? AvailableProfiles.FirstOrDefault(option => option.Profile == profile)
                              : null) ??
                          AvailableProfiles.First(option => option.Profile == TestProfile.Screening);

        if (saved == null) return;

        ApplySavedBaudRates(saved.BaudRates);

        Address = saved.Address ?? LineQualityProtocol.TestAddress;
        IsControllerMode = saved.IsControllerMode;

        TesterName = saved.TesterName ?? string.Empty;
        AdapterDescription = saved.AdapterDescription ?? string.Empty;
        AcuDescription = saved.AcuDescription ?? string.Empty;
        PdDescription = saved.PdDescription ?? string.Empty;
        AdapterLatencyTimerAdjusted = saved.AdapterLatencyTimerAdjusted;

        InstallationLocation = saved.InstallationLocation ?? string.Empty;
        CableDescription = saved.CableDescription ?? string.Empty;

        _reportDestination = saved.ReportDestination;
    }

    /// <summary>
    /// Restores which baud rates were included in the last sweep.
    /// </summary>
    /// <remarks>
    /// A saved set that no longer names any offered rate, because the library changed its defaults
    /// or the settings file was edited by hand, is discarded rather than applied. Applying it would
    /// leave every rate cleared, and a Start button that can never enable with nothing on the page
    /// to explain why.
    /// </remarks>
    private void ApplySavedBaudRates(int[]? savedBaudRates)
    {
        if (savedBaudRates is not { Length: > 0 }) return;
        if (!BaudRateOptions.Any(option => savedBaudRates.Contains(option.BaudRate))) return;

        foreach (var option in BaudRateOptions)
        {
            option.IsSelected = savedBaudRates.Contains(option.BaudRate);
        }
    }

    /// <summary>
    /// Records the current setup so the next launch starts where this one left off.
    /// </summary>
    /// <remarks>
    /// Written when a run starts and when a report is saved, rather than on every keystroke, and a
    /// failure is swallowed: a settings file that cannot be written is not a reason to refuse to
    /// test a line.
    /// </remarks>
    private async Task PersistSettings()
    {
        if (_userSettingsService == null) return;

        try
        {
            await _userSettingsService.UpdateLineQualitySettingsAsync(new LineQualityUserSettings
            {
                Profile = SelectedProfile.Profile.ToString(),
                BaudRates = BaudRateOptions.Where(option => option.IsSelected)
                    .Select(option => option.BaudRate).ToArray(),
                Address = Address,
                IsControllerMode = IsControllerMode,
                TesterName = NullIfBlank(TesterName),
                AdapterDescription = NullIfBlank(AdapterDescription),
                AcuDescription = NullIfBlank(AcuDescription),
                PdDescription = NullIfBlank(PdDescription),
                AdapterLatencyTimerAdjusted = AdapterLatencyTimerAdjusted,
                InstallationLocation = NullIfBlank(InstallationLocation),
                CableDescription = NullIfBlank(CableDescription),
                ReportDestination = _reportDestination
            });
        }
        catch (Exception)
        {
            // Carrying the setup forward is a convenience; losing it must not interrupt the run.
        }
    }

    #endregion

    private async Task InitializeSerialPorts()
    {
        try
        {
            var foundPorts = await _serialPortConnectionService.FindAvailableSerialPorts();
            ReplaceSerialPorts(foundPorts.ToList());
            _initializationComplete.SetResult(true);
        }
        catch (Exception exception)
        {
            _initializationComplete.SetException(exception);
        }
    }

    private void ReplaceSerialPorts(IReadOnlyList<AvailableSerialPort> ports)
    {
        string? previousSelection = SelectedSerialPort?.Name ?? _userSettingsService?.LastSerialPortName;

        AvailableSerialPorts.Clear();
        foreach (var port in ports)
        {
            AvailableSerialPorts.Add(port);
        }

        SelectedSerialPort = AvailableSerialPorts.FirstOrDefault(port => port.Name == previousSelection) ??
                             AvailableSerialPorts.FirstOrDefault();
    }

    private async void OnUsbDeviceChanged(object? sender, UsbDeviceChangedEventArgs args)
    {
        _ = args;

        // A port list that changes underneath a running test is not worth chasing: the run owns
        // the port it started on, and rebuilding the list would clear the selection under it.
        if (IsTestRunning || IsResponderRunning) return;

        try
        {
            var ports = await _serialPortConnectionService.FindAvailableSerialPorts();
            ReplaceSerialPorts(ports.ToList());
        }
        catch (Exception)
        {
            // A failed rescan leaves the previous list in place, which is the better of the two
            // wrong answers while the user is mid-selection.
        }
    }

    private void NotifyCommandsChanged()
    {
        OnPropertyChanged(nameof(CanStartTest));
        OnPropertyChanged(nameof(CanStartResponder));
        StartTestCommand.NotifyCanExecuteChanged();
        StartResponderCommand.NotifyCanExecuteChanged();
        StopResponderCommand.NotifyCanExecuteChanged();
        SaveReportCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsTestRunningChanged(bool value)
    {
        _ = value;
        OnPropertyChanged(nameof(IsBusy));
        NotifyCommandsChanged();
    }

    partial void OnIsResponderRunningChanged(bool value)
    {
        _ = value;
        OnPropertyChanged(nameof(IsBusy));
        NotifyCommandsChanged();
    }

    partial void OnRecommendedBaudRateChanged(int? value)
    {
        _ = value;
        OnPropertyChanged(nameof(RecommendedBaudRateText));
    }

    partial void OnTestDurationChanged(TimeSpan value)
    {
        _ = value;
        OnPropertyChanged(nameof(TestDurationText));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        _lineQualityService.ResponderExchangeCompleted -= OnResponderExchangeCompleted;
        _lineQualityService.ResponderBaudRateChanged -= OnResponderBaudRateChanged;
        _lineQualityService.ResponderStopped -= OnResponderStopped;

        foreach (var option in BaudRateOptions)
        {
            option.PropertyChanged -= OnBaudRateOptionChanged;
        }

        if (_usbDeviceMonitorService != null)
        {
            _usbDeviceMonitorService.UsbDeviceChanged -= OnUsbDeviceChanged;
        }

        GC.SuppressFinalize(this);
    }
}

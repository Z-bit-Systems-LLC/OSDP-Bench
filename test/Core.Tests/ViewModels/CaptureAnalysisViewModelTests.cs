using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using OSDPBench.Core.Services;
using OSDPBench.Core.Services.Analysis;
using OSDPBench.Core.ViewModels.Windows;

namespace OSDPBench.Core.Tests.ViewModels;

[TestFixture(TestOf = typeof(CaptureAnalysisViewModel))]
public class CaptureAnalysisViewModelTests
{
    private static readonly byte[] CustomKey = Enumerable.Range(0, 16).Select(i => (byte)i).ToArray();

    private FakeAnalysisService _service;
    private Mock<IDialogService> _dialogService;
    private CaptureAnalysisViewModel _viewModel;

    [SetUp]
    public void Setup()
    {
        _service = new FakeAnalysisService();
        _dialogService = new Mock<IDialogService>();
        _viewModel = new CaptureAnalysisViewModel(_service, _dialogService.Object);
        _viewModel.Load(new CaptureForAnalysis("trace.osdpcap", [1, 2, 3], CustomKey));
    }

    [TearDown]
    public void TearDown()
    {
        _viewModel.Dispose();
    }

    [Test]
    public async Task Initialize_WhenSignedOut_AsksForSignInAndCannotAnalyze()
    {
        await _viewModel.InitializeCommand.ExecuteAsync(null);

        Assert.That(_viewModel.NeedsSignIn, Is.True);
        Assert.That(_viewModel.IsSignedIn, Is.False);
        Assert.That(_viewModel.SignInCommand.CanExecute(null), Is.True);
        Assert.That(_viewModel.AnalyzeCommand.CanExecute(null), Is.False);
        Assert.That(_service.QuotaReads, Is.Zero);
        Assert.That(_viewModel.HasError, Is.False);
        Assert.That(_viewModel.HasSummary, Is.False);
    }

    [Test]
    public async Task Initialize_WithSavedSignIn_ShowsAccountAndQuota()
    {
        _service.Account = new CaptureAnalysisAccount("Pat", "pat@example.com");

        await _viewModel.InitializeCommand.ExecuteAsync(null);

        Assert.That(_viewModel.IsSignedIn, Is.True);
        Assert.That(_viewModel.AccountText, Does.Contain("pat@example.com"));
        Assert.That(_viewModel.QuotaText, Does.Contain("4").And.Contain("5"));
        Assert.That(_viewModel.AnalyzeCommand.CanExecute(null), Is.True);
    }

    [Test]
    public async Task Initialize_ServiceWithoutAccounts_NeedsNoSignIn()
    {
        _service.RequiresSignIn = false;

        await _viewModel.InitializeCommand.ExecuteAsync(null);

        Assert.That(_viewModel.NeedsSignIn, Is.False);
        Assert.That(_viewModel.IsSignedIn, Is.False);
        Assert.That(_viewModel.AnalyzeCommand.CanExecute(null), Is.True);
    }

    [Test]
    public async Task SignIn_SetsAccountAndQuota()
    {
        await _viewModel.SignInCommand.ExecuteAsync(null);

        Assert.That(_viewModel.IsSignedIn, Is.True);
        Assert.That(_viewModel.Quota, Is.EqualTo(new CaptureAnalysisQuota(4, 5)));
    }

    [Test]
    public async Task SignIn_WhenCancelledByPlatform_IsNotAnError()
    {
        _service.SignInFailure = new TaskCanceledException();

        await _viewModel.SignInCommand.ExecuteAsync(null);

        Assert.That(_viewModel.ErrorMessage, Is.Null);
        Assert.That(_viewModel.NeedsSignIn, Is.True);
        Assert.That(_viewModel.IsBusy, Is.False);
    }

    [Test]
    public async Task SignIn_WhileWaitingForBrowser_ShowsProgressAndCanBeCancelled()
    {
        _service.WaitForSignIn = true;

        var signingIn = _viewModel.SignInCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(_viewModel.IsSigningIn, Is.True);
            Assert.That(_viewModel.ShowSignInPrompt, Is.False);
            Assert.That(_viewModel.IsWorking, Is.False, "Signing in shows its own progress.");
            Assert.That(_viewModel.StopCommand.CanExecute(null), Is.True);
        });

        _viewModel.StopCommand.Execute(null);
        await signingIn;

        Assert.Multiple(() =>
        {
            Assert.That(_viewModel.IsSigningIn, Is.False);
            Assert.That(_viewModel.ShowSignInPrompt, Is.True);
            Assert.That(_viewModel.ErrorMessage, Is.Null);
        });
    }

    [Test]
    public async Task Analyze_StreamsReportIntoOneExchange()
    {
        await SignInAsync();
        _service.Events =
        [
            new CaptureAnalysisEvent.Started("abc",
                new CaptureAnalysisSummary("trace.osdpcap", 1234, false, [new CaptureAnalysisCheck("NAK replies", 3), new CaptureAnalysisCheck("Retries", 0)])),
            new CaptureAnalysisEvent.TextReceived("# Report\n"),
            new CaptureAnalysisEvent.TextReceived("All good."),
            new CaptureAnalysisEvent.Completed(false, new CaptureAnalysisQuota(3, 5))
        ];

        await _viewModel.AnalyzeCommand.ExecuteAsync(null);

        Assert.That(_viewModel.Exchanges, Has.Count.EqualTo(1));
        var report = _viewModel.Exchanges[0];
        Assert.That(report.Answer, Is.EqualTo("# Report\nAll good."));
        Assert.That(report.IsInProgress, Is.False);
        Assert.That(report.HasQuestion, Is.False);
        Assert.That(_viewModel.AnalysisId, Is.EqualTo("abc"));
        Assert.That(_viewModel.CanAskFollowUp, Is.True);
        Assert.That(_viewModel.Quota, Is.EqualTo(new CaptureAnalysisQuota(3, 5)));
        Assert.That(_viewModel.SummaryText, Does.Contain("1,234").Or.Contain("1234"));
        Assert.That(_viewModel.ChecksText, Does.Contain("NAK replies (3)").And.Not.Contain("Retries"));
        Assert.That(_viewModel.HasSummary, Is.True);
        Assert.That(_viewModel.HasError, Is.False);
    }

    [Test]
    public async Task Analyze_SendsKeyOnlyWhenUserAgrees()
    {
        await SignInAsync();

        await _viewModel.AnalyzeCommand.ExecuteAsync(null);
        Assert.That(_service.LastRequest.SecureChannelKey, Is.Null);

        _viewModel.IncludeSecureChannelKey = true;
        _viewModel.IncludeCredentials = true;
        _viewModel.Question = "  Why the NAKs?  ";
        await _viewModel.AnalyzeCommand.ExecuteAsync(null);

        Assert.That(_service.LastRequest.SecureChannelKey, Is.EqualTo(CustomKey));
        Assert.That(_service.LastRequest.IncludeCredentials, Is.True);
        Assert.That(_service.LastRequest.Question, Is.EqualTo("Why the NAKs?"));
        Assert.That(_service.LastRequest.FileName, Is.EqualTo("trace.osdpcap"));
    }

    [Test]
    public async Task Analyze_FailsBeforeAnyText_RemovesExchangeAndShowsError()
    {
        await SignInAsync();
        _service.StreamFailure = new CaptureAnalysisException(CaptureAnalysisErrorKind.QuotaExceeded, "No analyses left.",
            new CaptureAnalysisQuota(0, 5));

        await _viewModel.AnalyzeCommand.ExecuteAsync(null);

        Assert.That(_viewModel.Exchanges, Is.Empty);
        Assert.That(_viewModel.ErrorMessage, Is.EqualTo("No analyses left."));
        Assert.That(_viewModel.HasError, Is.True);
        Assert.That(_viewModel.Quota, Is.EqualTo(new CaptureAnalysisQuota(0, 5)));
    }

    [Test]
    public async Task Analyze_WhenSignInExpired_AsksForSignInAgain()
    {
        await SignInAsync();
        _service.StreamFailure = new CaptureAnalysisException(CaptureAnalysisErrorKind.NotSignedIn, "Sign in again.");

        await _viewModel.AnalyzeCommand.ExecuteAsync(null);

        Assert.That(_viewModel.NeedsSignIn, Is.True);
        Assert.That(_viewModel.Quota, Is.Null);
    }

    [Test]
    public async Task Ask_StreamsAnswerAfterReport()
    {
        await AnalyzeAsync();
        _service.Events = [new CaptureAnalysisEvent.Started("abc", null), new CaptureAnalysisEvent.TextReceived("Because."), new CaptureAnalysisEvent.Completed(false, null)];
        _viewModel.FollowUpQuestion = "Why?";

        await _viewModel.AskCommand.ExecuteAsync(null);

        Assert.That(_viewModel.Exchanges, Has.Count.EqualTo(2));
        Assert.That(_viewModel.Exchanges[1].Question, Is.EqualTo("Why?"));
        Assert.That(_viewModel.Exchanges[1].Answer, Is.EqualTo("Because."));
        Assert.That(_service.LastQuestion, Is.EqualTo(("abc", "Why?")));
        Assert.That(_viewModel.FollowUpQuestion, Is.Empty);
    }

    [Test]
    public void Save_BeforeAnalysis_CannotExecute()
    {
        Assert.That(_viewModel.SaveCommand.CanExecute(null), Is.False);
    }

    [Test]
    public async Task Save_WritesReportAndFollowUpsAsMarkdown()
    {
        await SignInAsync();
        _viewModel.Question = "Why no LED?";
        _service.Events =
        [
            new CaptureAnalysisEvent.Started("abc",
                new CaptureAnalysisSummary("trace.osdpcap", 1234, false, [new CaptureAnalysisCheck("NAK replies", 3)])),
            new CaptureAnalysisEvent.TextReceived("## Verdict\n**Faulty.**\n"),
            new CaptureAnalysisEvent.Completed(false, null)
        ];
        await _viewModel.AnalyzeCommand.ExecuteAsync(null);
        _service.Events = [new CaptureAnalysisEvent.Started("abc", null), new CaptureAnalysisEvent.TextReceived("Because."), new CaptureAnalysisEvent.Completed(true, null)];
        _viewModel.FollowUpQuestion = "Which command?";
        await _viewModel.AskCommand.ExecuteAsync(null);

        List<(string FileName, byte[] Data)> saved = null;
        _dialogService.Setup(x => x.SaveFilesWithDataAsync(
                It.IsAny<string>(), It.IsAny<IEnumerable<(string, byte[])>>(), It.IsAny<string>()))
            .Callback<string, IEnumerable<(string, byte[])>, string>((_, files, _) => saved = files.ToList())
            .ReturnsAsync(@"C:\Reports");

        Assert.That(_viewModel.SaveCommand.CanExecute(null), Is.True);
        await _viewModel.SaveCommand.ExecuteAsync(null);

        Assert.That(saved, Has.Count.EqualTo(1));
        Assert.That(saved[0].FileName, Does.StartWith("trace-analysis-").And.EndWith(".md"));
        var lines = Encoding.UTF8.GetString(saved[0].Data).Split(Environment.NewLine);
        Assert.That(lines, Is.SupersetOf((string[])
        [
            "# Capture Analysis: trace.osdpcap",
            "> **Question:** Why no LED?",
            "## Verdict",
            "**Faulty.**",
            "---",
            "> **Follow-up question:** Which command?",
            "Because.",
            "*This answer reached its length limit and was cut short.*"
        ]));
        Assert.That(lines, Has.Some.StartWith("Analyzed by Fake Analyzer on "));
        Assert.That(lines, Has.Some.Contains("NAK replies (3)"));
        Assert.That(Array.IndexOf(lines, "## Verdict"), Is.LessThan(Array.IndexOf(lines, "Because.")));
        _dialogService.Verify(x => x.ShowMessageDialog(
            It.IsAny<string>(), It.Is<string>(message => message.Contains(saved[0].FileName)), MessageIcon.Information));

        await _viewModel.SaveCommand.ExecuteAsync(null);

        _dialogService.Verify(x => x.SaveFilesWithDataAsync(
            It.IsAny<string>(), It.IsAny<IEnumerable<(string, byte[])>>(), @"C:\Reports"), Times.Once);
    }

    [Test]
    public async Task Save_WhenCancelled_ShowsNoMessage()
    {
        await AnalyzeAsync();
        _dialogService.Setup(x => x.SaveFilesWithDataAsync(
                It.IsAny<string>(), It.IsAny<IEnumerable<(string, byte[])>>(), It.IsAny<string>()))
            .ReturnsAsync((string)null);

        await _viewModel.SaveCommand.ExecuteAsync(null);

        _dialogService.Verify(x => x.ShowMessageDialog(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MessageIcon>()), Times.Never);
    }

    [Test]
    public async Task Save_WhenWritingFails_ShowsError()
    {
        await AnalyzeAsync();
        var failure = new UnauthorizedAccessException("Read-only folder.");
        _dialogService.Setup(x => x.SaveFilesWithDataAsync(
                It.IsAny<string>(), It.IsAny<IEnumerable<(string, byte[])>>(), It.IsAny<string>()))
            .ThrowsAsync(failure);

        await _viewModel.SaveCommand.ExecuteAsync(null);

        _dialogService.Verify(x => x.ShowExceptionDialog(It.IsAny<string>(), failure));
    }

    [Test]
    public async Task Ask_FailsBeforeAnyText_PutsQuestionBack()
    {
        await AnalyzeAsync();
        _service.StreamFailure = new CaptureAnalysisException(CaptureAnalysisErrorKind.AnswerInProgress, "Wait.");
        _viewModel.FollowUpQuestion = "Why?";

        await _viewModel.AskCommand.ExecuteAsync(null);

        Assert.That(_viewModel.Exchanges, Has.Count.EqualTo(1));
        Assert.That(_viewModel.FollowUpQuestion, Is.EqualTo("Why?"));
        Assert.That(_viewModel.CanAskFollowUp, Is.True);
    }

    [Test]
    public async Task Ask_WhenAnalysisExpired_StopsOfferingFollowUps()
    {
        await AnalyzeAsync();
        _service.StreamFailure = new CaptureAnalysisException(CaptureAnalysisErrorKind.AnalysisExpired, "Expired.");
        _viewModel.FollowUpQuestion = "Why?";

        await _viewModel.AskCommand.ExecuteAsync(null);

        Assert.That(_viewModel.CanAskFollowUp, Is.False);
        Assert.That(_viewModel.AskCommand.CanExecute(null), Is.False);
    }

    [Test]
    public void Ask_WithoutAnalysis_CannotExecute()
    {
        _viewModel.FollowUpQuestion = "Why?";

        Assert.That(_viewModel.AskCommand.CanExecute(null), Is.False);
    }

    [Test]
    public async Task Stop_KeepsTextSoFarAndMarksAnswerStopped()
    {
        await SignInAsync();
        _service.Events = [new CaptureAnalysisEvent.Started("abc", null), new CaptureAnalysisEvent.TextReceived("Partial")];
        _service.WaitAfterEvents = true;

        var analysis = _viewModel.AnalyzeCommand.ExecuteAsync(null);
        await _service.EventsSent.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.That(_viewModel.IsBusy, Is.True);
        _viewModel.StopCommand.Execute(null);
        await analysis.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.That(_viewModel.IsBusy, Is.False);
        Assert.That(_viewModel.Exchanges, Has.Count.EqualTo(1));
        Assert.That(_viewModel.Exchanges[0].Answer, Is.EqualTo("Partial"));
        Assert.That(_viewModel.Exchanges[0].WasStopped, Is.True);
        Assert.That(_viewModel.ErrorMessage, Is.Null);
    }

    [Test]
    public async Task SignOut_ClearsAccountAndAnalysis()
    {
        await AnalyzeAsync();

        await _viewModel.SignOutCommand.ExecuteAsync(null);

        Assert.That(_service.Account, Is.Null);
        Assert.That(_viewModel.NeedsSignIn, Is.True);
        Assert.That(_viewModel.CanAskFollowUp, Is.False);
    }

    private async Task SignInAsync()
    {
        await _viewModel.SignInCommand.ExecuteAsync(null);
        Assert.That(_viewModel.IsSignedIn, Is.True);
    }

    private async Task AnalyzeAsync()
    {
        await SignInAsync();
        await _viewModel.AnalyzeCommand.ExecuteAsync(null);
        Assert.That(_viewModel.CanAskFollowUp, Is.True);
    }

    private sealed class FakeAnalysisService : ICaptureAnalysisService
    {
        public CaptureAnalysisAccount Account { get; set; }
        public Exception SignInFailure { get; set; }
        public bool WaitForSignIn { get; set; }
        public Exception StreamFailure { get; set; }
        public bool WaitAfterEvents { get; set; }
        public TaskCompletionSource EventsSent { get; } = new();
        public int QuotaReads { get; private set; }
        public CaptureAnalysisRequest LastRequest { get; private set; }
        public (string, string) LastQuestion { get; private set; }

        public List<CaptureAnalysisEvent> Events { get; set; } =
        [
            new CaptureAnalysisEvent.Started("abc", null),
            new CaptureAnalysisEvent.TextReceived("Report"),
            new CaptureAnalysisEvent.Completed(false, null)
        ];

        public string DisplayName => "Fake Analyzer";
        public bool RequiresSignIn { get; set; } = true;
        public bool SupportsFollowUpQuestions => true;

        public Task<CaptureAnalysisAccount> GetAccountAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Account);

        public async Task<CaptureAnalysisAccount> SignInAsync(CancellationToken cancellationToken = default)
        {
            if (WaitForSignIn) await Task.Delay(Timeout.Infinite, cancellationToken);
            if (SignInFailure != null) throw SignInFailure;
            Account = new CaptureAnalysisAccount("Pat", "pat@example.com");
            return Account;
        }

        public Task SignOutAsync(CancellationToken cancellationToken = default)
        {
            Account = null;
            return Task.CompletedTask;
        }

        public Task<CaptureAnalysisQuota> GetQuotaAsync(CancellationToken cancellationToken = default)
        {
            QuotaReads++;
            return Task.FromResult(new CaptureAnalysisQuota(4, 5));
        }

        public IAsyncEnumerable<CaptureAnalysisEvent> AnalyzeAsync(CaptureAnalysisRequest request,
            CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return StreamAsync(cancellationToken);
        }

        public IAsyncEnumerable<CaptureAnalysisEvent> AskAsync(string analysisId, string question,
            CancellationToken cancellationToken = default)
        {
            LastQuestion = (analysisId, question);
            return StreamAsync(cancellationToken);
        }

        private async IAsyncEnumerable<CaptureAnalysisEvent> StreamAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.Yield();
            if (StreamFailure != null) throw StreamFailure;

            foreach (var analysisEvent in Events)
            {
                yield return analysisEvent;
            }

            EventsSent.TrySetResult();
            if (WaitAfterEvents)
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
        }
    }
}

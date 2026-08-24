using System.Reflection;
using OSDP.Net.LineQuality;

namespace OSDPBench.UI.Tests.Helpers;

/// <summary>
/// Builds a <see cref="LineQualityReport"/> for tests.
/// </summary>
/// <remarks>
/// Only a real run can produce a report: every constructor and mutator on the results tree is
/// internal to OSDP.Net, because nothing outside the library has any business inventing
/// measurements. Tests still need one to prove the results page renders what a run produces, so
/// they are assembled here by reflection rather than by widening the library's surface.
/// </remarks>
internal static class LineQualityReportBuilder
{
    private const BindingFlags Internal = BindingFlags.Instance | BindingFlags.NonPublic;

    /// <summary>
    /// Builds a report with one clean rate and one that failed its transition, which between them
    /// exercise every column the results table shows.
    /// </summary>
    public static LineQualityReport CreateSampleReport()
    {
        var report = Construct<LineQualityReport>(TestProfile.Screening, "COM3", (byte)125);

        var passing = AddBaudRate(report, 9600);
        var combination = AddCombination(passing, TestPattern.AllZeros, 48);
        SetInternalProperty(combination, "PacketsSent", 160);
        SetInternalProperty(combination, "PacketsReceived", 160);
        RecordResponseTime(combination, 31.0);
        RecordResponseTime(combination, 42.0);

        var failing = AddBaudRate(report, 230400);
        SetInternalProperty(failing, "FailureReason", "The responder did not acknowledge the rate change.");

        SetInternalProperty(report, "CompletedUtc", report.StartedUtc.AddSeconds(30));
        return report;
    }

    private static BaudRateResult AddBaudRate(LineQualityReport report, int baudRate) =>
        (BaudRateResult)Invoke(report, "AddBaudRate", baudRate);

    private static CombinationResult AddCombination(BaudRateResult result, TestPattern pattern, int length) =>
        (CombinationResult)Invoke(result, "AddCombination", pattern, length);

    private static void RecordResponseTime(CombinationResult combination, double milliseconds) =>
        Invoke(combination, "RecordResponseTime", milliseconds);

    private static T Construct<T>(params object[] arguments) =>
        (T)Activator.CreateInstance(typeof(T), Internal, null, arguments, null)!;

    private static object Invoke(object target, string methodName, params object[] arguments) =>
        target.GetType().GetMethod(methodName, Internal)!.Invoke(target, arguments)!;

    private static void SetInternalProperty(object target, string propertyName, object value) =>
        target.GetType().GetProperty(propertyName)!.SetValue(target, value);
}

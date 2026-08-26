using System;
using NUnit.Framework;
using OSDPBench.Core.Models;

namespace OSDPBench.Core.Tests.Models;

[TestFixture(TestOf = typeof(LineQualityReportFileName))]
public class LineQualityReportFileNameTests
{
    private static readonly DateTime Timestamp = new(2026, 8, 26, 14, 30, 5);

    [Test]
    public void Build_NamesTheFileAfterTheLineItMeasured()
    {
        string fileName = LineQualityReportFileName.Build("Panel 3, Drop 2", Timestamp);

        Assert.That(fileName, Is.EqualTo("line-quality-Panel-3-Drop-2-20260826-143005.md"));
    }

    [Test]
    public void Build_FallsBackToTheTimestampWhenNoLocationWasGiven()
    {
        Assert.Multiple(() =>
        {
            Assert.That(LineQualityReportFileName.Build(null, Timestamp),
                Is.EqualTo("line-quality-20260826-143005.md"));
            Assert.That(LineQualityReportFileName.Build("   ", Timestamp),
                Is.EqualTo("line-quality-20260826-143005.md"));

            // A location made only of punctuation leaves nothing to name the file after.
            Assert.That(LineQualityReportFileName.Build("--/--", Timestamp),
                Is.EqualTo("line-quality-20260826-143005.md"));
        });
    }

    [Test]
    public void Build_ReplacesTheCharactersAPathCannotHold()
    {
        string fileName = LineQualityReportFileName.Build(@"Bldg A / Level 2 \ Door #7 *east*", Timestamp);

        Assert.Multiple(() =>
        {
            Assert.That(fileName, Is.EqualTo("line-quality-Bldg-A-Level-2-Door-7-east-20260826-143005.md"));
            Assert.That(fileName.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()), Is.EqualTo(-1));
        });
    }

    [Test]
    public void Build_TrimsALocationTooLongToPutInAPath()
    {
        string fileName = LineQualityReportFileName.Build(new string('a', 80), Timestamp);

        Assert.Multiple(() =>
        {
            Assert.That(fileName, Is.EqualTo($"line-quality-{new string('a', 40)}-20260826-143005.md"));

            // Trimming must not leave the separator doubled where the cut landed on one.
            Assert.That(LineQualityReportFileName.Build($"{new string('a', 40)} tail", Timestamp),
                Is.EqualTo($"line-quality-{new string('a', 40)}-20260826-143005.md"));
        });
    }
}

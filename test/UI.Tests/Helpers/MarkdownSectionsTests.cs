using NUnit.Framework;
using OSDPBench.Windows.Helpers;

namespace OSDPBench.UI.Tests.Helpers;

[TestFixture(TestOf = typeof(MarkdownSections))]
public class MarkdownSectionsTests
{
    [Test]
    public void Split_StartsASectionAtEachTopLevelHeading()
    {
        const string report = "## Verdict\n**Faulty.**\n\n## Findings\n### 1. NAKs\n- One\n\n## Session overview\n| A |\n|---|\n| 1 |\n";

        var sections = MarkdownSections.Split(report);

        Assert.That(sections, Is.EqualTo((string[])
        [
            "## Verdict\n**Faulty.**",
            "## Findings\n### 1. NAKs\n- One",
            "## Session overview\n| A |\n|---|\n| 1 |"
        ]));
    }

    [Test]
    public void Split_KeepsTextBeforeTheFirstHeadingAsItsOwnSection()
    {
        var sections = MarkdownSections.Split("The first NAK was at line 412.\n\n# Details\nMore.");

        Assert.That(sections, Is.EqualTo((string[])["The first NAK was at line 412.", "# Details\nMore."]));
    }

    [Test]
    public void Split_IgnoresHashLinesInsideCodeBlocks()
    {
        const string answer = "## Bytes\n```\n## not a heading\n53 00 08\n```\n";

        Assert.That(MarkdownSections.Split(answer), Is.EqualTo((string[])["## Bytes\n```\n## not a heading\n53 00 08\n```"]));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("  \n ")]
    public void Split_WithNoText_HasNoSections(string? markdown)
    {
        Assert.That(MarkdownSections.Split(markdown), Is.Empty);
    }
}

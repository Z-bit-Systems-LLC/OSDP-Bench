using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Block = System.Windows.Documents.Block;
using Inline = System.Windows.Documents.Inline;
using MarkdownTable = Markdig.Extensions.Tables.Table;
using Table = System.Windows.Documents.Table;
using TableCell = System.Windows.Documents.TableCell;
using TableRow = System.Windows.Documents.TableRow;

namespace OSDPBench.Windows.Helpers;

/// <summary>
/// Shows Markdown in a read-only <see cref="RichTextBox"/>: set <c>helpers:MarkdownDocument.Markdown</c> on it. Updates
/// are batched, so text streaming in a piece at a time doesn't re-render on every piece.
/// </summary>
public static class MarkdownDocument
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UsePipeTables()
        .UseEmphasisExtras()
        .UseAutoLinks()
        .Build();

    private static readonly FontFamily CodeFont = new("Cascadia Mono, Consolas, Courier New");
    private static readonly TimeSpan RenderDelay = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Identifies the Markdown attached property.
    /// </summary>
    public static readonly DependencyProperty MarkdownProperty = DependencyProperty.RegisterAttached(
        "Markdown", typeof(string), typeof(MarkdownDocument), new PropertyMetadata(null, OnMarkdownChanged));

    private static readonly DependencyProperty RenderTimerProperty = DependencyProperty.RegisterAttached(
        "RenderTimer", typeof(DispatcherTimer), typeof(MarkdownDocument));

    /// <summary>
    /// Gets the Markdown shown in a rich text box.
    /// </summary>
    /// <param name="element">The rich text box.</param>
    /// <returns>The Markdown.</returns>
    public static string? GetMarkdown(DependencyObject element) => (string?)element.GetValue(MarkdownProperty);

    /// <summary>
    /// Sets the Markdown to show in a rich text box.
    /// </summary>
    /// <param name="element">The rich text box.</param>
    /// <param name="value">The Markdown.</param>
    public static void SetMarkdown(DependencyObject element, string? value) => element.SetValue(MarkdownProperty, value);

    private static void OnMarkdownChanged(DependencyObject element, DependencyPropertyChangedEventArgs e)
    {
        if (element is not RichTextBox textBox) return;

        if (textBox.GetValue(RenderTimerProperty) is not DispatcherTimer timer)
        {
            timer = new DispatcherTimer(RenderDelay, DispatcherPriority.Background, (_, _) =>
            {
                ((DispatcherTimer)textBox.GetValue(RenderTimerProperty)).Stop();
                Render(textBox);
            }, textBox.Dispatcher);
            textBox.SetValue(RenderTimerProperty, timer);
            Render(textBox); // Show the first text straight away.
            return;
        }

        if (!timer.IsEnabled) timer.Start();
    }

    private static void Render(RichTextBox textBox)
    {
        var document = new FlowDocument { PagePadding = new Thickness(0), TextAlignment = TextAlignment.Left };
        document.SetBinding(TextElement.FontFamilyProperty, new System.Windows.Data.Binding(nameof(Control.FontFamily)) { Source = textBox });
        document.SetBinding(TextElement.FontSizeProperty, new System.Windows.Data.Binding(nameof(Control.FontSize)) { Source = textBox });
        document.SetResourceReference(TextElement.ForegroundProperty, "TextFillColorPrimaryBrush");

        var markdown = GetMarkdown(textBox);
        if (!string.IsNullOrEmpty(markdown))
        {
            document.Blocks.AddRange(Markdown.Parse(markdown, Pipeline).Select(ToBlock).OfType<Block>().ToList());

            // Space above a heading separates it from what came before, not from the top of the answer.
            if (document.Blocks.FirstBlock is { } first) first.Margin = first.Margin with { Top = 0 };
        }

        textBox.Document = document;
    }

    private static Block? ToBlock(Markdig.Syntax.Block block) => block switch
    {
        HeadingBlock heading => ToHeading(heading),
        ParagraphBlock paragraph => WithInlines(new Paragraph { Margin = new Thickness(0, 0, 0, 8) }, paragraph.Inline),
        ListBlock list => ToList(list),
        QuoteBlock quote => ToQuote(quote),
        CodeBlock code => ToCode(code),
        MarkdownTable table => ToTable(table),
        ThematicBreakBlock => new Paragraph
        {
            Margin = new Thickness(0, 4, 0, 12),
            BorderThickness = new Thickness(0, 0, 0, 1)
        }.WithResource(Block.BorderBrushProperty, "ControlStrokeColorDefaultBrush"),
        LeafBlock leaf => new Paragraph(new Run(leaf.Lines.ToString())), // e.g. raw HTML, shown as text
        _ => null
    };

    private static Paragraph ToHeading(HeadingBlock heading)
    {
        var paragraph = WithInlines(new Paragraph
        {
            Margin = new Thickness(0, heading.Level <= 2 ? 24 : 16, 0, heading.Level <= 2 ? 10 : 6),
            FontWeight = FontWeights.SemiBold
        }, heading.Inline);
        paragraph.SetResourceReference(TextElement.FontSizeProperty, heading.Level switch
        {
            1 => "FontSize.Title",
            2 => "FontSize.Subtitle",
            _ => "FontSize.BodyLarge"
        });
        return paragraph;
    }

    private static List ToList(ListBlock list)
    {
        var result = new List
        {
            MarkerStyle = list.IsOrdered ? TextMarkerStyle.Decimal : TextMarkerStyle.Disc,
            Margin = new Thickness(0, 0, 0, 8),
            Padding = new Thickness(20, 0, 0, 0)
        };
        if (list.IsOrdered && int.TryParse(list.OrderedStart, out var start)) result.StartIndex = start;

        foreach (var item in list.OfType<ListItemBlock>())
        {
            var listItem = new ListItem();
            foreach (var child in item.Select(ToBlock).OfType<Block>())
            {
                // Tight lists: a small gap under each item's paragraph, enough to tell wrapped items apart.
                if (child is Paragraph paragraph) paragraph.Margin = new Thickness(0, 0, 0, 4);
                listItem.Blocks.Add(child);
            }

            result.ListItems.Add(listItem);
        }

        return result;
    }

    private static Section ToQuote(QuoteBlock quote)
    {
        var section = new Section
        {
            Margin = new Thickness(0, 0, 0, 8),
            Padding = new Thickness(12, 0, 0, 0),
            BorderThickness = new Thickness(3, 0, 0, 0)
        };
        section.SetResourceReference(Block.BorderBrushProperty, "ControlStrokeColorDefaultBrush");
        section.SetResourceReference(TextElement.ForegroundProperty, "TextFillColorSecondaryBrush");
        section.Blocks.AddRange(quote.Select(ToBlock).OfType<Block>().ToList());
        return section;
    }

    private static Paragraph ToCode(CodeBlock code)
    {
        var paragraph = new Paragraph(new Run(code.Lines.ToString()))
        {
            FontFamily = CodeFont,
            Margin = new Thickness(0, 0, 0, 8),
            Padding = new Thickness(8)
        };
        paragraph.SetResourceReference(TextElement.BackgroundProperty, "ControlFillColorSecondaryBrush");
        return paragraph;
    }

    private static Table ToTable(MarkdownTable table)
    {
        var result = new Table { CellSpacing = 0, Margin = new Thickness(0, 0, 0, 8), BorderThickness = new Thickness(1, 1, 0, 0) };
        result.SetResourceReference(Block.BorderBrushProperty, "ControlStrokeColorDefaultBrush");
        var group = new TableRowGroup();
        result.RowGroups.Add(group);

        foreach (var row in table.OfType<Markdig.Extensions.Tables.TableRow>())
        {
            var tableRow = new TableRow();
            if (row.IsHeader)
            {
                tableRow.FontWeight = FontWeights.SemiBold;
                tableRow.SetResourceReference(TextElement.BackgroundProperty, "ControlFillColorSecondaryBrush");
            }

            foreach (var cell in row.OfType<Markdig.Extensions.Tables.TableCell>())
            {
                var tableCell = new TableCell
                {
                    Padding = new Thickness(6, 3, 6, 3),
                    BorderThickness = new Thickness(0, 0, 1, 1)
                };
                tableCell.SetResourceReference(TableCell.BorderBrushProperty, "ControlStrokeColorDefaultBrush");
                foreach (var block in cell.Select(ToBlock).OfType<Block>())
                {
                    block.Margin = new Thickness(0);
                    tableCell.Blocks.Add(block);
                }

                tableRow.Cells.Add(tableCell);
            }

            group.Rows.Add(tableRow);
        }

        return result;
    }

    private static T WithInlines<T>(T paragraph, ContainerInline? inlines) where T : Paragraph
    {
        if (inlines != null) paragraph.Inlines.AddRange(inlines.Select(ToInline).OfType<Inline>().ToList());
        return paragraph;
    }

    private static Inline? ToInline(Markdig.Syntax.Inlines.Inline inline)
    {
        switch (inline)
        {
            case LiteralInline literal:
                return new Run(literal.Content.ToString());
            case CodeInline code:
                // Non-breaking spaces keep bytes such as `53 00 08` on one line.
                var run = new Run(code.Content.Replace(' ', ' ')) { FontFamily = CodeFont };
                run.SetResourceReference(TextElement.BackgroundProperty, "ControlFillColorSecondaryBrush");
                return run;
            case LineBreakInline lineBreak:
                return lineBreak.IsHard ? new LineBreak() : new Run(" ");
            case LinkInline { IsImage: false } link:
                return Link(link.Url, link);
            case AutolinkInline autolink:
                return Link(autolink.Url, null);
            case EmphasisInline emphasis:
                Span span = emphasis.DelimiterChar == '~' ? new Span() : emphasis.DelimiterCount >= 2 ? new Bold() : new Italic();
                if (emphasis.DelimiterChar == '~') span.TextDecorations = TextDecorations.Strikethrough;
                span.Inlines.AddRange(emphasis.Select(ToInline).OfType<Inline>().ToList());
                return span;
            case ContainerInline container:
                var containerSpan = new Span();
                containerSpan.Inlines.AddRange(container.Select(ToInline).OfType<Inline>().ToList());
                return containerSpan;
            case HtmlInline html:
                return new Run(html.Tag);
            case HtmlEntityInline entity:
                return new Run(entity.Transcoded.ToString());
            default:
                return null;
        }
    }

    private static Inline Link(string? url, ContainerInline? text)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
        {
            // Only web links open; anything else is shown as text.
            var span = new Span();
            if (text != null) span.Inlines.AddRange(text.Select(ToInline).OfType<Inline>().ToList());
            else span.Inlines.Add(new Run(url));
            return span;
        }

        var hyperlink = new Hyperlink { NavigateUri = uri, ToolTip = uri.AbsoluteUri };
        if (text != null) hyperlink.Inlines.AddRange(text.Select(ToInline).OfType<Inline>().ToList());
        else hyperlink.Inlines.Add(new Run(url));
        hyperlink.RequestNavigate += (_, e) =>
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        };
        return hyperlink;
    }

    private static T WithResource<T>(this T element, DependencyProperty property, string resourceKey)
        where T : FrameworkContentElement
    {
        element.SetResourceReference(property, resourceKey);
        return element;
    }
}

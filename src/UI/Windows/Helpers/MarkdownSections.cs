using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Markdig;
using Markdig.Syntax;

namespace OSDPBench.Windows.Helpers;

/// <summary>
/// Shows Markdown one section at a time, so each can sit on its own card: set <c>helpers:MarkdownSections.Markdown</c>
/// on an <see cref="ItemsControl"/> whose item template shows a <see cref="MarkdownSection"/>. A section starts at each
/// <c>#</c> or <c>##</c> heading; text before the first heading is a section of its own. Updates are batched, and a
/// section whose text hasn't changed keeps its item, so an answer streaming in only re-renders the section being
/// written.
/// </summary>
public static class MarkdownSections
{
    private static readonly TimeSpan UpdateDelay = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Identifies the Markdown attached property.
    /// </summary>
    public static readonly DependencyProperty MarkdownProperty = DependencyProperty.RegisterAttached(
        "Markdown", typeof(string), typeof(MarkdownSections), new PropertyMetadata(null, OnMarkdownChanged));

    private static readonly DependencyProperty UpdateTimerProperty = DependencyProperty.RegisterAttached(
        "UpdateTimer", typeof(DispatcherTimer), typeof(MarkdownSections));

    /// <summary>
    /// Gets the Markdown shown in an items control.
    /// </summary>
    /// <param name="element">The items control.</param>
    /// <returns>The Markdown.</returns>
    public static string? GetMarkdown(DependencyObject element) => (string?)element.GetValue(MarkdownProperty);

    /// <summary>
    /// Sets the Markdown to show, one section per item, in an items control.
    /// </summary>
    /// <param name="element">The items control.</param>
    /// <param name="value">The Markdown.</param>
    public static void SetMarkdown(DependencyObject element, string? value) => element.SetValue(MarkdownProperty, value);

    /// <summary>
    /// Splits Markdown into sections, each starting at a <c>#</c> or <c>##</c> heading.
    /// </summary>
    /// <param name="markdown">The Markdown.</param>
    /// <returns>The sections' Markdown, trimmed, leaving out empty ones.</returns>
    public static IReadOnlyList<string> Split(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown)) return [];

        // Parsing finds the headings, so a line starting with # inside a code block doesn't start a section.
        var starts = Markdown.Parse(markdown).OfType<HeadingBlock>()
            .Where(heading => heading.Level <= 2)
            .Select(heading => heading.Span.Start)
            .Prepend(0)
            .Distinct()
            .ToList();

        return starts
            .Select((start, index) => markdown[start..(index + 1 < starts.Count ? starts[index + 1] : markdown.Length)].Trim())
            .Where(section => section.Length > 0)
            .ToList();
    }

    private static void OnMarkdownChanged(DependencyObject element, DependencyPropertyChangedEventArgs e)
    {
        if (element is not ItemsControl itemsControl) return;

        if (itemsControl.GetValue(UpdateTimerProperty) is not DispatcherTimer timer)
        {
            timer = new DispatcherTimer(UpdateDelay, DispatcherPriority.Background, (_, _) =>
            {
                ((DispatcherTimer)itemsControl.GetValue(UpdateTimerProperty)).Stop();
                Update(itemsControl);
            }, itemsControl.Dispatcher);
            itemsControl.SetValue(UpdateTimerProperty, timer);
            itemsControl.ItemsSource = new ObservableCollection<MarkdownSection>();
            Update(itemsControl); // Show the first text straight away.
            return;
        }

        if (!timer.IsEnabled) timer.Start();
    }

    private static void Update(ItemsControl itemsControl)
    {
        if (itemsControl.ItemsSource is not ObservableCollection<MarkdownSection> items) return;

        var sections = Split(GetMarkdown(itemsControl));
        for (var index = 0; index < sections.Count; index++)
        {
            if (index < items.Count) items[index].Markdown = sections[index];
            else items.Add(new MarkdownSection { Markdown = sections[index] });
        }

        while (items.Count > sections.Count) items.RemoveAt(items.Count - 1);
    }
}

/// <summary>
/// One section of Markdown shown by <see cref="MarkdownSections"/>.
/// </summary>
public sealed partial class MarkdownSection : ObservableObject
{
    /// <summary>
    /// The section's Markdown, starting with its heading.
    /// </summary>
    [ObservableProperty] private string _markdown = string.Empty;
}

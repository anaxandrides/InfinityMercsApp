using InfinityMercsApp.Services.Wiki;
using InfinityMercsApp.Views.Base;

namespace InfinityMercsApp.Views.Controls;

/// <summary>Draws wiki content blocks: section headers as bars, paragraphs, and bullet items (indented when nested).</summary>
public sealed class WikiBlocksView : ContentView
{
    public static readonly BindableProperty BlocksProperty =
        BindableProperty.Create(
            nameof(Blocks),
            typeof(IReadOnlyList<WikiContentBlock>),
            typeof(WikiBlocksView),
            null,
            propertyChanged: (bindable, _, _) => ((WikiBlocksView)bindable).Rebuild());

    private readonly VerticalStackLayout _stack = new() { Spacing = 4 };

    public WikiBlocksView() => Content = _stack;

    public IReadOnlyList<WikiContentBlock>? Blocks
    {
        get => (IReadOnlyList<WikiContentBlock>?)GetValue(BlocksProperty);
        set => SetValue(BlocksProperty, value);
    }

    private void Rebuild()
    {
        _stack.Children.Clear();
        foreach (var block in Blocks ?? [])
        {
            _stack.Children.Add(block.Type switch
            {
                WikiBlockType.SectionHeader => SectionHeader(block),
                WikiBlockType.Paragraph => Paragraph(block),
                _ => BulletItem(block)
            });
        }
    }

    private static Border SectionHeader(WikiContentBlock block) => new()
    {
        BackgroundColor = Color.FromArgb("#3A4554"),
        StrokeThickness = 0,
        Padding = new Thickness(10, 6),
        Margin = new Thickness(0, 8, 0, 2),
        Content = new Label
        {
            Text = block.Text,
            Style = LabelStyle("LabelBody"),
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#E6EBF2")
        }
    };

    private static Label Paragraph(WikiContentBlock block) => new()
    {
        Text = block.Text,
        Style = LabelStyle("LabelBody"),
        TextColor = Color.FromArgb("#B5C0CE"),
        LineBreakMode = LineBreakMode.WordWrap,
        Margin = new Thickness(0, 4, 0, 0)
    };

    private static Grid BulletItem(WikiContentBlock block)
    {
        var row = new Grid
        {
            ColumnSpacing = 4,
            Margin = new Thickness(block.IndentLevel == 0 ? 4 : 20, 3, 0, 0),
            ColumnDefinitions = [new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star)]
        };

        row.Children.Add(new Label
        {
            Text = "▶",
            Style = LabelStyle("LabelMicro"),
            TextColor = Color.FromArgb("#8A97A8"),
            VerticalTextAlignment = TextAlignment.Start,
            Margin = new Thickness(0, 3, 0, 0)
        });

        var text = new Label
        {
            Text = block.Text,
            Style = LabelStyle("LabelBody"),
            FontAttributes = block.Bold ? FontAttributes.Bold : FontAttributes.None,
            TextColor = block.Bold ? Color.FromArgb("#E6EBF2") : Color.FromArgb("#B5C0CE"),
            LineBreakMode = LineBreakMode.WordWrap
        };
        Grid.SetColumn(text, 1);
        row.Children.Add(text);
        return row;
    }

    private static Style LabelStyle(string key) => AppStyles.Named(key);
}

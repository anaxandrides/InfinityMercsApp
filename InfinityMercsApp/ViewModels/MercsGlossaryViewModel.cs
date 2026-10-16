using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InfinityMercsApp.Services.Glossary;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.Services.Packaging;
using InfinityMercsApp.ViewModels.Base;
using InfinityMercsApp.ViewModels.Layouts;

namespace InfinityMercsApp.ViewModels;

/// <summary>The MERCS rules index; choosing a rule shows its text.</summary>
public partial class MercsGlossaryViewModel(
    INavigationService navigationService,
    IAppPackageFiles packageFiles) : AdaptiveViewModelBase<SplitPaneLayout>(navigationService, mode => SplitPaneLayout.For(mode, Sizes))
{
    private static readonly SplitPaneSizes Sizes = new(300, 320, 340, 860);

    public const string NoSelectionTitle = "Select a rule";
    private const string NoSelectionText = "Choose an entry from the Rules Index to view its details.";
    private const string NoDetailsText = "No details available for this rule yet.";

    // Bumped on every selection so a slow load cannot overwrite a later choice.
    private int _selectionVersion;

    public ObservableCollection<GlossaryEntry> GlossaryItems { get; } = [];

    [ObservableProperty]
    public partial GlossaryEntry? SelectedItem { get; set; }

    [ObservableProperty]
    public partial string SelectedTitle { get; set; } = NoSelectionTitle;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ContentSource))]
    public partial string SelectedContentHtml { get; set; } = MercsGlossary.TextPage(NoSelectionText);

    /// <summary>Whether the rule text replaces the index on narrow screens.</summary>
    [ObservableProperty]
    public partial bool IsShowingDetail { get; set; }

    [ObservableProperty]
    public partial bool IsIndexPaneVisible { get; set; } = true;

    [ObservableProperty]
    public partial bool IsDetailPaneVisible { get; set; }

    /// <summary>
    /// Rules-index text size, scaled to the available width so long titles stay readable and fit inside the
    /// index box. Compact has the full screen width; the medium and wider rail is narrow.
    /// </summary>
    [ObservableProperty]
    public partial double ListFontSize { get; set; } = 17d;

    protected override void OnLayoutApplied()
    {
        ShowPanes();
        ListFontSize = LayoutMode switch
        {
            AdaptiveLayoutMode.Compact => 17d,
            AdaptiveLayoutMode.Medium => 14d,
            AdaptiveLayoutMode.Expanded => 15d,
            _ => 16d
        };
    }

    partial void OnIsShowingDetailChanged(bool value) => ShowPanes();

    private void ShowPanes()
    {
        IsIndexPaneVisible = Layout.ShowsList(IsShowingDetail);
        IsDetailPaneVisible = Layout.ShowsDetail(IsShowingDetail);
    }

    public HtmlWebViewSource ContentSource => new() { Html = SelectedContentHtml };

    /// <summary>Loads the index once and shows the first rule, leaving narrow screens on the index.</summary>
    public override async Task InitializeAsync()
    {
        if (IsInitialized)
        {
            return;
        }

        try
        {
            GlossaryItems.Clear();
            foreach (var item in MercsGlossary.ParseIndex(await packageFiles.ReadTextAsync(MercsGlossary.IndexFile)))
            {
                GlossaryItems.Add(item);
            }

            SelectedItem = GlossaryItems.FirstOrDefault(item => !item.IsHeader);
            IsShowingDetail = false;
        }
        catch (Exception ex)
        {
            SelectedTitle = "Load Error";
            SelectedContentHtml = MercsGlossary.TextPage($"Failed to load glossary data: {ex.Message}");
        }
    }

    // Set while a header tap is undone, so the current rule is left as it was.
    private bool _revertingHeaderSelection;

    partial void OnSelectedItemChanged(GlossaryEntry? oldValue, GlossaryEntry? newValue)
    {
        if (_revertingHeaderSelection)
        {
            return;
        }

        // Section headers can't be selected: keep the current rule.
        if (newValue?.IsHeader == true)
        {
            _revertingHeaderSelection = true;
            SelectedItem = oldValue;
            _revertingHeaderSelection = false;
            return;
        }

        IsShowingDetail = newValue is not null;

        if (newValue is null)
        {
            SelectedTitle = NoSelectionTitle;
            SelectedContentHtml = MercsGlossary.TextPage(NoSelectionText);
            return;
        }

        SelectedTitle = newValue.Title;
        _ = LoadContentAsync(newValue, ++_selectionVersion);
    }

    [RelayCommand]
    private void CloseDetail() => IsShowingDetail = false;

    private async Task LoadContentAsync(GlossaryEntry entry, int version)
    {
        string html;
        if (string.IsNullOrWhiteSpace(entry.MarkdownFile))
        {
            html = MercsGlossary.TextPage(NoDetailsText);
        }
        else
        {
            try
            {
                var markdown = await packageFiles.ReadTextAsync(MercsGlossary.MarkdownPath(entry));
                html = MercsGlossary.MarkdownPage(string.IsNullOrWhiteSpace(markdown) ? NoDetailsText : markdown);
            }
            catch (Exception ex)
            {
                html = MercsGlossary.TextPage($"Failed to load glossary markdown file '{entry.MarkdownFile}': {ex.Message}");
            }
        }

        if (version == _selectionVersion)
        {
            SelectedContentHtml = html;
        }
    }
}

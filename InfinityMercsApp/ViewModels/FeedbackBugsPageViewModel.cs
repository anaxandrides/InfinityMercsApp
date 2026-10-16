using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InfinityMercsApp.Services.Feedback;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.ViewModels.Base;

namespace InfinityMercsApp.ViewModels;

/// <summary>Collects and submits a feedback/bug ticket.</summary>
public partial class FeedbackBugsPageViewModel(
    INavigationService navigationService,
    IFeedbackService? feedbackService) : ViewModelBase(navigationService)
{
    public IReadOnlyList<string> Categories { get; } = ["Bug", "Feedback"];

    [ObservableProperty]
    private string? _selectedCategory = "Bug";

    [ObservableProperty]
    private string? _userEmail;

    [ObservableProperty]
    private string? _ticketTitle;

    [ObservableProperty]
    private string? _description;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
    private bool _isSubmitting;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusColor))]
    private bool _isStatusError;

    public Color StatusColor => IsStatusError ? Colors.OrangeRed : Colors.LightGreen;

    private bool CanSubmit() => !IsSubmitting;

    [RelayCommand(CanExecute = nameof(CanSubmit))]
    private async Task SubmitAsync()
    {
        if (feedbackService is null)
        {
            SetStatus("Feedback service unavailable.", isError: true);
            return;
        }

        var category = SelectedCategory ?? string.Empty;
        var title = TicketTitle?.Trim() ?? string.Empty;
        var description = Description?.Trim() ?? string.Empty;
        var userEmail = UserEmail?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(category))
        {
            SetStatus("Please choose a category.", isError: true);
            return;
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            SetStatus("Title is required.", isError: true);
            return;
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            SetStatus("Description is required.", isError: true);
            return;
        }

        if (!IsValidEmail(userEmail))
        {
            SetStatus("A valid email is required.", isError: true);
            return;
        }

        try
        {
            IsSubmitting = true;
            SetStatus(string.Empty);

            var result = await feedbackService.SubmitAsync(new FeedbackSubmission
            {
                Category = category,
                Title = title,
                Description = description,
                UserEmail = userEmail
            });

            SetStatus(result.Message, isError: !result.Success);

            if (result.Success)
            {
                TicketTitle = string.Empty;
                Description = string.Empty;
            }
        }
        catch (Exception ex)
        {
            SetStatus($"Submit failed: {ex.Message}", isError: true);
        }
        finally
        {
            IsSubmitting = false;
        }
    }

    private void SetStatus(string message, bool isError = false)
    {
        StatusMessage = message;
        IsStatusError = isError;
    }

    internal static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.CultureInvariant);
    }
}

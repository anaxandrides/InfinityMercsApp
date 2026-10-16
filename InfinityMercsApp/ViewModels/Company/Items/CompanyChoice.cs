using CommunityToolkit.Mvvm.Input;
using InfinityMercsApp.ViewModels.Layouts;

namespace InfinityMercsApp.ViewModels.Company.Items;

/// <summary>A company card: its title and icon, the command that opens it, and where it sits.</summary>
public sealed record CompanyChoice(string Title, string IconSource, IAsyncRelayCommand Command, GridCell Cell, CompanyCardStyle Style);

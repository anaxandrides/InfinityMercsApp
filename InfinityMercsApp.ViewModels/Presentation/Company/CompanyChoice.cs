using CommunityToolkit.Mvvm.Input;
using InfinityMercsApp.Presentation.Layouts;

namespace InfinityMercsApp.Presentation.Company;

/// <summary>A company card: its title and icon, the command that opens it, and where it sits.</summary>
public sealed record CompanyChoice(string Title, string IconSource, IAsyncRelayCommand Command, GridCell Cell, CompanyCardStyle Style);

using CommunityToolkit.Mvvm.ComponentModel;

namespace InfinityMercsApp.Web.UnitTests.Components.Base;

/// <summary>A view model with one property to change.</summary>
public sealed partial class ObservableObjectProbe : ObservableObject
{
    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;
}

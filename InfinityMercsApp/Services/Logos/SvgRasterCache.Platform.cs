using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;

namespace InfinityMercsApp.Services.Logos;

/// <summary>
/// MAUI wiring for <see cref="SvgRasterCache"/>. Kept in its own partial so the core cache logic
/// can be compiled into the plain unit-test project without pulling in MAUI Essentials.
/// </summary>
public sealed partial class SvgRasterCache
{
    /// <summary>Parameterless constructor used by DI; binds the cache to the real app environment.</summary>
    public SvgRasterCache()
        : this(
            Path.Combine(FileSystem.Current.AppDataDirectory, "svg-cache"),
            static () => DeviceDisplay.Current.MainDisplayInfo.Density,
            static async (name, _) => (Stream?)await FileSystem.Current.OpenAppPackageFileAsync(name))
    {
    }
}

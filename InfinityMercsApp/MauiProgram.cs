using CommunityToolkit.Maui;
using InfinityMercsApp.Infrastructure;
using InfinityMercsApp.Infrastructure.Options;
using InfinityMercsApp.Services;
using InfinityMercsApp.ViewModels;
using InfinityMercsApp.Views;
using InfinityMercsApp.Views.UnitEncyclopedia;
using Microsoft.Extensions.Logging;
using SkiaSharp.Views.Maui.Controls.Hosting;

namespace InfinityMercsApp;

public static class MauiProgram
{
	// The plain net10.0 target exists only so the unit tests can reference the app; it never runs MauiProgram.
#pragma warning disable CA1416
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
			.UseSkiaSharp()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
				fonts.AddFont("Ethnocentric-Regular.otf", "EthnocentricRegular");
				fonts.AddFont("Conthrax-SemiBold.otf", "ConthraxSemiBold");
			});

		// A database from before the typed-table layout is discarded; the network import rebuilds it.
		var dbPath = Path.Combine(FileSystem.Current.AppDataDirectory, "infinitymercs.db3");
		DatabaseReset.DiscardIfOutdated(dbPath);

		builder.Services
				.AddInfrastructureServices()
				.AddAppServices()
				.AddViewModels()
				.AddPages()
				// Change this once AppSettings is set up. Wish MAUI did this by default.
				.AddSingleton(new SQLIteConfiguration() { DBPath = dbPath });

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
#pragma warning restore CA1416
}

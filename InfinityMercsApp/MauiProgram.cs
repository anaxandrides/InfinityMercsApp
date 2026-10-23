using System.Runtime.Versioning;
using CommunityToolkit.Maui;
using InfinityMercsApp.Infrastructure;
using InfinityMercsApp.Infrastructure.Options;
using InfinityMercsApp.Services.Import;
using InfinityMercsApp.Views.Fonts;
using Microsoft.Extensions.Logging;
using SkiaSharp.Views.Maui.Controls.Hosting;

namespace InfinityMercsApp;

public static class MauiProgram
{
	// Only the platform apps start; the plain net10.0 build exists so the unit tests can reference the app.
	[SupportedOSPlatform("android21.0")]
	[SupportedOSPlatform("ios15.0")]
	[SupportedOSPlatform("maccatalyst15.0")]
	[SupportedOSPlatform("windows10.0.17763")]
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
			.UseSkiaSharp()
			.ConfigureFonts(fonts =>
			{
				foreach (var font in AppFonts.All)
				{
					fonts.AddFont(font.FileName, font.Alias);
				}
			});

#if WINDOWS
		// The unpackaged Windows build keeps its data under the publisher name and the app id. The publisher was the
		// template's "User Name" until it became infinitymercsteam, and the app id was com.companyname.infinitymercsapp
		// until it became com.infinitymercsteam.infinitymercs; the newest old folder is copied across once.
		var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		foreach (var publisher in (IReadOnlyList<string>)["infinitymercsteam", "User Name"])
		{
			if (InfinityMercsApp.Services.Records.DataFolderMove.CopyIfNew(
				Path.Combine(localAppData, publisher, "com.companyname.infinitymercsapp", "Data"),
				FileSystem.Current.AppDataDirectory))
			{
				break;
			}
		}

		InfinityMercsApp.WinUI.WindowsToolbar.HideOverflowButtons();
#endif

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
}

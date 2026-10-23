using System.Collections;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using CommunityToolkit.Mvvm.Messaging;
using InfinityMercsApp.Messages;
using InfinityMercsApp.Services.Sync;
using InfinityMercsApp.Services.Telemetry;
using InfinityMercsApp.Views.Adaptive;
using InfinityMercsApp.Views.Splash;

namespace InfinityMercsApp;

public partial class App : Application
{
	/// <summary>The startup milestone reported once the army data is in and the main screen shows.</summary>
	public const string MainScreenMilestone = "MainScreen";

	private readonly SplashPage _splashPage;
	private readonly ITelemetryCoordinator _telemetry;

	public App(SplashPage splashPage, ISyncCoordinator sync, ITelemetryRecorder telemetryRecorder, ITelemetryCoordinator telemetry, TimeProvider time)
	{
		_splashPage = splashPage;
		_telemetry = telemetry;
		CrashLog.Initialize();

		// Sync and telemetry start once the army data is in and the app has opened; the telemetry left by an earlier
		// session (a crash's report among it) goes then.
		WeakReferenceMessenger.Default.Register<SplashCompletedMessage>(this, (_, _) =>
		{
			if (LaunchClock.MillisecondsSinceLaunch() is { } milliseconds)
			{
				telemetryRecorder.Record(TelemetryEvents.Startup(MainScreenMilestone, milliseconds, time.GetUtcNow()));
			}

			sync.Start();
			telemetry.Start();
		});

		// Datapad identity is dark-only; pin the theme so system-drawn
		// controls (pickers, dialogs) don't flip to a light variant.
		UserAppTheme = AppTheme.Dark;

		AppDomain.CurrentDomain.UnhandledException += (_, args) =>
		{
			CrashLog.Write("AppDomain.CurrentDomain.UnhandledException", args.ExceptionObject as Exception);
			if (args.ExceptionObject is Exception exception)
			{
				telemetryRecorder.Record(TelemetryEvents.Crash("AppDomain.UnhandledException", exception, time.GetUtcNow()));
			}
		};

		TaskScheduler.UnobservedTaskException += (_, args) =>
		{
			CrashLog.Write("TaskScheduler.UnobservedTaskException", args.Exception);
			telemetryRecorder.Record(TelemetryEvents.Crash("TaskScheduler.UnobservedTaskException", args.Exception, time.GetUtcNow()));
		};

		// Intentionally NOT subscribing to FirstChanceException: it fires for every
		// caught exception anywhere in the framework (and MAUI/Android throw many during
		// startup). Logging each one synchronously to a file under a lock taxed cold start
		// and steady-state alike. UnhandledException/UnobservedTaskException cover real crashes.

		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new AppWindow(_splashPage);
	}

	/// <summary>The app may be closed while in the background, so what telemetry is waiting goes now.</summary>
	protected override void OnSleep()
	{
		base.OnSleep();
		_telemetry.SendSoon();
	}

	internal static class CrashLog
	{
		private static readonly object Sync = new();
		private static string _logPath = "crash.log";

		public static string LogPath => _logPath;

		public static void Initialize()
		{
			try
			{
				_logPath = Path.Combine(FileSystem.Current.AppDataDirectory, "runtime-crash.log");
				Write("Crash logger initialized.");
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"CrashLog.Initialize failed: {ex.Message}");
			}
		}

		public static void Write(string source, Exception? exception = null)
		{
			try
			{
				var sb = new StringBuilder();
				sb.AppendLine("================================================================");
				sb.AppendLine($"[{DateTimeOffset.Now:O}] {source}");

				// Only real crashes (those carrying an exception) get the full diagnostic
				// dump; informational writes like the init line stay terse.
				if (exception is not null)
				{
					AppendSystemReport(sb);
					sb.AppendLine();
					sb.AppendLine("----- Exception -----");
					AppendExceptionReport(sb, exception, depth: 0);
				}

				sb.AppendLine();

				lock (Sync)
				{
					File.AppendAllText(_logPath, sb.ToString());
				}
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"CrashLog.Write failed: {ex.Message}");
			}
		}

		// Snapshot of the app/device/runtime state at crash time. Every field is fetched
		// behind its own guard so that one unavailable value can't suppress the rest.
		private static void AppendSystemReport(StringBuilder sb)
		{
			sb.AppendLine("----- Environment -----");
			TryAppend(sb, "App", () =>
				$"{AppInfo.Current.Name} {AppInfo.Current.VersionString} (build {AppInfo.Current.BuildString}), package {AppInfo.Current.PackageName}");
			TryAppend(sb, "Device", () =>
				$"{DeviceInfo.Current.Manufacturer} {DeviceInfo.Current.Model} \"{DeviceInfo.Current.Name}\" ({DeviceInfo.Current.Idiom}, {DeviceInfo.Current.DeviceType})");
			TryAppend(sb, "Platform", () =>
				$"{DeviceInfo.Current.Platform} {DeviceInfo.Current.VersionString}");
			TryAppend(sb, "OS", () =>
				$"{RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture})");
			TryAppend(sb, "Runtime", () =>
				$"{RuntimeInformation.FrameworkDescription} ({RuntimeInformation.ProcessArchitecture})");
			TryAppend(sb, "Display", () =>
			{
				var d = DeviceDisplay.Current.MainDisplayInfo;
				return $"{d.Width}x{d.Height} @ {d.Density}x, {d.Orientation}";
			});
			TryAppend(sb, "Culture", () =>
				$"{CultureInfo.CurrentCulture.Name} / UI {CultureInfo.CurrentUICulture.Name}");
			TryAppend(sb, "Memory", () =>
				$"working set {Environment.WorkingSet / (1024 * 1024)} MB, GC heap {GC.GetTotalMemory(false) / (1024 * 1024)} MB");
			TryAppend(sb, "CPU", () => $"{Environment.ProcessorCount} logical processors");
			TryAppend(sb, "Thread", () =>
				$"#{Environment.CurrentManagedThreadId} (pool: {Thread.CurrentThread.IsThreadPoolThread})");
		}

		private static void TryAppend(StringBuilder sb, string label, Func<string> value)
		{
			try
			{
				sb.AppendLine($"{label}: {value()}");
			}
			catch (Exception ex)
			{
				sb.AppendLine($"{label}: <unavailable: {ex.Message}>");
			}
		}

		// Walks the full inner-exception chain (and every branch of an AggregateException)
		// dumping type, message, HResult, Source, the Data bag, and stack trace at each level.
		private static void AppendExceptionReport(StringBuilder sb, Exception exception, int depth)
		{
			var indent = new string(' ', depth * 2);

			sb.AppendLine($"{indent}[{depth}] {exception.GetType().FullName}: {exception.Message}");
			sb.AppendLine($"{indent}    HResult: 0x{exception.HResult:X8}");
			if (!string.IsNullOrEmpty(exception.Source))
			{
				sb.AppendLine($"{indent}    Source: {exception.Source}");
			}

			if (exception.Data.Count > 0)
			{
				sb.AppendLine($"{indent}    Data:");
				foreach (DictionaryEntry entry in exception.Data)
				{
					sb.AppendLine($"{indent}      {entry.Key} = {entry.Value}");
				}
			}

			if (!string.IsNullOrEmpty(exception.StackTrace))
			{
				sb.AppendLine($"{indent}    StackTrace:");
				sb.AppendLine(exception.StackTrace);
			}

			if (exception is AggregateException aggregate && aggregate.InnerExceptions.Count > 0)
			{
				for (var i = 0; i < aggregate.InnerExceptions.Count; i++)
				{
					sb.AppendLine($"{indent}    --- AggregateException inner [{i}] ---");
					AppendExceptionReport(sb, aggregate.InnerExceptions[i], depth + 1);
				}
			}
			else if (exception.InnerException is not null)
			{
				AppendExceptionReport(sb, exception.InnerException, depth + 1);
			}
		}
	}
}

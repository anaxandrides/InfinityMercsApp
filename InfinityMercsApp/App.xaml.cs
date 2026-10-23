using System.Collections;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using CommunityToolkit.Mvvm.Messaging;
using InfinityMercsApp.Messages;
using InfinityMercsApp.Services.Sync;
using InfinityMercsApp.Views.Adaptive;
using InfinityMercsApp.Views.Splash;

namespace InfinityMercsApp;

public partial class App : Application
{
	private readonly SplashPage _splashPage;

	public App(SplashPage splashPage, ISyncCoordinator sync)
	{
		_splashPage = splashPage;
		CrashLog.Initialize();

		// Sync starts once the army data is in and the app has opened.
		WeakReferenceMessenger.Default.Register<SplashCompletedMessage>(this, (_, _) => sync.Start());

		// Datapad identity is dark-only; pin the theme so system-drawn
		// controls (pickers, dialogs) don't flip to a light variant.
		UserAppTheme = AppTheme.Dark;

		AppDomain.CurrentDomain.UnhandledException += (_, args) =>
		{
			CrashLog.Write("AppDomain.CurrentDomain.UnhandledException", args.ExceptionObject as Exception);
		};

		TaskScheduler.UnobservedTaskException += (_, args) =>
		{
			CrashLog.Write("TaskScheduler.UnobservedTaskException", args.Exception);
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

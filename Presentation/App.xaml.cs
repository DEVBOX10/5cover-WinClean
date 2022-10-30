﻿global using System.IO;

global using static Humanizer.StringExtensions;

using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;

using CommandLine;

using Scover.WinClean.BusinessLogic;
using Scover.WinClean.BusinessLogic.Scripts;
using Scover.WinClean.DataAccess;
using Scover.WinClean.Presentation.Logging;
using Scover.WinClean.Presentation.Windows;
using Scover.WinClean.Resources;

namespace Scover.WinClean.Presentation;

/// <summary>Handles the startup / shutdown strategy and holds data related to the Presentation layer.</summary>
public sealed partial class App
{
    private record Callbacks(Action WarnOnUpdate,
                             InvalidScriptDataCallback ReloadElseIgnore,
                             Action<Exception> WarnOnUnhandledException,
                             FSOperationCallback RetryElseFail);

    private static Logger? _logger;
    private static ScriptCollection? _scripts;

    /// <summary>The application's logger, available after startup.</summary>
    public static Logger Logger => _logger.AssertNotNull();

    /// <summary>The application's scripts, available after startup.</summary>
    public static ScriptCollection Scripts => _scripts.AssertNotNull();

    private static void Initialize(Logger logger, Callbacks callbacks)
    {
        // 1. Set the logger
        _logger = logger;

        // 2. Add the unhandled exception handler
        Current.DispatcherUnhandledException += (_, args) => callbacks.WarnOnUnhandledException(args.Exception);

        // 3. Set the app file callback
        AppInfo.OpenAppFileRetryElseFail = callbacks.RetryElseFail;

        // 4. Check for updates
        if (SourceControlClient.Instance.Value.LatestVersionName != AppInfo.Version)
        {
            callbacks.WarnOnUpdate();
        }

        // 5. Load scripts.
        _scripts = ScriptCollection.LoadScripts(AppDirectory.ScriptsDir, callbacks.ReloadElseIgnore);
    }

    private static void StartConsole(string[] args)
    {
        // Get invariant (en-US) output.
        CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("kernel32.dll")]
        static extern bool AttachConsole(uint dwProcessId);
        AttachConsole(unchecked((uint)-1));

        Initialize(new ConsoleLogger(), consoleCallbacks);

        _ = Parser.Default.ParseArguments<CommandLineOptions>(args)
            .WithParsed(options => Environment.ExitCode = options.Execute());
        Current.Shutdown();
    }

    private static void StartGui()
    {
        Initialize(new CsvLogger(), uiCallbacks);

        new MainWindow().Show();
    }

    private void ApplicationExit(object? sender, ExitEventArgs? e)
    {
        Scripts.Save();
        AppInfo.Settings.Save();
        Logger.Log(Logs.Exiting);
    }

    private void ApplicationStartup(object? sender, StartupEventArgs? e)
    {
        if (e?.Args.Any() ?? false)
        {
            StartConsole(e.Args);
        }
        else
        {
            StartGui();
        }
    }
}
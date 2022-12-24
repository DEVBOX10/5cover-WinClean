﻿using Scover.Dialogs;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Humanizer;

using Scover.WinClean.BusinessLogic.Scripts;
using Scover.WinClean.Presentation.Logging;
using Scover.WinClean.Resources;

namespace Scover.WinClean.Presentation;

/// <summary>
/// Walks the user through the multi-step high-level operation of executing multiple scripts asynchronously by displaying a
/// dialog tracking the progress.
/// </summary>
public sealed partial class ScriptExecutionWizard
{
    private readonly Page _askRestartCompleted;
    private readonly Page _askRestorePoint;
    private readonly MultiPageDialog _dialog;
    private readonly Page _executionProgress;
    private readonly Dictionary<Page, NextPageSelector> _nextPageSelectors = new();
    private readonly Page _restorePointProgress;

    private readonly IReadOnlyList<Script> _scripts;

    /// <summary>Executes the script(s) and displays a dialog tracking the progress.</summary>
    public void Execute() => _dialog.Show();

    private static TimeSpan? GetExecutionTime(Script script)
        => App.ScriptExecutionTimes.TryGetValue(script.InvariantName, out TimeSpan executionTime) ? executionTime : null;

    private static void RestartSystem()
    {
        Logs.RebootingForAppMaintenance.Log();
        Process.Start("shutdown", "/g /t 0 /d p:4:1")?.Dispose();
        System.Windows.Application.Current.Shutdown();
    }

    private async IAsyncEnumerable<TimeSpan> ExecuteScripts([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        Logs.StartingExecution.FormatWith(_scripts.Count).Log(LogLevel.Info);
        Stopwatch stopwatch = new();

        foreach (Script script in _scripts)
        {
            stopwatch.Restart();
            await script.ExecuteAsync(cancellationToken);
            yield return stopwatch.Elapsed;
        }
        Logs.ScriptsExecuted.Log(LogLevel.Info);
    }
}
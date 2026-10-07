using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Сборка отдельных серверного и клиентского билдов для проверки
/// клиент-серверной архитектуры. Роль выбирается при запуске аргументами
/// <c>-server</c> / <c>-client</c> (см. <see cref="NetworkBootstrapper"/>),
/// поэтому оба билда собираются одинаково.
/// </summary>
/// <remarks>
/// Из командной строки:
/// <code>
///   unity build . --target StandaloneWindows64 --execute-method BuildScript.BuildServer
///   unity build . --target StandaloneWindows64 --execute-method BuildScript.BuildClient
/// </code>
/// Из запущенного редактора (через unity_eval), без блокировки на время сборки:
/// <code>
///   BuildScript.RequestAllBuilds();   // ставит сервер и клиент в очередь
///   BuildScript.GetReport();          // состояние и результат
/// </code>
/// </remarks>
public static class BuildScript
{
    private const string OutputRoot = "Builds";

    /// <summary>Сцены сборки. Пока в проекте игровая сцена одна.</summary>
    private static readonly string[] Scenes =
    {
        "Assets/Scenes/SampleScene.unity"
    };

    private static readonly Queue<string> Pending = new Queue<string>();
    private static readonly List<string> Finished = new List<string>();
    private static bool _running;

    // Точки входа для unity build --execute-method
    public static void BuildServer() => Build("Server");

    public static void BuildClient() => Build("Client");

    /// <summary>Ставит в очередь серверную сборку (не блокирует вызов).</summary>
    public static void RequestServerBuild() => Enqueue("Server");

    /// <summary>Ставит в очередь клиентскую сборку (не блокирует вызов).</summary>
    public static void RequestClientBuild() => Enqueue("Client");

    /// <summary>Ставит в очередь обе сборки: сначала сервер, затем клиент.</summary>
    public static void RequestAllBuilds()
    {
        Enqueue("Server");
        Enqueue("Client");
    }

    /// <summary>Состояние: idle, queued, running или finished.</summary>
    public static string GetStatus()
    {
        if (_running) return "running";
        if (Pending.Count > 0) return "queued:" + Pending.Count;
        return Finished.Count == 0 ? "idle" : "finished";
    }

    /// <summary>Отчёт по завершённым сборкам и очереди.</summary>
    public static string GetReport()
    {
        string report = Finished.Count > 0 ? string.Join(" | ", Finished) : "сборок ещё не было";
        if (Pending.Count > 0) report += " | в очереди: " + string.Join(",", Pending);
        return report;
    }

    private static void Enqueue(string name)
    {
        Pending.Enqueue(name);

        if (_running) return;

        EditorApplication.delayCall += Pump;
    }

    private static void Pump()
    {
        if (Pending.Count == 0) return;

        string name = Pending.Dequeue();
        _running = true;

        try
        {
            Build(name);
        }
        catch (Exception e)
        {
            Finished.Add(name + ": ИСКЛЮЧЕНИЕ " + e.Message);
            Pending.Clear();
            _running = false;
            return;
        }

        _running = false;

        if (Pending.Count > 0)
        {
            EditorApplication.delayCall += Pump;
        }
    }

    private static void Build(string name)
    {
        string directory = Path.Combine(OutputRoot, name);
        Directory.CreateDirectory(directory);

        string location = Path.Combine(directory, $"{name}.exe");

        var options = new BuildPlayerOptions
        {
            scenes = Scenes,
            locationPathName = location,
            target = BuildTarget.StandaloneWindows64,
            targetGroup = BuildTargetGroup.Standalone,
            options = BuildOptions.None
        };

        Debug.Log($"[BuildScript] Сборка {name}: {location}");
        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        string result = $"{name}: {summary.result}, {summary.totalSize / (1024 * 1024)} МБ, " +
                        $"ошибок {summary.totalErrors}, предупреждений {summary.totalWarnings}";

        if (summary.result == BuildResult.Succeeded)
        {
            Finished.Add(result);
            Debug.Log("[BuildScript] " + result);
            return;
        }

        Finished.Add(result + " → " + location);
        Debug.LogError("[BuildScript] " + result);

        if (Application.isBatchMode)
        {
            EditorApplication.Exit(1);
        }
    }
}

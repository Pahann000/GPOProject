using System;
using Mirror;
using UnityEngine;

/// <summary>
/// Запуск отдельной сборки в роли сервера или клиента по аргументам командной строки.
/// Нужен для проверки клиент-серверной архитектуры раздельными процессами:
/// <code>
///   Server.exe -batchmode -nographics -server
///   Client.exe -batchmode -nographics -client -address 127.0.0.1
/// </code>
/// Без этих аргументов (обычный запуск и редактор) ничего не происходит —
/// роль выбирает разработчик или игровой UI.
/// </summary>
public static class NetworkBootstrapper
{
    private const string ServerFlag = "-server";
    private const string ClientFlag = "-client";
    private const string AddressFlag = "-address";
    private const string PortFlag = "-port";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void StartFromCommandLine()
    {
        string[] args = Environment.GetCommandLineArgs();

        bool asServer = HasFlag(args, ServerFlag);
        bool asClient = HasFlag(args, ClientFlag);

        if (!asServer && !asClient) return;

        NetworkManager manager = UnityEngine.Object.FindFirstObjectByType<NetworkManager>();
        if (manager == null)
        {
            Debug.LogError("[Bootstrap] В сцене нет NetworkManager — запуск роли невозможен.");
            return;
        }

        ApplyTransportSettings(manager, args);

        if (asServer)
        {
            manager.StartServer();
            Debug.Log("[Bootstrap] Запуск в роли сервера.");
        }
        else
        {
            manager.StartClient();
            Debug.Log($"[Bootstrap] Запуск в роли клиента, адрес {manager.networkAddress}.");
        }
    }

    /// <summary>
    /// Порт и адрес можно переопределить аргументами, чтобы поднять несколько
    /// экземпляров на одной машине.
    /// </summary>
    private static void ApplyTransportSettings(NetworkManager manager, string[] args)
    {
        string address = GetValue(args, AddressFlag);
        if (!string.IsNullOrEmpty(address))
        {
            manager.networkAddress = address;
        }

        string portText = GetValue(args, PortFlag);
        if (!int.TryParse(portText, out int port) || port <= 0) return;

        Transport transport = manager.transport;
        if (transport == null)
        {
            Debug.LogWarning("[Bootstrap] У NetworkManager не назначен транспорт: порт не применён.");
            return;
        }

        // Порт есть у конкретных транспортов (KCP и т.п.), у базового Transport его нет.
        var portField = transport.GetType().GetField("port");
        if (portField == null || portField.FieldType != typeof(ushort))
        {
            Debug.LogWarning($"[Bootstrap] Транспорт {transport.GetType().Name} не поддерживает переопределение порта.");
            return;
        }

        portField.SetValue(transport, (ushort)port);
        Debug.Log($"[Bootstrap] Порт транспорта изменён на {port}.");
    }

    private static bool HasFlag(string[] args, string flag)
    {
        foreach (string arg in args)
        {
            if (string.Equals(arg, flag, StringComparison.OrdinalIgnoreCase)) return true;
        }

        return false;
    }

    private static string GetValue(string[] args, string flag)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], flag, StringComparison.OrdinalIgnoreCase))
            {
                return args[i + 1];
            }
        }

        return null;
    }
}

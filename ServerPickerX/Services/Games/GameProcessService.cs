using ServerPickerX.Constants;
using ServerPickerX.Services.Servers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ServerPickerX.Services.Games
{
    // Finds the running process of the game behind a given game mode. The watcher polls
    // this only while waiting for a launch, a handful of name lookups every few seconds
    // costs far less than a WMI process watcher and needs no extra permissions. Once the
    // game is found, its exit is awaited on the process itself instead.
    public class GameProcessService : IGameProcessService
    {
        private readonly ServerDefinitionProvider _serverDefinitionProvider;

        public GameProcessService(ServerDefinitionProvider serverDefinitionProvider)
        {
            _serverDefinitionProvider = serverDefinitionProvider;
        }

        public IReadOnlyList<string> GetProcessNames(string gameMode)
        {
            ServerDefinition? serverDefinition = _serverDefinitionProvider.GetServerDefinitionByGameMode(gameMode);

            if (serverDefinition == null)
            {
                return Array.Empty<string>();
            }

            // A definition can name its own processes, which is the escape hatch for a
            // game that renames its executable. Otherwise fall back to the built in map
            List<string> definitionProcessNames = (serverDefinition.ProcessNames ?? new List<string>())
                .Where(processName => !string.IsNullOrWhiteSpace(processName))
                .Select(processName => processName.Trim())
                .ToList();

            return definitionProcessNames.Count > 0
                ? definitionProcessNames
                : GameProcesses.GetByAppId(serverDefinition.AppId.ToString());
        }

        public Process? FindRunningGameProcess(string gameMode)
        {
            foreach (string processName in GetProcessNames(gameMode))
            {
                Process[] processes;

                try
                {
                    processes = Process.GetProcessesByName(NormalizeProcessName(processName));
                }
                catch (Exception)
                {
                    // A process list that cannot be read is treated as "not running"
                    // rather than throwing out of a timer tick
                    continue;
                }

                if (processes.Length == 0)
                {
                    continue;
                }

                // Only the first is handed back, the rest are released here
                for (int i = 1; i < processes.Length; i++)
                {
                    processes[i].Dispose();
                }

                return processes[0];
            }

            return null;
        }

        // Process.GetProcessesByName never wants the extension, strip it in case a
        // hand edited ServerDefinitions.json includes one
        private static string NormalizeProcessName(string processName)
        {
            return processName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                ? processName[..^4]
                : processName;
        }
    }
}

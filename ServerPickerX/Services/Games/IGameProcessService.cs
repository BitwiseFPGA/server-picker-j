using System.Collections.Generic;
using System.Diagnostics;

namespace ServerPickerX.Services.Games
{
    public interface IGameProcessService
    {
        IReadOnlyList<string> GetProcessNames(string gameMode);

        // One running process of the game, or null when it is not running. The caller
        // owns the returned process and has to dispose it
        Process? FindRunningGameProcess(string gameMode);
    }
}

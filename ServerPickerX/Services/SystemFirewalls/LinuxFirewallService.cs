using ServerPickerX.Models;
using ServerPickerX.Services.Localizations;
using ServerPickerX.Services.Loggers;
using ServerPickerX.Services.MessageBoxes;
using ServerPickerX.Services.Processes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace ServerPickerX.Services.SystemFirewalls
{
    public class LinuxFirewallService(
        ILoggerService _loggerService,
        ILocalizationService _localizationService,
        IMessageBoxService _messageBoxService,
        IProcessService _processService
        ) : ISystemFirewallService
    {
        // Waits a few seconds for the iptables lock instead of failing outright.
        // Docker and fail2ban both hold it briefly, and a rule that was not added
        // because something else held the lock for a moment is a block the user
        // asked for and did not get
        private const string LockWait = "-w 5";


        // Both passes read the rules once first, then only add what is missing and
        // only remove what is there. Every server is tried, and the ones that fail
        // are reported together at the end so the app can show each server as it
        // really is. A failed iptables call used to be written to the log while the
        // app carried on as though it had worked
        public async Task BlockServersAsync(ObservableCollection<ServerModel> serverModels)
        {
            Dictionary<string, int> dropRules = await ReadDropRulesAsync();

            List<ServerModel> failedServers = [];

            foreach (var serverModel in serverModels)
            {
                try
                {
                    // An empty address list would turn into "-s -j DROP", which
                    // iptables rejects, so it is reported as the failure it is
                    if (serverModel.RelayModels.Count == 0)
                    {
                        throw new InvalidOperationException("the server has no addresses to block");
                    }

                    // An address that already has a rule does not get a second one.
                    // Copies used to pile up when a blocked server was blocked again,
                    // and unblocking took off only one of them, so the server stayed
                    // blocked while the app showed it as unblocked
                    List<string> missingAddresses = GetAddresses(serverModel)
                        .Where(address => !dropRules.ContainsKey(address))
                        .ToList();

                    if (missingAddresses.Count == 0)
                    {
                        continue;
                    }

                    await RunIptablesAsync($"{LockWait} -A INPUT -s {string.Join(",", missingAddresses)} -j DROP");

                    foreach (string address in missingAddresses)
                    {
                        dropRules[address] = 1;
                    }
                }
                catch (Exception ex)
                {
                    failedServers.Add(serverModel);

                    await _loggerService.LogErrorAsync($"Could not block {serverModel.Description}", ex.Message);
                }
            }

            if (failedServers.Count > 0)
            {
                throw new FirewallOperationException(failedServers);
            }
        }

        public async Task UnblockServersAsync(ObservableCollection<ServerModel> serverModels)
        {
            Dictionary<string, int> dropRules = await ReadDropRulesAsync();

            List<ServerModel> failedServers = [];

            foreach (var serverModel in serverModels)
            {
                try
                {
                    // Takes off every copy of every rule for this server's addresses.
                    // A server with nothing in the firewall needs no command at all:
                    // iptables treats deleting a rule that is not there as an error,
                    // and "Unblock all" goes through every server, blocked or not.
                    // Each round lowers the counts, so this always comes to an end
                    while (true)
                    {
                        List<string> presentAddresses = GetAddresses(serverModel)
                            .Where(address => dropRules.GetValueOrDefault(address) > 0)
                            .ToList();

                        if (presentAddresses.Count == 0)
                        {
                            break;
                        }

                        await RunIptablesAsync($"{LockWait} -D INPUT -s {string.Join(",", presentAddresses)} -j DROP");

                        foreach (string address in presentAddresses)
                        {
                            dropRules[address]--;
                        }
                    }
                }
                catch (Exception ex)
                {
                    failedServers.Add(serverModel);

                    await _loggerService.LogErrorAsync($"Could not unblock {serverModel.Description}", ex.Message);
                }
            }

            if (failedServers.Count > 0)
            {
                throw new FirewallOperationException(failedServers);
            }
        }

        private static IEnumerable<string> GetAddresses(ServerModel serverModel)
            => serverModel.RelayModels.Select(relayModel => relayModel.IPv4).Distinct();

        // The DROP rules on INPUT in the shape this app writes them, as address and
        // number of copies. Rules of any other shape are not this app's and are
        // never counted, so they are never touched. When the rules cannot be read
        // at all nothing has been changed yet, so that failure is all or nothing
        private async Task<Dictionary<string, int>> ReadDropRulesAsync()
        {
            string listing = await RunIptablesAsync("-S INPUT");

            Dictionary<string, int> dropRules = new(StringComparer.Ordinal);

            foreach (string line in listing.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                // "-A INPUT -s 155.133.226.68/32 -j DROP"
                string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length != 6
                    || parts[0] != "-A"
                    || parts[1] != "INPUT"
                    || parts[2] != "-s"
                    || parts[4] != "-j"
                    || parts[5] != "DROP")
                {
                    continue;
                }

                string address = parts[3].EndsWith("/32", StringComparison.Ordinal)
                    ? parts[3][..^3]
                    : parts[3];

                dropRules[address] = dropRules.GetValueOrDefault(address) + 1;
            }

            return dropRules;
        }

        // Runs one iptables command and hands back what it printed. Anything but a
        // clean exit is thrown, with what iptables said, rather than logged and
        // passed over
        private async Task<string> RunIptablesAsync(string arguments)
        {
            using var process = _processService.CreateProcess("sudo");

            process.StartInfo.Arguments = "iptables " + arguments;

            process.Start();

            // Both outputs are read while the command runs rather than after it
            // exits. A command that fills either pipe stops and waits for it to be
            // emptied, so waiting for the exit first would leave both sides waiting
            // on each other for good
            Task<string> stdOutTask = process.StandardOutput.ReadToEndAsync();
            Task<string> stdErrTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            string stdOut = await stdOutTask;
            string stdErr = (await stdErrTask).Trim();

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"iptables {arguments} exited with {process.ExitCode}: {stdErr}"
                    );
            }

            return stdOut;
        }

        // Deletes only the DROP rules this app added, one per server. The previous
        // implementation ran "iptables -F", which flushes every rule in the table and
        // would take unrelated rules, including ones protecting remote access, with it
        public async Task<bool> ResetFirewallAsync(ObservableCollection<ServerModel> serverModels)
        {
            bool confirmed = await _messageBoxService.ShowMessageBoxConfirmationAsync(
                _localizationService.GetLocaleValue("MessageBoxInfoTitle"),
                _localizationService.GetLocaleValue("FirewallResetConfirmDialogue"),
                MsBox.Avalonia.Enums.Icon.Warning
                );

            if (!confirmed)
            {
                return false;
            }

            try
            {
                await UnblockServersAsync(serverModels);

                await _loggerService.LogInfoAsync($"Removed firewall rules for {serverModels.Count} servers");

                await _messageBoxService.ShowMessageBoxAsync(
                    _localizationService.GetLocaleValue("MessageBoxInfoTitle"),
                    string.Format(
                        _localizationService.GetLocaleValue("FirewallResetSuccessDialogue"),
                        serverModels.Count
                        ),
                    MsBox.Avalonia.Enums.Icon.Success
                    );

                return true;
            }
            catch (Exception ex)
            {
                await _loggerService.LogErrorAsync(ex.Message);
                throw;
            }
        }

        // iptables rules carry no marker tying them back to a server, so the saved
        // state stays authoritative here rather than being reconciled against nothing
        public Task<List<ServerModel>?> GetBlockedServersAsync(ObservableCollection<ServerModel> serverModels)
            => Task.FromResult<List<ServerModel>?>(null);
    }
}

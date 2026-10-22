using ServerPickerX.Models;
using System;
using System.Collections.Generic;

namespace ServerPickerX.Services.SystemFirewalls
{
    // Thrown after a block or unblock pass has tried every server and some of
    // them did not go through. Every other server in the pass did, so the caller
    // can show each one as it really is instead of treating the whole pass as
    // all or nothing. Before this, one failure either aborted the pass halfway,
    // leaving rules in the firewall the app showed as unblocked, or (on Linux)
    // was written to the log while the app reported success
    public class FirewallOperationException(IReadOnlyList<ServerModel> failedServers)
        : Exception($"{failedServers.Count} server(s) could not be changed in the firewall")
    {
        public IReadOnlyList<ServerModel> FailedServers { get; } = failedServers;
    }
}

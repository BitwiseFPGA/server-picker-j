using ServerPickerX.Helpers;
using ServerPickerX.Models;
using ServerPickerX.Services.Loggers;
using ServerPickerX.Services.MessageBoxes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace ServerPickerX.Services.Servers
{
    public class ConfiguredServerDataService(
        ServerDefinition _serverDefinition,
        ILoggerService _logger,
        IMessageBoxService _messageBoxService,
        HttpClient _httpClient
        ) : IServerDataService
    {
        private ServerData _serverData = new();

        public async Task<bool> LoadServersAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync(string.Format(_serverDefinition.ResponseUrlTemplate, _serverDefinition.AppId));

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception(
                        "Failed to load servers!" + Environment.NewLine + Environment.NewLine +
                        "- Verify your internet connection or firewall are working and enabled" + Environment.NewLine +
                        "- Make sure to run the app as admin or with sudo level execution"
                    );
                }

                using var stream = await response.Content.ReadAsStreamAsync();
                var mainJson = await JsonNode.ParseAsync(stream) as JsonObject;

                if (mainJson?["revision"] == null || mainJson?["pops"] == null)
                {
                    throw new Exception("Server relay data unavailable. Please try again later.");
                }

                string revision = mainJson["revision"]!.ToString();
                
                _serverData.Revision = revision;

                ProcessServers(mainJson, _serverData);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Failed to load {_serverDefinition.DisplayName ?? _serverDefinition.Id} servers", ex.Message);
                await _messageBoxService.ShowMessageBoxAsync("Error", ex.Message);

                return false;
            }

            return true;
        }

        private void ProcessServers(JsonObject mainJson, ServerData serverData)
        {
            var unclusteredServers = new List<ServerModel>();
            var clusteredServers = new List<ServerModel>();

            foreach (KeyValuePair<string, JsonNode?> server in (JsonObject)mainJson["pops"]!)
            {
                // One entry Valve gets wrong costs that one server, not the whole
                // list. Before, a missing description or an oddly shaped relay list
                // threw out of this loop and the app showed no servers at all
                if (server.Value is not JsonObject pop || pop["relays"] is not JsonArray relays)
                {
                    continue;
                }

                string? serverDescription = pop["desc"]?.ToString();

                if (string.IsNullOrWhiteSpace(serverDescription) || !IsServerAccepted(serverDescription))
                {
                    continue;
                }

                var serverModel = new ServerModel
                {
                    Flag = FlagResolver.GetFlagPath(serverDescription),
                    Name = server.Key,
                    Description = serverDescription,
                };

                foreach (JsonNode? relay in relays)
                {
                    // Only real IPv4 addresses are kept. A missing one used to come
                    // through as "", which crashed the app the moment that server
                    // was pinged, and anything here also ends up in a firewall rule
                    // or an iptables command, where it must be an address and
                    // nothing else
                    string? address = (relay as JsonObject)?["ipv4"]?.ToString();

                    if (address != null
                        && IPAddress.TryParse(address, out IPAddress? parsed)
                        && parsed.AddressFamily == AddressFamily.InterNetwork)
                    {
                        serverModel.RelayModels.Add(new RelayModel { IPv4 = parsed.ToString() });
                    }
                }

                // A server with no address left can be neither pinged nor blocked
                if (serverModel.RelayModels.Count == 0)
                {
                    continue;
                }

                unclusteredServers.Add(serverModel);

                string clusterName = GetClusterKeywords().FirstOrDefault(keyword => serverDescription.Contains(keyword), "");
                if (!string.IsNullOrEmpty(clusterName))
                {
                    var clusteredServer = clusteredServers.FirstOrDefault(s => s.Description == clusterName, new ServerModel());

                    clusteredServer.RelayModels.AddRange(serverModel.RelayModels);

                    if (string.IsNullOrEmpty(clusteredServer.Description))
                    {
                        clusteredServer.Flag = serverModel.Flag;
                        clusteredServer.Name = "cluster";
                        clusteredServer.Description = clusterName;

                        clusteredServers.Add(clusteredServer);
                    }
                }
                else
                {
                    clusteredServers.Add(serverModel);
                }
            }

            serverData.UnclusteredServers = unclusteredServers;
            serverData.ClusteredServers = clusteredServers;
        }

        public string GetFetchedRevision()
        {
            return _serverData.Revision;
        }

        public ServerData GetServerData()
        {
            return _serverData;
        }

        public bool IsServerAccepted(string serverDescription)
        {
            if (_serverDefinition.KeywordFilterMode?.ToLower() == "include")
            {
                return _serverDefinition.Keywords.Any(k => serverDescription.Contains(k));
            }

            if (_serverDefinition.KeywordFilterMode?.ToLower() == "exclude")
            {
                return !_serverDefinition.Keywords.Any(k => serverDescription.Contains(k));
            }

            return true;
        }

        public List<string> GetClusterKeywords()
        {
            return _serverDefinition.ClusterKeywords ?? [];
        }
    }
}

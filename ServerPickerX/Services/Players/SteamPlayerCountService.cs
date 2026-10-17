using ServerPickerX.Services.Loggers;
using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace ServerPickerX.Services.Players
{
    // One small request to Steam's public API, the same host the server list comes from.
    // Only the game's app ID is sent, there is nothing about the user in it
    public class SteamPlayerCountService : IPlayerCountService
    {
        private const string PlayerCountUrl =
            "https://api.steampowered.com/ISteamUserStats/GetNumberOfCurrentPlayers/v1/?appid=";

        // The count is a nice to have, so a slow answer is dropped rather than waited on
        private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(5);

        private readonly HttpClient _httpClient;
        private readonly ILoggerService _loggerService;

        public SteamPlayerCountService(HttpClient httpClient, ILoggerService loggerService)
        {
            _httpClient = httpClient;
            _loggerService = loggerService;
        }

        public async Task<int?> GetCurrentPlayerCountAsync(string appId)
        {
            if (string.IsNullOrWhiteSpace(appId) || !appId.All(char.IsAsciiDigit))
            {
                return null;
            }

            try
            {
                using CancellationTokenSource timeout = new(RequestTimeout);
                using HttpResponseMessage response = await _httpClient.GetAsync(PlayerCountUrl + appId, timeout.Token);

                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                string body = await response.Content.ReadAsStringAsync(timeout.Token);

                return ParsePlayerCount(body);
            }
            catch (Exception ex)
            {
                // Offline, blocked or slow. The count simply stays hidden
                await _loggerService.LogWarningAsync("Could not get the player count from Steam: " + ex.Message);

                return null;
            }
        }

        // The answer looks like {"response":{"player_count":12345,"result":1}}
        public static int? ParsePlayerCount(string body)
        {
            try
            {
                JsonNode? response = JsonNode.Parse(body)?["response"];

                if (response == null || response["result"]?.GetValue<int>() != 1)
                {
                    return null;
                }

                int? playerCount = response["player_count"]?.GetValue<int>();

                return playerCount >= 0 ? playerCount : null;
            }
            catch (Exception)
            {
                // Anything that is not the expected shape counts as no answer
                return null;
            }
        }
    }
}

using ServerPickerX.Services.Loggers;
using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace ServerPickerX.Services.Players
{
    // Small requests to Steam's public API, the same host the server list comes from.
    // Only the game's app ID is sent, there is nothing about the user in it
    public class SteamPlayerCountService : IPlayerCountService
    {
        // Straight answer for one game, but Valve does not give one for every game
        private const string PlayerCountUrl =
            "https://api.steampowered.com/ISteamUserStats/GetNumberOfCurrentPlayers/v1/?appid=";

        // The list behind Steam's own most played charts. It does carry the games the call
        // above refuses, Deadlock among them, as long as they are in the top 100
        private const string ChartsUrl =
            "https://api.steampowered.com/ISteamChartsService/GetGamesByConcurrentPlayers/v1/";

        // The count is a nice to have, so a slow answer is dropped rather than waited on.
        // Long enough for the first connection of the session, which also sets up TLS
        private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(10);

        private readonly HttpClient _httpClient;
        private readonly ILoggerService _loggerService;

        public SteamPlayerCountService(HttpClient httpClient, ILoggerService loggerService)
        {
            _httpClient = httpClient;
            _loggerService = loggerService;
        }

        public async Task<int?> GetCurrentPlayerCountAsync(string appId)
        {
            if (string.IsNullOrWhiteSpace(appId)
                || !appId.All(char.IsAsciiDigit)
                || !int.TryParse(appId, out int appIdNumber))
            {
                return null;
            }

            string? body = await GetAsync(PlayerCountUrl + appId);

            int? playerCount = body == null ? null : ParsePlayerCount(body);

            if (playerCount != null)
            {
                return playerCount;
            }

            // Steam has no count for this game by itself, so the charts are asked next
            body = await GetAsync(ChartsUrl);

            if (body == null)
            {
                return null;
            }

            playerCount = ParseChartedPlayerCount(body, appIdNumber);

            if (playerCount == null)
            {
                // A game outside the top 100 is not in that list, and then the count stays
                // hidden rather than showing a number that belongs to another game
                await _loggerService.LogWarningAsync($"Steam has no player count for app {appIdNumber}");
            }

            return playerCount;
        }

        // Returns the body, or null when Steam cannot be reached or refuses
        private async Task<string?> GetAsync(string url)
        {
            try
            {
                using CancellationTokenSource timeout = new(RequestTimeout);
                using HttpResponseMessage response = await _httpClient.GetAsync(url, timeout.Token);

                if (!response.IsSuccessStatusCode)
                {
                    await _loggerService.LogWarningAsync(
                        $"Steam answered {(int)response.StatusCode} for the player count");

                    return null;
                }

                return await response.Content.ReadAsStringAsync(timeout.Token);
            }
            catch (Exception ex)
            {
                // Offline, blocked or slow. The count simply stays hidden
                await _loggerService.LogWarningAsync("Could not get the player count from Steam: " + ex.Message);

                return null;
            }
        }

        // The answer looks like {"response":{"player_count":12345,"result":1}}, and for a
        // game Valve keeps to itself, such as Deadlock, like {"response":{"result":42}}
        public static int? ParsePlayerCount(string body)
        {
            try
            {
                JsonNode? response = JsonNode.Parse(body)?["response"];

                if (response == null)
                {
                    return null;
                }

                // 1 means Steam has an answer. Anything else is a refusal, but the field
                // itself is not required: the count is what matters
                if (response["result"] is JsonNode result && result.GetValue<int>() != 1)
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

        // The charts answer looks like
        // {"response":{"ranks":[{"rank":5,"appid":1422450,"concurrent_in_game":191987,...}]}}
        public static int? ParseChartedPlayerCount(string body, int appId)
        {
            try
            {
                if (JsonNode.Parse(body)?["response"]?["ranks"] is not JsonArray ranks)
                {
                    return null;
                }

                foreach (JsonNode? game in ranks)
                {
                    if (game?["appid"]?.GetValue<int>() != appId)
                    {
                        continue;
                    }

                    int? playerCount = game["concurrent_in_game"]?.GetValue<int>();

                    return playerCount >= 0 ? playerCount : null;
                }

                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}

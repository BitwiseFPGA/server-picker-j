using Moq;
using ServerPickerX.Services.Loggers;
using ServerPickerX.Services.Players;

namespace ServerPickerX.Tests.Services.Players
{
    public class SteamPlayerCountServiceTests
    {
        // Shortened, in the shape Steam's most played charts come back in
        private const string ChartsBody =
            "{\"response\":{\"last_update\":1792213103,\"ranks\":[" +
            "{\"rank\":1,\"appid\":730,\"concurrent_in_game\":559543,\"peak_in_game\":1297140}," +
            "{\"rank\":5,\"appid\":1422450,\"concurrent_in_game\":191987,\"peak_in_game\":303347}," +
            "{\"rank\":100,\"appid\":284160,\"concurrent_in_game\":13416,\"peak_in_game\":28133}]}}";

        [Theory]
        [InlineData("{\"response\":{\"player_count\":18240,\"result\":1}}", 18240)]
        [InlineData("{\"response\":{\"player_count\":0,\"result\":1}}", 0)]
        // The count alone is enough, Steam does not have to say "result" with it
        [InlineData("{\"response\":{\"player_count\":18240}}", 18240)]
        public void ReadsThePlayerCount(string body, int expected)
        {
            Assert.Equal(expected, SteamPlayerCountService.ParsePlayerCount(body));
        }

        [Theory]
        // What Steam answers for Deadlock: it has no count of its own to give
        [InlineData("{\"response\":{\"result\":42}}")]
        [InlineData("{\"response\":{\"player_count\":5,\"result\":2}}")]
        [InlineData("{\"response\":{\"player_count\":\"lots\",\"result\":1}}")]
        [InlineData("{\"response\":{\"player_count\":-3,\"result\":1}}")]
        [InlineData("{}")]
        [InlineData("not json")]
        [InlineData("")]
        public void AnythingElse_CountsAsNoAnswer(string body)
        {
            Assert.Null(SteamPlayerCountService.ParsePlayerCount(body));
        }

        [Theory]
        [InlineData(1422450, 191987)]
        [InlineData(730, 559543)]
        [InlineData(284160, 13416)]
        public void ReadsThePlayerCountFromTheCharts(int appId, int expected)
        {
            Assert.Equal(expected, SteamPlayerCountService.ParseChartedPlayerCount(ChartsBody, appId));
        }

        // A game outside the top 100 is not in the list, and no other game's number is shown
        [Fact]
        public void GamesOutsideTheCharts_HaveNoCount()
        {
            Assert.Null(SteamPlayerCountService.ParseChartedPlayerCount(ChartsBody, 3065800));
        }

        [Theory]
        [InlineData("{\"response\":{\"ranks\":[{\"rank\":1,\"appid\":730}]}}")]
        [InlineData("{\"response\":{\"ranks\":[]}}")]
        [InlineData("{\"response\":{}}")]
        [InlineData("not json")]
        [InlineData("")]
        public void AChartsAnswerWithoutNumbers_CountsAsNoAnswer(string body)
        {
            Assert.Null(SteamPlayerCountService.ParseChartedPlayerCount(body, 730));
        }

        [Theory]
        [InlineData("")]
        [InlineData("1422450&key=x")]
        [InlineData("abc")]
        public async Task NonNumericAppIds_AreNeverSent(string appId)
        {
            Mock<ILoggerService> logger = new();
            using HttpClient httpClient = new();
            SteamPlayerCountService service = new(httpClient, logger.Object);

            Assert.Null(await service.GetCurrentPlayerCountAsync(appId));

            logger.VerifyNoOtherCalls();
        }
    }
}

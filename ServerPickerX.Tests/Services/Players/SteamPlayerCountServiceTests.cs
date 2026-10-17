using Moq;
using ServerPickerX.Services.Loggers;
using ServerPickerX.Services.Players;

namespace ServerPickerX.Tests.Services.Players
{
    public class SteamPlayerCountServiceTests
    {
        [Theory]
        [InlineData("{\"response\":{\"player_count\":18240,\"result\":1}}", 18240)]
        [InlineData("{\"response\":{\"player_count\":0,\"result\":1}}", 0)]
        public void ReadsThePlayerCount(string body, int expected)
        {
            Assert.Equal(expected, SteamPlayerCountService.ParsePlayerCount(body));
        }

        [Theory]
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

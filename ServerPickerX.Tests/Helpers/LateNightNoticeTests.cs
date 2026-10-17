using ServerPickerX.Helpers;

namespace ServerPickerX.Tests.Helpers
{
    public class LateNightNoticeTests
    {
        private static readonly DateTime TwoThirtyAm = new(2026, 10, 17, 2, 30, 0);

        [Fact]
        public void Shows_LateAtNight_WithMostServersBlocked()
        {
            Assert.True(LateNightNotice.ShouldShow(TwoThirtyAm, 7, 10, false, "2026-10-16"));
        }

        [Theory]
        [InlineData(0, 59, false)]
        [InlineData(1, 0, true)]
        [InlineData(6, 59, true)]
        [InlineData(7, 0, false)]
        [InlineData(15, 0, false)]
        [InlineData(23, 30, false)]
        public void Shows_OnlyBetweenOneAndSevenInTheMorning(int hour, int minute, bool expected)
        {
            DateTime localNow = new(2026, 10, 17, hour, minute, 0);

            Assert.Equal(expected, LateNightNotice.ShouldShow(localNow, 7, 10, false, string.Empty));
        }

        [Theory]
        [InlineData(5, 10, false)]
        [InlineData(6, 10, true)]
        [InlineData(10, 10, true)]
        [InlineData(1, 1, true)]
        [InlineData(0, 0, false)]
        public void Shows_OnlyWhenMoreThanHalfAreBlocked(int blocked, int total, bool expected)
        {
            Assert.Equal(expected, LateNightNotice.ShouldShow(TwoThirtyAm, blocked, total, false, null));
        }

        [Fact]
        public void NeverShows_AfterDontShowAgain()
        {
            Assert.False(LateNightNotice.ShouldShow(TwoThirtyAm, 10, 10, true, null));
        }

        [Fact]
        public void ShowsOnlyOnce_PerDay()
        {
            string today = LateNightNotice.ToDateKey(TwoThirtyAm);

            Assert.Equal("2026-10-17", today);
            Assert.False(LateNightNotice.ShouldShow(TwoThirtyAm, 10, 10, false, today));
        }
    }
}

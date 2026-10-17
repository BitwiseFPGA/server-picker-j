using System;
using System.Globalization;

namespace ServerPickerX.Helpers
{
    // Decides when to explain that long queues late at night come from fewer players being
    // online, not from the app. Only the PC's own clock is used, nothing leaves the PC
    public static class LateNightNotice
    {
        // Between 1 and 7 in the morning, local time
        public const int StartHour = 1;
        public const int EndHour = 7;

        public static bool ShouldShow(
            DateTime localNow,
            int blockedServerCount,
            int serverCount,
            bool hiddenForGood,
            string? lastShownDate
            )
        {
            if (hiddenForGood || serverCount <= 0)
            {
                return false;
            }

            if (localNow.Hour < StartHour || localNow.Hour >= EndHour)
            {
                return false;
            }

            // Only once most servers are blocked, that is when queues get longer because of it
            if (blockedServerCount * 2 <= serverCount)
            {
                return false;
            }

            // At most once a day
            return !string.Equals(lastShownDate, ToDateKey(localNow), StringComparison.Ordinal);
        }

        public static string ToDateKey(DateTime localNow)
        {
            return localNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }
    }
}

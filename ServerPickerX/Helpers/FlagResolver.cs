using System;
using System.Collections.Generic;
using System.Linq;

namespace ServerPickerX.Helpers
{
    // Picks a server's flag from the place in its name, so a server Valve adds later shows the
    // right flag without an update as long as its country already has one. Flags are stored once
    // per country as Assets/flags/<country code>.png. Hong Kong, Taiwan and the like get their
    // own flag whenever the server name says so, the app never decides that by itself
    public static class FlagResolver
    {
        private const string FlagFolder = "/Assets/flags/";

        // Every code here must have a matching image in Assets/flags, a test checks this
        private static readonly Dictionary<string, string> CountryCodes = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Argentina"] = "ar",
            ["Australia"] = "au",
            ["Austria"] = "at",
            ["Brazil"] = "br",
            ["Chile"] = "cl",
            ["China"] = "cn",
            ["England"] = "gb",
            ["Finland"] = "fi",
            ["France"] = "fr",
            ["Germany"] = "de",
            ["Guam"] = "gu",
            ["Hong Kong"] = "hk",
            ["India"] = "in",
            ["Japan"] = "jp",
            ["Korea"] = "kr",
            ["Netherlands"] = "nl",
            ["Peru"] = "pe",
            ["Poland"] = "pl",
            ["Singapore"] = "sg",
            ["South Africa"] = "za",
            ["South Korea"] = "kr",
            ["Spain"] = "es",
            ["Sweden"] = "se",
            ["UAE"] = "ae",
            ["UK"] = "gb",
            ["United Arab Emirates"] = "ae",
            ["United Kingdom"] = "gb",
            ["United States"] = "us",
            ["USA"] = "us",

            // US servers are named by city and state, for example "Dallas (Texas)"
            ["New York"] = "us",
            ["Alabama"] = "us",
            ["Alaska"] = "us",
            ["Arizona"] = "us",
            ["Arkansas"] = "us",
            ["California"] = "us",
            ["Colorado"] = "us",
            ["Connecticut"] = "us",
            ["Delaware"] = "us",
            ["District of Columbia"] = "us",
            ["Florida"] = "us",
            ["Georgia"] = "us",
            ["Hawaii"] = "us",
            ["Idaho"] = "us",
            ["Illinois"] = "us",
            ["Indiana"] = "us",
            ["Iowa"] = "us",
            ["Kansas"] = "us",
            ["Kentucky"] = "us",
            ["Louisiana"] = "us",
            ["Maine"] = "us",
            ["Maryland"] = "us",
            ["Massachusetts"] = "us",
            ["Michigan"] = "us",
            ["Minnesota"] = "us",
            ["Mississippi"] = "us",
            ["Missouri"] = "us",
            ["Montana"] = "us",
            ["Nebraska"] = "us",
            ["Nevada"] = "us",
            ["New Hampshire"] = "us",
            ["New Jersey"] = "us",
            ["New Mexico"] = "us",
            ["North Carolina"] = "us",
            ["North Dakota"] = "us",
            ["Ohio"] = "us",
            ["Oklahoma"] = "us",
            ["Oregon"] = "us",
            ["Pennsylvania"] = "us",
            ["Rhode Island"] = "us",
            ["South Carolina"] = "us",
            ["South Dakota"] = "us",
            ["Tennessee"] = "us",
            ["Texas"] = "us",
            ["Utah"] = "us",
            ["Vermont"] = "us",
            ["Virginia"] = "us",
            ["Washington"] = "us",
            ["West Virginia"] = "us",
            ["Wisconsin"] = "us",
            ["Wyoming"] = "us",
        };

        // Longest first, so "South Korea" wins over "Korea" and "West Virginia" over "Virginia"
        private static readonly string[] PlacesLongestFirst = CountryCodes.Keys
            .OrderByDescending(place => place.Length)
            .ToArray();

        public static IReadOnlyCollection<string> KnownCountryCodes { get; } = CountryCodes.Values
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        // Returns the flag's asset path, or an empty string when the place is not known yet
        public static string GetFlagPath(string? serverDescription)
        {
            string? countryCode = GetCountryCode(serverDescription);

            return countryCode == null
                ? string.Empty
                : FlagFolder + countryCode + ".png";
        }

        public static string? GetCountryCode(string? serverDescription)
        {
            if (string.IsNullOrWhiteSpace(serverDescription))
            {
                return null;
            }

            // Most names end with the place in brackets: "Frankfurt (Germany)",
            // "Datapacket Fujairah (UAE)" or "Perfect World (sha-4) Backbone (Shanghai, China)"
            int open = serverDescription.LastIndexOf('(');
            int close = serverDescription.LastIndexOf(')');

            if (open >= 0 && close > open)
            {
                string place = serverDescription[(open + 1)..close];
                int comma = place.LastIndexOf(',');

                if (comma >= 0)
                {
                    place = place[(comma + 1)..];
                }

                if (CountryCodes.TryGetValue(place.Trim(), out string? bracketCode))
                {
                    return bracketCode;
                }
            }

            // The rest name the place directly, such as "Singapore" or "Multiplay Hong Kong"
            foreach (string place in PlacesLongestFirst)
            {
                if (ContainsWholeWords(serverDescription, place))
                {
                    return CountryCodes[place];
                }
            }

            return null;
        }

        // Matches whole words only, so "Indiana" is never read as "India"
        private static bool ContainsWholeWords(string text, string words)
        {
            int index = text.IndexOf(words, StringComparison.OrdinalIgnoreCase);

            while (index >= 0)
            {
                int end = index + words.Length;

                bool startsWord = index == 0 || !char.IsLetterOrDigit(text[index - 1]);
                bool endsWord = end == text.Length || !char.IsLetterOrDigit(text[end]);

                if (startsWord && endsWord)
                {
                    return true;
                }

                index = text.IndexOf(words, index + 1, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }
    }
}

using ServerPickerX.Helpers;

namespace ServerPickerX.Tests.Helpers
{
    public class FlagResolverTests
    {
        // Every server name the app knew when flags moved to one image per country,
        // with the country each one has to show
        [Theory]
        [InlineData("Alibaba Cloud Beijing - Mobile (China)", "cn")]
        [InlineData("Alibaba Cloud Beijing - Telecom (China)", "cn")]
        [InlineData("Alibaba Cloud Beijing - Unicom (China)", "cn")]
        [InlineData("Alibaba Cloud Chengdu - Mobile (China)", "cn")]
        [InlineData("Alibaba Cloud Chengdu - Telecom (China)", "cn")]
        [InlineData("Alibaba Cloud Chengdu - Unicom (China)", "cn")]
        [InlineData("Alibaba Cloud Shanghai - Mobile (China)", "cn")]
        [InlineData("Alibaba Cloud Shanghai - Telecom (China)", "cn")]
        [InlineData("Alibaba Cloud Shanghai - Unicom (China)", "cn")]
        [InlineData("Alibaba Cloud Shanghai Alicloud - Mobile (China)", "cn")]
        [InlineData("Alibaba Cloud Shanghai Alicloud - Telecom (China)", "cn")]
        [InlineData("Alibaba Cloud Shanghai Alicloud - Unicom (China)", "cn")]
        [InlineData("Amsterdam (Netherlands)", "nl")]
        [InlineData("Atlanta (Georgia)", "us")]
        [InlineData("Buenos Aires (Argentina)", "ar")]
        [InlineData("Chennai - Ambattur (India)", "in")]
        [InlineData("Chicago (Illinois)", "us")]
        [InlineData("China", "cn")]
        [InlineData("Dallas (Texas)", "us")]
        [InlineData("Datapacket Frankfurt (Germany)", "de")]
        [InlineData("Datapacket Fujairah (UAE)", "ae")]
        [InlineData("Datapacket Vienna (Austria)", "at")]
        [InlineData("Dubai (United Arab Emirates)", "ae")]
        [InlineData("Frankfurt (Germany)", "de")]
        [InlineData("Guam", "gu")]
        [InlineData("Helsinki (Finland)", "fi")]
        [InlineData("Hong Kong", "hk")]
        [InlineData("India", "in")]
        [InlineData("Japan", "jp")]
        [InlineData("Johannesburg (South Africa)", "za")]
        [InlineData("Lima (Peru)", "pe")]
        [InlineData("London (England)", "gb")]
        [InlineData("Los Angeles (California)", "us")]
        [InlineData("Madrid (Spain)", "es")]
        [InlineData("Multiplay Amsterdam (Netherlands)", "nl")]
        [InlineData("Multiplay Dallas (Texas)", "us")]
        [InlineData("Multiplay Hong Kong", "hk")]
        [InlineData("Mumbai (India)", "in")]
        [InlineData("New York", "us")]
        [InlineData("Paris (France)", "fr")]
        [InlineData("Perfect World (sha-4) Backbone (Shanghai, China)", "cn")]
        [InlineData("Perfect World Guangdong 1 (China)", "cn")]
        [InlineData("Perfect World Guangdong 2 - Mobile (China)", "cn")]
        [InlineData("Perfect World Guangdong 2 - Telecom (China)", "cn")]
        [InlineData("Perfect World Guangdong 2 - Unicom (China)", "cn")]
        [InlineData("Perfect World Hebei (China)", "cn")]
        [InlineData("Perfect World Shanghai - Mobile (China)", "cn")]
        [InlineData("Perfect World Shanghai - Telecom (China)", "cn")]
        [InlineData("Perfect World Shanghai - Unicom (China)", "cn")]
        [InlineData("Perfect World Tianjin (China)", "cn")]
        [InlineData("Perfect World Tianjin - Mobile (China)", "cn")]
        [InlineData("Perfect World Tianjin - Telecom (China)", "cn")]
        [InlineData("Perfect World Tianjin - Unicom (China)", "cn")]
        [InlineData("Perfect World Wuhan (China)", "cn")]
        [InlineData("Perfect World Zhejiang (China)", "cn")]
        [InlineData("Santiago (Chile)", "cl")]
        [InlineData("Sao Paulo (Brazil)", "br")]
        [InlineData("Seattle (Washington)", "us")]
        [InlineData("Seoul (South Korea)", "kr")]
        [InlineData("Singapore", "sg")]
        [InlineData("Sterling (Virginia)", "us")]
        [InlineData("Stockholm (Sweden)", "se")]
        [InlineData("Stockholm - Bromma (Sweden)", "se")]
        [InlineData("Stockholm - Kista (Sweden)", "se")]
        [InlineData("Sydney (Australia)", "au")]
        [InlineData("Tencent Guangzhou - Mobile (China)", "cn")]
        [InlineData("Tencent Guangzhou - Telecom (China)", "cn")]
        [InlineData("Tencent Guangzhou - Unicom (China)", "cn")]
        [InlineData("Tokyo Koto City (Japan)", "jp")]
        [InlineData("Vienna (Austria)", "at")]
        [InlineData("Warsaw (Poland)", "pl")]
        public void KnownServers_GetTheirCountryFlag(string serverDescription, string expectedCountryCode)
        {
            Assert.Equal(expectedCountryCode, FlagResolver.GetCountryCode(serverDescription));
            Assert.Equal($"/Assets/flags/{expectedCountryCode}.png", FlagResolver.GetFlagPath(serverDescription));
        }

        // Servers Valve may add later, in countries that already have a flag
        [Theory]
        [InlineData("Datapacket Madrid (Spain)", "es")]
        [InlineData("Ashburn (Virginia)", "us")]
        [InlineData("Charleston (West Virginia)", "us")]
        [InlineData("Indianapolis (Indiana)", "us")]
        [InlineData("Busan (Korea)", "kr")]
        [InlineData("Manchester (United Kingdom)", "gb")]
        [InlineData("Osaka, Japan", "jp")]
        public void NewServers_InKnownCountries_GetAFlag(string serverDescription, string expectedCountryCode)
        {
            Assert.Equal(expectedCountryCode, FlagResolver.GetCountryCode(serverDescription));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("Toronto (Canada)")]
        [InlineData("Ukrainian relay")]
        public void UnknownPlaces_GetNoFlag(string? serverDescription)
        {
            Assert.Null(FlagResolver.GetCountryCode(serverDescription));
            Assert.Equal(string.Empty, FlagResolver.GetFlagPath(serverDescription));
        }

        [Fact]
        public void EveryCountryCode_HasAnImage_AndNoImageIsUnused()
        {
            string flagFolder = TestPaths.FromRepositoryRoot("ServerPickerX", "Assets", "flags");

            HashSet<string> images = Directory.GetFiles(flagFolder, "*.png")
                .Select(path => Path.GetFileNameWithoutExtension(path))
                .ToHashSet(StringComparer.Ordinal);

            Assert.Empty(FlagResolver.KnownCountryCodes.Where(code => !images.Contains(code)));
            Assert.Empty(images.Where(image => !FlagResolver.KnownCountryCodes.Contains(image)));
        }
    }
}

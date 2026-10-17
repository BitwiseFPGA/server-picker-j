using ServerPickerX.Models;

namespace ServerPickerX.Tests.Models
{
    public class PresetModelTests
    {
        private static PresetModel EuOnlyPreset() => new()
        {
            Name = "EU only",
            GameMode = "Deadlock",
            BlockedServerKeys = ["sgp", "hkg"],
            KnownServerKeys = ["fra", "ams", "sgp", "hkg"],
        };

        [Theory]
        [InlineData("sgp", true)]
        [InlineData("HKG", true)]
        [InlineData("fra", false)]
        [InlineData("ams", false)]
        public void KnownServers_FollowTheBlockedList(string serverKey, bool expected)
        {
            Assert.Equal(expected, EuOnlyPreset().BlocksServer(serverKey));
        }

        // The bug this guards against: a server Valve adds later, such as the UAE one,
        // must not slip into an existing preset as allowed
        [Fact]
        public void ServersAddedAfterThePresetWasSaved_AreBlocked()
        {
            Assert.True(EuOnlyPreset().BlocksServer("dfjr"));
        }

        // Presets saved by older versions behave as they always did until they learn the server list
        [Fact]
        public void OlderPresets_OnlyBlockTheirBlockedList()
        {
            PresetModel preset = new() { Name = "Old", GameMode = "Deadlock", BlockedServerKeys = ["sgp"] };

            Assert.True(preset.BlocksServer("sgp"));
            Assert.False(preset.BlocksServer("dfjr"));
        }

        // A preset that somehow knows no servers must not block everything
        [Fact]
        public void AnEmptyServerList_CountsAsNotLearnedYet()
        {
            PresetModel preset = new() { Name = "Old", GameMode = "Deadlock", BlockedServerKeys = ["sgp"], KnownServerKeys = [] };

            Assert.True(preset.BlocksServer("sgp"));
            Assert.False(preset.BlocksServer("fra"));
        }
    }
}

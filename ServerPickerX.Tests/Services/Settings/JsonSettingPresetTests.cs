using Moq;
using ServerPickerX.Models;
using ServerPickerX.Services.Loggers;
using ServerPickerX.Services.MessageBoxes;
using ServerPickerX.Settings;

namespace ServerPickerX.Tests.Services.Settings
{
    public class JsonSettingPresetTests
    {
        private static JsonSetting NewSetting(params PresetModel[] presets) =>
            new(new Mock<IMessageBoxService>().Object, new Mock<ILoggerService>().Object)
            {
                server_presets = [.. presets],
            };

        [Fact]
        public async Task OlderPresets_LearnTheCurrentServerList_OnlyOnce_AndOnlyForTheirGame()
        {
            JsonSetting setting = NewSetting(
                new PresetModel { Name = "Old", GameMode = "Deadlock", BlockedServerKeys = ["sgp"] },
                new PresetModel { Name = "Old clustered", GameMode = "Deadlock", IsClustered = true, BlockedServerKeys = [] },
                new PresetModel { Name = "Current", GameMode = "Deadlock", BlockedServerKeys = [], KnownServerKeys = ["fra"] },
                new PresetModel { Name = "Other game", GameMode = "Counter Strike 2", BlockedServerKeys = [] }
                );

            bool changed = await setting.InitializeKnownServerKeysByGameModeAsync(
                "Deadlock",
                ["China", "Frankfurt (Germany)"],
                ["fra", "sgp"]
                );

            Assert.True(changed);
            Assert.Equal(new[] { "fra", "sgp" }, setting.server_presets[0].KnownServerKeys!);
            Assert.Equal(new[] { "China", "Frankfurt (Germany)" }, setting.server_presets[1].KnownServerKeys!);
            Assert.Equal(new[] { "fra" }, setting.server_presets[2].KnownServerKeys!);
            Assert.Null(setting.server_presets[3].KnownServerKeys);

            // What the older preset allowed stays allowed, its blocked servers stay blocked
            Assert.True(setting.server_presets[0].BlocksServer("sgp"));
            Assert.False(setting.server_presets[0].BlocksServer("fra"));
        }

        // Learning an empty list would make the preset block every server
        [Fact]
        public async Task OlderPresets_DoNotLearnAnEmptyServerList()
        {
            JsonSetting setting = NewSetting(new PresetModel { Name = "Old", GameMode = "Deadlock", BlockedServerKeys = ["sgp"] });

            bool changed = await setting.InitializeKnownServerKeysByGameModeAsync("Deadlock", [], [" "]);

            Assert.False(changed);
            Assert.Null(setting.server_presets[0].KnownServerKeys);
        }

        [Fact]
        public async Task SavingAPreset_KeepsItsKnownServers()
        {
            JsonSetting setting = NewSetting();

            await setting.AddOrUpdatePresetAsync(new PresetModel
            {
                Name = "EU only",
                GameMode = "Deadlock",
                BlockedServerKeys = ["sgp"],
                KnownServerKeys = ["sgp", "fra", "fra"],
            });

            Assert.Equal(new[] { "fra", "sgp" }, setting.server_presets[0].KnownServerKeys!);
        }
    }
}

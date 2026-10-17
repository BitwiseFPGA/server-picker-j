using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ServerPickerX.Models
{
    public class PresetModel : ObservableObject
    {
        public string Name { get; set; } = string.Empty;

        public string GameMode { get; set; } = string.Empty;

        public bool IsClustered { get; set; }

        public List<string> BlockedServerKeys { get; set; } = [];

        // Every server the preset knew about when it was last saved. A server Valve adds later
        // is missing here, so the preset treats it as blocked instead of letting it slip in as
        // allowed. Null for presets saved by older versions until the app fills it in once
        public List<string>? KnownServerKeys { get; set; }

        // Blocked on purpose, or newer than the preset
        public bool BlocksServer(string serverKey)
        {
            if ((BlockedServerKeys ?? []).Contains(serverKey, StringComparer.OrdinalIgnoreCase))
            {
                return true;
            }

            // An empty list counts as not learned yet, it must never make a preset block everything
            return KnownServerKeys is { Count: > 0 } knownServerKeys
                && !knownServerKeys.Contains(serverKey, StringComparer.OrdinalIgnoreCase);
        }

        public override bool Equals(object? obj)
        {
            return obj is PresetModel other &&
                GameMode.Equals(other.GameMode, StringComparison.OrdinalIgnoreCase) &&
                Name.Equals(other.Name, StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                StringComparer.OrdinalIgnoreCase.GetHashCode(GameMode),
                StringComparer.OrdinalIgnoreCase.GetHashCode(Name)
            );
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dalamud.Configuration;
using Dalamud.Plugin;
using Dalamud.Utility;

namespace XIVComboPlugin
{
    [Serializable]
    public class XIVComboConfiguration : IPluginConfiguration
    {

        public CustomComboPreset[] ComboPresets { get; set; } = new CustomComboPreset[Enum.GetValues(typeof(CustomComboPreset)).Cast<CustomComboPreset>().Where(x => x != CustomComboPreset.None).Max(x => x.GetAttribute<CustomComboInfoAttribute>().Left) + 1];
        public int Version { get; set; }

        public List<bool> HiddenActions;

    }
    public static class CustomComboPresetExtension
    {
        public static bool HasFlag(this CustomComboPreset[] presets, CustomComboPreset preset)
        {
            var left = preset.GetAttribute<CustomComboInfoAttribute>().Left;
            if (presets.Length < left || presets[left] == default) return false;
            return presets[left].HasFlag(preset);
        }
    }
}

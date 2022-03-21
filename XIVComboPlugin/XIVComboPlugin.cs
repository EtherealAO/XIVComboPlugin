using Dalamud.Game.Command;
using Dalamud.Plugin;
using System;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.JobGauge;
using Dalamud.Game.Gui;
using Dalamud.IoC;
using Dalamud.Utility;
using Dalamud.Data;

namespace XIVComboPlugin
{
    class XIVComboPlugin : IDalamudPlugin
    {
        public string Name => "XIV Combo Plugin";

        public XIVComboConfiguration Configuration;

        private IconReplacer iconReplacer;
        private CustomComboPreset[] orderedByClassJob;

        [PluginService] public static CommandManager CommandManager { get; private set; } = null!;
        [PluginService] public static DalamudPluginInterface PluginInterface { get; private set; } = null!;
        [PluginService] public static SigScanner TargetModuleScanner { get; private set; } = null!;
        [PluginService] public static ClientState ClientState { get; private set; } = null!;
        [PluginService] public static ChatGui ChatGui { get; private set; } = null!;
        [PluginService] public static JobGauges JobGauges { get; private set; } = null!;

        public XIVComboPlugin(DataManager manager)
        {

            CommandManager.AddHandler("/pcombo", new CommandInfo(OnCommandDebugCombo)
            {
                HelpMessage = "Open a window to edit custom combo settings.",
                ShowInHelp = true
            });

            this.Configuration = PluginInterface.GetPluginConfig() as XIVComboConfiguration ?? new XIVComboConfiguration();
            if (Configuration.Version < 4)
            {
                Configuration.Version = 4;
            }

            this.iconReplacer = new IconReplacer(TargetModuleScanner, ClientState, manager, this.Configuration);

            this.iconReplacer.Enable();

            PluginInterface.UiBuilder.OpenConfigUi += () => isImguiComboSetupOpen = true;
            PluginInterface.UiBuilder.Draw += UiBuilder_OnBuildUi;

            var values = Enum.GetValues(typeof(CustomComboPreset)).Cast<CustomComboPreset>();
            orderedByClassJob = values.Where(x => x != CustomComboPreset.None && x.GetAttribute<CustomComboInfoAttribute>() != null).OrderBy(x => x.GetAttribute<CustomComboInfoAttribute>().ClassJob).ToArray();
            UpdateConfig();
        }

        private bool isImguiComboSetupOpen = false;

        private string ClassJobToName(byte key)
        {
            return key switch
            {
                1 => "Gladiator",
                2 => "Pugilist",
                3 => "Marauder",
                4 => "Lancer",
                5 => "Archer",
                6 => "Conjurer",
                7 => "Thaumaturge",
                8 => "Carpenter",
                9 => "Blacksmith",
                10 => "Armorer",
                11 => "Goldsmith",
                12 => "Leatherworker",
                13 => "Weaver",
                14 => "Alchemist",
                15 => "Culinarian",
                16 => "Miner",
                17 => "Botanist",
                18 => "Fisher",
                19 => "Paladin",
                20 => "Monk",
                21 => "Warrior",
                22 => "Dragoon",
                23 => "Bard",
                24 => "White Mage",
                25 => "Black Mage",
                26 => "Arcanist",
                27 => "Summoner",
                28 => "Scholar",
                29 => "Rogue",
                30 => "Ninja",
                31 => "Machinist",
                32 => "Dark Knight",
                33 => "Astrologian",
                34 => "Samurai",
                35 => "Red Mage",
                36 => "Blue Mage",
                37 => "Gunbreaker",
                38 => "Dancer",
                39 => "Reaper",
                40 => "Sage",
                _ => "Unknown",
            };
        }

        private void UpdateConfig()
        {

        }

        private void UiBuilder_OnBuildUi()
        {

            if (!isImguiComboSetupOpen)
                return;
            var flagsSelected = new bool[orderedByClassJob.Length];
            for (var i = 0; i < orderedByClassJob.Length; i++)
            {
                flagsSelected[i] = Configuration.ComboPresets.HasFlag(orderedByClassJob[i]);
            }

            ImGui.SetNextWindowSize(new Vector2(740, 490));

            ImGui.Begin("Custom Combo Setup", ref isImguiComboSetupOpen, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar);

            ImGui.Text("This window allows you to enable and disable custom combos to your liking.");
            ImGui.Separator();

            ImGui.BeginChild("scrolling", new Vector2(0, 400), true, ImGuiWindowFlags.HorizontalScrollbar);

            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0, 5));

            var lastClassJob = 0;

            for (var i = 0; i < orderedByClassJob.Length; i++)
            {
                var flag = orderedByClassJob[i];
                var flagInfo = flag.GetAttribute<CustomComboInfoAttribute>();
                if (lastClassJob != flagInfo.ClassJob)
                {
                    lastClassJob = flagInfo.ClassJob;
                    if (ImGui.CollapsingHeader(ClassJobToName((byte)lastClassJob)))
                    {
                        for (int j = i; j < orderedByClassJob.Length; j++)
                        {
                            flag = orderedByClassJob[j];
                            flagInfo = flag.GetAttribute<CustomComboInfoAttribute>();
                            if (lastClassJob != flagInfo.ClassJob)
                            {
                                break;
                            }
                            ImGui.PushItemWidth(200);
                            ImGui.Checkbox(flagInfo.FancyName, ref flagsSelected[j]);
                            ImGui.PopItemWidth();
                            ImGui.TextColored(new Vector4(0.68f, 0.68f, 0.68f, 1.0f), $"#{j + 1}:" + flagInfo.Description);
                            ImGui.Spacing();
                        }

                    }

                }
            }

            for (var i = 0; i < orderedByClassJob.Length; i++)
            {
                var left = orderedByClassJob[i].GetAttribute<CustomComboInfoAttribute>().Left;
                if (flagsSelected[i])
                {
                    Configuration.ComboPresets[left] |= orderedByClassJob[i];
                    Dalamud.Logging.PluginLog.Information("enabled {0}", orderedByClassJob[i].ToString());
                }
                else
                {
                    Configuration.ComboPresets[left] &= ~orderedByClassJob[i];
                    if (orderedByClassJob[i] == CustomComboPreset.WarriorBloodwhettingCombo)
                        Dalamud.Logging.PluginLog.Information("disabled {0}", orderedByClassJob[i].ToString());
                }
            }

            ImGui.PopStyleVar();

            ImGui.EndChild();

            ImGui.Separator();
            if (ImGui.Button("Save"))
            {
                PluginInterface.SavePluginConfig(Configuration);
                UpdateConfig();
            }
            ImGui.SameLine();
            if (ImGui.Button("Save and Close"))
            {
                PluginInterface.SavePluginConfig(Configuration);
                this.isImguiComboSetupOpen = false;
                UpdateConfig();
            }

            ImGui.End();
        }

        public void Dispose()
        {
            this.iconReplacer.Dispose();

            CommandManager.RemoveHandler("/pcombo");
        }

        private void OnCommandDebugCombo(string command, string arguments)
        {
            isImguiComboSetupOpen = true;
            PluginInterface.SavePluginConfig(Configuration);
        }
    }
}

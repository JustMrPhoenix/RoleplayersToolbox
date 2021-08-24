using System;
using System.Collections.Generic;
using System.Reflection;
using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using Lumina;
using RoleplayersToolbox.Tools;
using RoleplayersToolbox.Tools.Housing;
using RoleplayersToolbox.Tools.Targeting;
using XivCommon;
#if ILLEGAL
using RoleplayersToolbox.Tools.Illegal.Emote;
using RoleplayersToolbox.Tools.Illegal.EmoteSnap;

#endif

namespace RoleplayersToolbox {
    internal class Plugin : IDalamudPlugin {
        public string Name => "The Roleplayer's Toolbox";

        [PluginService]
        internal DalamudPluginInterface Interface { get; init; } = null!;

        [PluginService]
        internal ChatGui ChatGui { get; init; } = null!;

        [PluginService]
        internal ClientState ClientState { get; init; } = null!;

        [PluginService]
        internal CommandManager CommandManager { get; init; } = null!;

        [PluginService]
        internal DataManager DataManager { get; init; } = null!;

        [PluginService]
        internal Framework Framework { get; init; } = null!;

        [PluginService]
        internal GameGui GameGui { get; init; } = null!;

        [PluginService]
        internal ObjectTable ObjectTable { get; init; } = null!;

        [PluginService]
        internal SeStringManager SeStringManager { get; init; } = null!;

        [PluginService]
        internal SigScanner SigScanner { get; init; } = null!;

        internal GameData? GameData { get; }
        internal Configuration Config { get; }
        internal XivCommonBase Common { get; }
        internal List<ITool> Tools { get; } = new();
        internal PluginUi Ui { get; }
        private Commands Commands { get; }

        public Plugin() {
            this.GameData = (GameData?) this.DataManager
                .GetType()
                .GetField("gameData", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.GetValue(this.DataManager);
            this.Config = this.Interface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Common = new XivCommonBase(Hooks.ContextMenu | Hooks.PartyFinderListings);

            this.Tools.Add(new HousingTool(this));
            this.Tools.Add(new TargetingTool(this));

            #if ILLEGAL
            this.Tools.Add(new EmoteTool(this));
            this.Tools.Add(new EmoteSnapTool(this));
            #endif

            this.Ui = new PluginUi(this);

            this.Commands = new Commands(this);

            if (this.GameData == null) {
                PluginLog.LogWarning("Could not find GameData - some features will be disabled");
            }
        }

        public void Dispose() {
            this.Commands.Dispose();
            this.Ui.Dispose();

            foreach (var tool in this.Tools) {
                if (tool is IDisposable disposable) {
                    disposable.Dispose();
                }
            }

            this.Tools.Clear();

            this.Common.Dispose();
        }

        internal void SaveConfig() {
            this.Interface.SavePluginConfig(this.Config);
        }
    }
}

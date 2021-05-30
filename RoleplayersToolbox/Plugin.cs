using System;
using System.Collections.Generic;
using System.Reflection;
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
    internal class Plugin : IDisposable {
        internal DalamudPluginInterface Interface { get; }
        internal GameData? GameData { get; }
        internal Configuration Config { get; }
        internal XivCommonBase Common { get; }
        internal List<ITool> Tools { get; } = new();
        internal PluginUi Ui { get; }
        private Commands Commands { get; }

        public Plugin(DalamudPluginInterface pluginInterface) {
            this.Interface = pluginInterface;
            this.GameData = (GameData?) this.Interface.Data
                .GetType()
                .GetField("gameData", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.GetValue(this.Interface.Data);
            this.Config = this.Interface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Common = new XivCommonBase(pluginInterface, Hooks.ContextMenu | Hooks.PartyFinderListings);

            this.Ui = new PluginUi(this);

            this.Tools.Add(new HousingTool(this));
            this.Tools.Add(new TargetingTool(this));

            #if ILLEGAL
            this.Tools.Add(new EmoteTool(this));
            this.Tools.Add(new EmoteSnapTool(this));
            #endif

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

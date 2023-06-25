using System;
using System.Collections.Generic;
using Dalamud.ContextMenu;
using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.IoC;
using Dalamud.Plugin;
using RoleplayersToolbox.Tools;
using RoleplayersToolbox.Tools.Housing;
using RoleplayersToolbox.Tools.Targeting;
using XivCommon;
using Dalamud.Game.Gui.Dtr;
#if ILLEGAL
using RoleplayersToolbox.Tools.Illegal.Emote;
using RoleplayersToolbox.Tools.Illegal.EmoteSnap;

#endif

namespace RoleplayersToolbox {
    internal class Plugin : IDalamudPlugin {
        #if DEBUG
        public string Name => "The Roleplayer's Toolbox (Debug)";
        #else
        public string Name => "The Roleplayer's Toolbox";
        #endif

        public DalamudPluginInterface PluginInterface { get; private set; } = null!;

        internal ChatGui ChatGui { get; init; } = null!;

        internal ClientState ClientState { get; init; } = null!;

        internal CommandManager CommandManager { get; init; } = null!;

        // [PluginService]
        // internal ContextMenu ContextMenu { get; init; } = null!;

        internal DalamudContextMenu ContextMenu { get; }

        internal DataManager DataManager { get; init; } = null!;

        internal Framework Framework { get; init; } = null!;

        internal GameGui GameGui { get; init; } = null!;

        internal ObjectTable ObjectTable { get; init; } = null!;

        internal SigScanner SigScanner { get; init; } = null!;

        internal Configuration Config { get; }
        internal XivCommonBase Common { get; }
        internal List<ITool> Tools { get; } = new();
        internal PluginUi Ui { get; }
        private Commands Commands { get; }

        public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] ChatGui chatGui,
            [RequiredVersion("1.0")] CommandManager commands,
            [RequiredVersion("1.0")] ClientState clientState,
            [RequiredVersion("1.0")] DataManager dataManager,
            [RequiredVersion("1.0")] SigScanner sigScanner,
            [RequiredVersion("1.0")] Framework framework,
            [RequiredVersion("1.0")] GameGui gameGui,
            [RequiredVersion("1.0")] ObjectTable objectTable)
        {
            this.PluginInterface = pluginInterface;
            this.ChatGui = chatGui;
            this.CommandManager = commands;
            this.ClientState = clientState;
            this.DataManager = dataManager;
            this.SigScanner = sigScanner;
            this.Framework = framework;
            this.GameGui = gameGui;
            this.ObjectTable = objectTable;

            Dalamud.Logging.PluginLog.Information("sas");
            Dalamud.Logging.PluginLog.Information("this.Interface - " + this.PluginInterface.GetHashCode());
            this.Config = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Common = new XivCommonBase(Hooks.PartyFinderListings);

            this.ContextMenu = new DalamudContextMenu();

            this.Tools.Add(new HousingTool(this));
            this.Tools.Add(new TargetingTool(this));

            #if ILLEGAL
            this.Tools.Add(new EmoteTool(this));
            this.Tools.Add(new EmoteSnapTool(this));
            #endif

            this.Ui = new PluginUi(this);

            this.Commands = new Commands(this);
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
            this.PluginInterface .SavePluginConfig(this.Config);
        }
    }
}

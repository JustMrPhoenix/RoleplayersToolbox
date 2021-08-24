using System;
using Dalamud.Game.Command;

namespace RoleplayersToolbox {
    internal class Commands : IDisposable {
        private Plugin Plugin { get; }

        internal Commands(Plugin plugin) {
            this.Plugin = plugin;

            this.Plugin.CommandManager.AddHandler("/rptools", new CommandInfo(this.OnCommand) {
                HelpMessage = "Open The Roleplayer's Toolbox",
            });
        }

        public void Dispose() {
            this.Plugin.CommandManager.RemoveHandler("/rptools");
        }

        private void OnCommand(string command, string arguments) {
            this.Plugin.Ui.ShowInterface ^= true;
        }
    }
}

#if ILLEGAL

using System;
using Dalamud.Game.Command;
using Dalamud.Hooking;
using ImGuiNET;

namespace RoleplayersToolbox.Tools.Illegal.EmoteSnap {
    internal class EmoteSnapTool : BaseTool, IDisposable {
        private static class Signatures {
            internal const string ShouldSnap = "E8 ?? ?? ?? ?? 84 C0 74 55 40 84 FF";// "E8 ?? ?? ?? ?? 84 C0 74 46 4C 8D 6D C7";
        }

        private delegate byte ShouldSnapDelegate(IntPtr a1, IntPtr a2);

        public override string Name => "Emote Snap";

        private Plugin Plugin { get; }
        private EmoteSnapConfig Config { get; }
        private Hook<ShouldSnapDelegate>? ShouldSnapHook { get; }

        internal EmoteSnapTool(Plugin plugin) {
            this.Plugin = plugin;
            this.Config = this.Plugin.Config.Tools.EmoteSnap;

            if (this.Plugin.SigScanner.TryScanText(Signatures.ShouldSnap, out var snapPtr)) {
                this.ShouldSnapHook = new Hook<ShouldSnapDelegate>(snapPtr, this.ShouldSnapDetour);
                this.ShouldSnapHook.Enable();
            }

            this.Plugin.CommandManager.AddHandler("/dozesnap", new CommandInfo(this.OnCommand) {
                HelpMessage = "Toggle snapping for the /doze emote",
            });
        }

        public void Dispose() {
            this.Plugin.CommandManager.RemoveHandler("/dozesnap");
            this.ShouldSnapHook?.Dispose();
        }

        public override void DrawSettings(ref bool anyChanged) {
            anyChanged |= ImGui.Checkbox("Disable /doze snap", ref this.Config.DisableDozeSnap);

            ImGui.TextUnformatted("Check this box to prevent /doze and the sleep emote from snapping. In order to use the sleep emote, you need to have it on your bar.");
            ImGui.TextUnformatted("The /dozesnap command can be used to toggle this.");
            ImGui.TextColored(new System.Numerics.Vector4(255,0,0,255), "WARNING! You will NOT be able to move if you turn this on. This is a temporary fix while I look for a better snap solution");
        }

        private byte ShouldSnapDetour(IntPtr a1, IntPtr a2) {
            return this.Config.DisableDozeSnap
                ? (byte) 0
                : this.ShouldSnapHook!.Original(a1, a2);
        }

        private void OnCommand(string command, string arguments) {
            this.Config.DisableDozeSnap ^= true;

            var status = this.Config.DisableDozeSnap ? "off" : "on";
            this.Plugin.ChatGui.Print($"/doze snap toggled {status}.");
        }
    }
}

#endif

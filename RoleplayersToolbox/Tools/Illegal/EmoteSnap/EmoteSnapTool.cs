#if ILLEGAL

using System;
using Dalamud.Hooking;
using ImGuiNET;

namespace RoleplayersToolbox.Tools.Illegal.EmoteSnap {
    internal class EmoteSnapTool : BaseTool, IDisposable {
        private static class Signatures {
            internal const string ShouldSnap = "E8 ?? ?? ?? ?? 84 C0 74 46 4C 8D 6D C7";
        }

        private delegate byte ShouldSnapDelegate(IntPtr a1, IntPtr a2);

        public override string Name => "Emote Snap";

        private Plugin Plugin { get; }
        private EmoteSnapConfig Config { get; }
        private Hook<ShouldSnapDelegate>? ShouldSnapHook { get; }

        internal EmoteSnapTool(Plugin plugin) {
            this.Plugin = plugin;
            this.Config = this.Plugin.Config.Tools.EmoteSnap;

            if (this.Plugin.Interface.TargetModuleScanner.TryScanText(Signatures.ShouldSnap, out var snapPtr)) {
                this.ShouldSnapHook = new Hook<ShouldSnapDelegate>(snapPtr, new ShouldSnapDelegate(this.ShouldSnapDetour));
                this.ShouldSnapHook.Enable();
            }
        }

        public void Dispose() {
            this.ShouldSnapHook?.Dispose();
        }

        public override void DrawSettings(ref bool anyChanged) {
            anyChanged |= ImGui.Checkbox("Disable /doze snap", ref this.Config.DisableDozeSnap);

            ImGui.TextUnformatted("Check this box to prevent /doze and the sleep emote from snapping. In order to use the sleep emote, you need to have it on your bar.");
        }

        private byte ShouldSnapDetour(IntPtr a1, IntPtr a2) {
            return this.Config.DisableDozeSnap
                ? (byte) 0
                : this.ShouldSnapHook!.Original(a1, a2);
        }
    }
}

#endif

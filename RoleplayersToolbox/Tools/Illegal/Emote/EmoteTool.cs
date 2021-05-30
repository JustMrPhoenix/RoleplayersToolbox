#if ILLEGAL

using System;
using Dalamud.Hooking;
using ImGuiNET;

namespace RoleplayersToolbox.Tools.Illegal.Emote {
    internal class EmoteTool : BaseTool, IDisposable {
        private static class Signatures {
            internal const string SetActionOnHotbar = "E8 ?? ?? ?? ?? 4C 39 6F 08";
        }

        private delegate IntPtr SetActionOnHotbarDelegate(IntPtr a1, IntPtr a2, byte actionType, uint actionId);

        public override string Name => "Emotes";
        private Plugin Plugin { get; }
        private Hook<SetActionOnHotbarDelegate>? SetActionOnHotbarHook { get; }
        private bool Custom { get; set; }
        private Emote? Emote { get; set; }

        internal EmoteTool(Plugin plugin) {
            this.Plugin = plugin;

            if (this.Plugin.Interface.TargetModuleScanner.TryScanText(Signatures.SetActionOnHotbar, out var setPtr)) {
                this.SetActionOnHotbarHook = new Hook<SetActionOnHotbarDelegate>(setPtr, new SetActionOnHotbarDelegate(this.SetActionOnHotbarDetour));
                this.SetActionOnHotbarHook.Enable();
            }
        }

        public void Dispose() {
            this.SetActionOnHotbarHook?.Dispose();
        }

        public override void DrawSettings(ref bool anyChanged) {
            if (this.SetActionOnHotbarHook == null) {
                ImGui.TextUnformatted("An update broke this tool. Please let Anna know.");
                return;
            }

            ImGui.TextUnformatted("Click one of the options below, then drag anything onto your hotbar. Instead of what you dragged, your hotbar will have that emote instead.");

            foreach (var emote in (Emote[]) Enum.GetValues(typeof(Emote))) {
                if (ImGui.RadioButton(emote.Name(), !this.Custom && this.Emote == emote)) {
                    this.Custom = false;
                    this.Emote = emote;
                }
            }

            if (ImGui.RadioButton("Custom", this.Custom)) {
                this.Custom = true;
                this.Emote = null;
            }

            if (this.Custom) {
                var id = (int) (this.Emote ?? 0);
                if (ImGui.InputInt("###custom-emote", ref id)) {
                    this.Emote = (Emote?) Math.Max(0, id);
                }
            }

            if (this.Emote != null && ImGui.Button("Cancel")) {
                this.Custom = false;
                this.Emote = null;
            }
        }

        private IntPtr SetActionOnHotbarDetour(IntPtr a1, IntPtr a2, byte actionType, uint actionId) {
            var emote = this.Emote;
            if (emote == null) {
                return this.SetActionOnHotbarHook!.Original(a1, a2, actionType, actionId);
            }

            this.Custom = false;
            this.Emote = null;
            return this.SetActionOnHotbarHook!.Original(a1, a2, 6, (uint) emote);
        }
    }
}

#endif

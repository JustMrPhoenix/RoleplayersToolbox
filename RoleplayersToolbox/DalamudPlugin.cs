using Dalamud.Plugin;

namespace RoleplayersToolbox {
    // ReSharper disable once UnusedType.Global
    public class DalamudPlugin : IDalamudPlugin {
        public string Name => "The Roleplayer's Toolbox";

        private Plugin Plugin { get; set; } = null!;

        public void Initialize(DalamudPluginInterface pluginInterface) {
            this.Plugin = new Plugin(pluginInterface);
        }

        public void Dispose() {
            this.Plugin.Dispose();
        }
    }
}

using System;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using Dalamud.Game.Command;
using Dalamud.Game.Internal;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using XivCommon.Functions.ContextMenu;

namespace RoleplayersToolbox.Tools.Housing {
    internal class HousingTool : BaseTool, IDisposable {
        private static class Signatures {
            internal const string AddonMapHide = "40 53 48 83 EC 30 0F B6 91 ?? ?? ?? ?? 48 8B D9 E8 ?? ?? ?? ??";
            internal const string HousingPointer = "48 8B 05 ?? ?? ?? ?? 48 83 78 ?? ?? 74 16 48 8D 8F ?? ?? ?? ?? 66 89 5C 24 ?? 48 8D 54 24 ?? E8 ?? ?? ?? ?? 48 8B 7C 24";
        }

        // Updated: 5.55
        // 48 89 5C 24 ?? 57 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 84 24 ?? ?? ?? ?? 80 3D ?? ?? ?? ?? ??
        private const int AgentMapId = 38;

        // AgentMap.vf8 has this offset if the sig above doesn't work
        private const int AgentMapFlagSetOffset = 0x5997;

        private delegate IntPtr AddonMapHideDelegate(IntPtr addon);

        public override string Name => "Housing";
        private Plugin Plugin { get; }
        private HousingConfig Config { get; }
        private HousingInfo Info { get; }
        private Teleport Teleport { get; }

        private DestinationInfo? _destination;

        private DestinationInfo? Destination {
            get => this._destination;
            set {
                this._destination = value;

                if (value == null) {
                    this.ClearFlagAndCloseMap();
                } else if (this.Config.PlaceFlagOnSelect) {
                    this.FlagDestinationOnMap();
                }
            }
        }

        // Updated: 5.55
        // 48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 41 56 41 57 48 83 EC 20 49 8B 00
        private unsafe ushort CurrentWard {
            get {
                var objPtr = Util.FollowPointerChain(this._housingPointer, new[] { 0, 8 });
                return (ushort) (*(ushort*) (objPtr + 0x96a2) + 1);
            }
        }

        private readonly AddonMapHideDelegate? _addonMapHide;
        private readonly IntPtr _housingPointer;

        internal HousingTool(Plugin plugin) {
            this.Plugin = plugin;
            this.Config = plugin.Config.Tools.Housing;
            this.Info = new HousingInfo(plugin);
            this.Teleport = new Teleport(plugin);

            if (this.Plugin.Interface.TargetModuleScanner.TryScanText(Signatures.AddonMapHide, out var addonMapHidePtr)) {
                this._addonMapHide = Marshal.GetDelegateForFunctionPointer<AddonMapHideDelegate>(addonMapHidePtr);
            }

            this.Plugin.Interface.TargetModuleScanner.TryGetStaticAddressFromSig(Signatures.HousingPointer, out this._housingPointer);

            this.Plugin.Common.Functions.ContextMenu.OpenContextMenu += this.OnContextMenu;
            this.Plugin.Interface.Framework.OnUpdateEvent += this.OnFramework;
            this.Plugin.Interface.CommandManager.AddHandler("/route", new CommandInfo(this.OnCommand));
        }

        public void Dispose() {
            this.Plugin.Interface.CommandManager.RemoveHandler("/route");
            this.Plugin.Interface.Framework.OnUpdateEvent -= this.OnFramework;
            this.Plugin.Common.Functions.ContextMenu.OpenContextMenu -= this.OnContextMenu;
        }

        public override void DrawSettings(ref bool anyChanged) {
            anyChanged |= ImGui.Checkbox("Place flag and open map after selecting destination", ref this.Config.PlaceFlagOnSelect);
            anyChanged |= ImGui.Checkbox("Clear flag on approach", ref this.Config.ClearFlagOnApproach);
            anyChanged |= ImGui.Checkbox("Close map on approach", ref this.Config.CloseMapOnApproach);

            ImGui.Separator();

            if (ImGui.Button("Open routing window")) {
                this.Destination = new DestinationInfo(this.Info);
            }

            ImGui.TextUnformatted("You can also use the /route command for this.");
            ImGui.TextUnformatted("Ex: /route jen lb w5 p3");
        }

        public override void DrawAlways() {
            if (this.Destination == null) {
                return;
            }

            if (!ImGui.Begin("Housing destination", ImGuiWindowFlags.AlwaysAutoResize)) {
                ImGui.End();
                return;
            }

            ImGui.TextUnformatted("Routing to...");

            var anyChanged = false;

            var world = this.Destination.World;
            if (ImGui.BeginCombo("World", world?.Name?.ToString() ?? string.Empty)) {
                var dataCentre = this.Plugin.Interface.ClientState.LocalPlayer?.HomeWorld?.GameData?.DataCenter?.Row;

                foreach (var availWorld in this.Plugin.Interface.Data.GetExcelSheet<World>()) {
                    if (availWorld.DataCenter.Row != dataCentre || !availWorld.IsPublic) {
                        continue;
                    }

                    if (!ImGui.Selectable(availWorld.Name.ToString())) {
                        continue;
                    }

                    this.Destination.World = availWorld;
                    anyChanged = true;
                }

                ImGui.EndCombo();
            }

            var area = this.Destination.Area;
            if (ImGui.BeginCombo("Housing area", area?.Name() ?? string.Empty)) {
                foreach (var housingArea in (HousingArea[]) Enum.GetValues(typeof(HousingArea))) {
                    if (!ImGui.Selectable(housingArea.Name(), area == housingArea)) {
                        continue;
                    }

                    this.Destination.Area = housingArea;
                    anyChanged = true;
                }

                ImGui.EndCombo();
            }

            var ward = (int) (this.Destination.Ward ?? 0);
            if (ImGui.InputInt("Ward", ref ward)) {
                this.Destination.Ward = (uint) Math.Max(1, Math.Min(60, ward));
                anyChanged = true;
            }

            var plot = (int) (this.Destination.Plot ?? 0);
            if (ImGui.InputInt("Plot", ref plot)) {
                this.Destination.Plot = (uint) Math.Max(1, Math.Min(60, plot));
                anyChanged = true;
            }

            if (ImGui.Button("Clear")) {
                this.Destination = null;
            }

            if (this.Destination?.Area != null) {
                ImGui.SameLine();

                var name = this.Destination.Area.Value.CityState(this.Plugin.Interface.Data).PlaceName.Value.Name;
                if (ImGui.Button($"Teleport to {name}")) {
                    this.Teleport.TeleportToHousingArea(this.Destination.Area.Value);
                }
            }

            if (anyChanged) {
                this.FlagDestinationOnMap();
            }

            ImGui.End();
        }

        private void OnCommand(string command, string arguments) {
            var player = this.Plugin.Interface.ClientState.LocalPlayer;
            if (player == null) {
                return;
            }

            this.Destination = InfoExtractor.Extract(arguments, player.HomeWorld.GameData.DataCenter.Row, this.Plugin.Interface.Data, this.Info);
        }

        private void OnContextMenu(ContextMenuOpenArgs args) {
            if (args.ParentAddonName != "LookingForGroup" || args.ContentIdLower == 0) {
                return;
            }

            args.Items.Add(new NormalContextMenuItem("Select as Destination", this.SelectDestination));
        }

        private void SelectDestination(ContextMenuItemSelectedArgs args) {
            var listing = this.Plugin.Common.Functions.PartyFinder.CurrentListings.Values.FirstOrDefault(listing => listing.ContentIdLower == args.ContentIdLower);
            if (listing == null) {
                return;
            }

            this.ClearFlag();
            this.Destination = InfoExtractor.Extract(listing.Description.TextValue, listing.World.Value.DataCenter.Row, this.Plugin.Interface.Data, this.Info);
        }

        private void OnFramework(Framework framework) {
            this.ClearIfNear();
            this.HighlightSelectString();
            this.HighlightResidentialTeleport();
            this.HighlightWorldTravel();
        }

        private void ClearIfNear() {
            var destination = this.Destination;
            if (destination == null || destination.AnyNull()) {
                return;
            }

            var info = this.Plugin.Interface.Data.GetExcelSheet<HousingMapMarkerInfo>().GetRow((uint) destination.Area!.Value, (uint) destination.Plot! - 1);
            if (info == null) {
                return;
            }

            var player = this.Plugin.Interface.ClientState.LocalPlayer;
            if (player == null) {
                return;
            }

            // ensure on correct world
            if (player.CurrentWorld.GameData != destination.World) {
                return;
            }

            // ensure in correct zone
            if (this.Plugin.Interface.ClientState.TerritoryType != (ushort) destination.Area) {
                return;
            }

            // ensure in correct ward
            if (this.CurrentWard != destination.Ward) {
                return;
            }

            var localPos = player.Position;
            var localPosCorrected = new Vector3(localPos.X, localPos.Z, localPos.Y);
            var distance = Util.DistanceBetween(localPosCorrected, new Vector3(info.X, info.Y, info.Z));

            if (distance >= 15) {
                return;
            }

            this._destination = null;

            if (this.Config.ClearFlagOnApproach) {
                this.ClearFlag();
            }

            if (this.Config.CloseMapOnApproach) {
                this.CloseMap();
            }
        }

        private void ClearFlagAndCloseMap() {
            this.ClearFlag();
            this.CloseMap();
        }

        private unsafe void ClearFlag() {
            var mapAgent = this.Plugin.Common.Functions.GetAgentByInternalId(AgentMapId);
            if (mapAgent != IntPtr.Zero) {
                *(byte*) (mapAgent + AgentMapFlagSetOffset) = 0;
            }
        }

        private void CloseMap() {
            var addon = this.Plugin.Interface.Framework.Gui.GetAddonByName("AreaMap", 1);
            if (addon != null) {
                this._addonMapHide?.Invoke(addon.Address);
            }
        }

        private void FlagDestinationOnMap() {
            if (this.Destination?.Area == null || this.Destination?.Plot == null) {
                return;
            }

            this.FlagHouseOnMap(this.Destination.Area.Value, this.Destination.Plot.Value);
        }

        private void FlagHouseOnMap(HousingArea area, uint plot) {
            var info = this.Plugin.Interface.Data.GetExcelSheet<HousingMapMarkerInfo>().GetRow((uint) area, plot - 1);
            if (info == null) {
                return;
            }

            var map = info.Map.Value;
            var terr = map?.TerritoryType?.Value;

            if (terr == null) {
                return;
            }

            var mapLink = new MapLinkPayload(
                this.Plugin.Interface.Data,
                terr.RowId,
                map!.RowId,
                (int) (info.X * 1_000f),
                (int) (info.Z * 1_000f)
            );

            this.Plugin.Interface.Framework.Gui.OpenMapWithMapLink(mapLink);
        }

        private unsafe void HighlightResidentialTeleport() {
            var addon = this.Plugin.Interface.Framework.Gui.GetAddonByName("HousingSelectBlock", 1);
            if (addon == null) {
                return;
            }

            var shouldSet = false;

            var player = this.Plugin.Interface.ClientState.LocalPlayer;
            if (player != null && this.Destination?.World != null) {
                shouldSet = player.CurrentWorld.GameData == this.Destination.World;
            }

            if (this.Destination?.Area == null) {
                shouldSet = false;
            } else {
                var currentArea = this.Plugin.Interface.ClientState.TerritoryType;
                shouldSet = shouldSet && (currentArea == (ushort) this.Destination.Area || currentArea == this.Destination.Area.Value.CityStateTerritoryType());
            }

            var unit = (AtkUnitBase*) addon.Address;
            var uld = unit->UldManager;
            if (uld.NodeListCount < 1) {
                return;
            }

            var parentNode = uld.NodeList[0];

            var siblingCount = 0;
            var prev = parentNode->ChildNode;
            while ((prev = prev->PrevSiblingNode) != null) {
                siblingCount += 1;
                if (siblingCount == 8) {
                    break;
                }
            }

            var radioContainer = prev;
            var radioButton = radioContainer->ChildNode;
            do {
                var component = (AtkComponentNode*) radioButton;
                var radioUld = component->Component->UldManager;
                if (radioUld.NodeListCount < 4) {
                    return;
                }

                var textNode = (AtkTextNode*) radioUld.NodeList[3];
                var text = Util.ReadSeString((IntPtr) textNode->NodeText.StringPtr, this.Plugin.Interface.SeStringManager);
                HighlightIf(radioButton, shouldSet && text.TextValue == $"{this.Destination?.Ward}");
            } while ((radioButton = radioButton->PrevSiblingNode) != null);
        }

        private unsafe void HighlightSelectString() {
            var addon = this.Plugin.Interface.Framework.Gui.GetAddonByName("SelectString", 1);
            if (addon == null) {
                return;
            }

            var select = (AddonSelectString*) addon.Address;
            var list = select->PopupMenu.List;
            if (list == null) {
                return;
            }

            this.HighlightSelectStringItems(list);
        }

        private bool ShouldHighlight(SeString str) {
            var text = str.TextValue;

            var sameWorld = this.Destination?.World == this.Plugin.Interface.ClientState.LocalPlayer?.CurrentWorld?.GameData;
            if (!sameWorld && this.Destination?.World != null) {
                return text == " Visit Another World Server.";
            }

            // TODO: figure out how to use HousingAethernet.Order with current one missing
            var placeName = this.Destination?.ClosestAethernet?.PlaceName?.Value?.Name?.ToString();
            if (this.CurrentWard == this.Destination?.Ward && placeName != null && text.StartsWith(placeName) && text.Length == placeName.Length + 1) {
                return true;
            }

            // ReSharper disable once InvertIf
            if (this.Destination?.Ward != null && this.Plugin.Interface.ClientState.TerritoryType == this.Destination?.Area?.CityStateTerritoryType()) {
                switch (text) {
                    case " Residential District Aethernet.":
                    case "Go to specified ward. (Review Tabs)":
                        return true;
                }
            }

            return false;
        }

        private unsafe void HighlightSelectStringItems(AtkComponentList* list) {
            for (var i = 0; i < list->ListLength; i++) {
                var item = list->ItemRendererList + i;
                var button = item->AtkComponentListItemRenderer->AtkComponentButton;
                var buttonText = Util.ReadSeString((IntPtr) button.ButtonTextNode->NodeText.StringPtr, this.Plugin.Interface.SeStringManager);

                var component = (AtkComponentBase*) item->AtkComponentListItemRenderer;

                HighlightIf(&component->OwnerNode->AtkResNode, this.ShouldHighlight(buttonText));
            }
        }

        private unsafe void HighlightWorldTravel() {
            var player = this.Plugin.Interface.ClientState.LocalPlayer;
            if (player == null) {
                return;
            }

            var world = this.Destination?.World;

            var addon = this.Plugin.Interface.Framework.Gui.GetAddonByName("WorldTravelSelect", 1);
            if (addon == null) {
                return;
            }

            var unit = (AtkUnitBase*) addon.Address;
            var root = unit->RootNode;
            if (root == null) {
                return;
            }

            var windowComponent = (AtkComponentNode*) root->ChildNode;
            var informationBox = (AtkComponentNode*) windowComponent->AtkResNode.PrevSiblingNode;
            var informationBoxBorder = (AtkNineGridNode*) informationBox->AtkResNode.PrevSiblingNode;
            var worldListComponent = (AtkComponentNode*) informationBoxBorder->AtkResNode.PrevSiblingNode;
            var listChild = worldListComponent->Component->UldManager.RootNode;

            var prev = listChild;
            if (prev == null) {
                return;
            }

            do {
                if ((uint) prev->Type != 1010) {
                    continue;
                }

                var comp = (AtkComponentNode*) prev;
                var res = comp->Component->UldManager.RootNode->PrevSiblingNode->PrevSiblingNode->PrevSiblingNode;
                var text = (AtkTextNode*) res->ChildNode;
                var str = Util.ReadSeString((IntPtr) text->NodeText.StringPtr, this.Plugin.Interface.SeStringManager);
                HighlightIf(&text->AtkResNode, str.TextValue == world?.Name?.ToString());
            } while ((prev = prev->PrevSiblingNode) != null);
        }

        private static unsafe void HighlightIf(AtkResNode* node, bool cond) {
            if (cond) {
                node->MultiplyRed = 0;
                node->MultiplyGreen = 100;
                node->MultiplyBlue = 0;
            } else {
                node->MultiplyRed = 100;
                node->MultiplyGreen = 100;
                node->MultiplyBlue = 100;
            }
        }
    }
}

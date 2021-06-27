using System;
using System.Numerics;
using Dalamud.Interface;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;

namespace RoleplayersToolbox.Tools.Housing {
    internal class BookmarksUi {
        private Plugin Plugin { get; }
        private HousingTool Tool { get; }
        private HousingConfig Config { get; }
        private (Bookmark editing, int index)? _editing;

        internal bool ShouldDraw;

        internal BookmarksUi(Plugin plugin, HousingTool tool, HousingConfig config) {
            this.Plugin = plugin;
            this.Tool = tool;
            this.Config = config;
        }

        internal void Draw() {
            if (!this.ShouldDraw) {
                return;
            }

            this.AddEditWindow();

            if (!ImGui.Begin("Housing Bookmarks", ref this.ShouldDraw)) {
                ImGui.End();
                return;
            }

            if (Util.IconButton(FontAwesomeIcon.Plus)) {
                this._editing = (new Bookmark(string.Empty), -1);
            }

            var toDelete = -1;

            if (ImGui.BeginChild("bookmark-list", new Vector2(-1, -1))) {
                for (var i = 0; i < this.Config.Bookmarks.Count; i++) {
                    var bookmark = this.Config.Bookmarks[i];
                    var hash = bookmark.GetHashCode().ToString();

                    if (ImGui.TreeNode($"{bookmark.Name}##{hash}")) {
                        var worldName = this.Plugin.Interface.Data.GetExcelSheet<World>().GetRow(bookmark.WorldId)?.Name;
                        ImGui.TextUnformatted($"{worldName}/{bookmark.Area.Name()}/W{bookmark.Ward}/P{bookmark.Plot}");

                        if (Util.IconButton(FontAwesomeIcon.MapMarkerAlt, hash)) {
                            this.Tool.FlagHouseOnMap(bookmark.Area, bookmark.Plot);
                        }

                        Util.Tooltip("Show on map");

                        ImGui.SameLine();

                        if (Util.IconButton(FontAwesomeIcon.Route, hash)) {
                            this.Tool.Destination = new DestinationInfo(
                                this.Tool.Info,
                                this.Plugin.Interface.Data.GetExcelSheet<World>().GetRow(bookmark.WorldId),
                                bookmark.Area,
                                bookmark.Ward,
                                bookmark.Plot
                            );
                        }

                        Util.Tooltip("Open routing window");

                        ImGui.SameLine();

                        if (Util.IconButton(FontAwesomeIcon.PencilAlt, hash)) {
                            this._editing = (bookmark.Clone(), i);
                        }

                        Util.Tooltip("Edit");

                        ImGui.SameLine();

                        if (Util.IconButton(FontAwesomeIcon.Trash, hash)) {
                            toDelete = i;
                        }

                        Util.Tooltip("Delete");

                        ImGui.TreePop();
                    }

                    ImGui.Separator();
                }

                ImGui.EndChild();
            }

            if (toDelete > -1) {
                this.Config.Bookmarks.RemoveAt(toDelete);
                this.Plugin.SaveConfig();
            }

            ImGui.End();
        }

        private void AddEditWindow() {
            if (this._editing == null) {
                return;
            }

            if (!ImGui.Begin("Edit bookmark", ImGuiWindowFlags.AlwaysAutoResize)) {
                ImGui.End();
                return;
            }

            var (bookmark, index) = this._editing.Value;

            ImGui.InputText("Name", ref bookmark.Name, 255);

            var world = bookmark.WorldId == 0
                ? null
                : this.Plugin.Interface.Data.GetExcelSheet<World>().GetRow(bookmark.WorldId);
            if (ImGui.BeginCombo("World", world?.Name?.ToString() ?? string.Empty)) {
                var dataCentre = this.Plugin.Interface.ClientState.LocalPlayer?.HomeWorld?.GameData?.DataCenter?.Row;

                foreach (var availWorld in this.Plugin.Interface.Data.GetExcelSheet<World>()) {
                    if (availWorld.DataCenter.Row != dataCentre || !availWorld.IsPublic) {
                        continue;
                    }

                    if (!ImGui.Selectable(availWorld.Name.ToString())) {
                        continue;
                    }

                    bookmark.WorldId = availWorld.RowId;
                }

                ImGui.EndCombo();
            }

            var area = bookmark.Area;
            if (ImGui.BeginCombo("Housing area", area != 0 ? area.Name() : string.Empty)) {
                foreach (var housingArea in (HousingArea[]) Enum.GetValues(typeof(HousingArea))) {
                    if (!ImGui.Selectable(housingArea.Name(), area == housingArea)) {
                        continue;
                    }

                    bookmark.Area = housingArea;
                }

                ImGui.EndCombo();
            }

            var ward = (int) bookmark.Ward;
            if (ImGui.InputInt("Ward", ref ward)) {
                bookmark.Ward = (uint) Math.Max(1, Math.Min(24, ward));
            }

            var plot = (int) bookmark.Plot;
            if (ImGui.InputInt("Plot", ref plot)) {
                bookmark.Plot = (uint) Math.Max(1, Math.Min(60, plot));
            }

            if (ImGui.Button("Save")) {
                if (index < 0) {
                    this.Config.Bookmarks.Add(bookmark);
                } else if (index < this.Config.Bookmarks.Count) {
                    this.Config.Bookmarks[index] = bookmark;
                }

                this.Plugin.SaveConfig();
                this._editing = null;
            }

            ImGui.SameLine();

            if (ImGui.Button("Cancel")) {
                this._editing = null;
            }

            ImGui.End();
        }
    }
}

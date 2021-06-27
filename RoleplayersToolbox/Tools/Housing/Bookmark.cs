using System;

namespace RoleplayersToolbox.Tools.Housing {
    [Serializable]
    internal class Bookmark {
        public string Name;
        public uint WorldId;
        public HousingArea Area;
        public uint Ward;
        public uint Plot;

        public Bookmark(string name) {
            this.Name = name;
        }

        internal Bookmark Clone() {
            return new(this.Name) {
                WorldId = this.WorldId,
                Area = this.Area,
                Ward = this.Ward,
                Plot = this.Plot,
            };
        }
    }
}

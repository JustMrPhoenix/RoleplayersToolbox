#if ILLEGAL

using System;

namespace RoleplayersToolbox.Tools.Illegal.Emote {
    internal enum Emote : uint {
        ObjectSit = 96,
        Sleep = 88,
    }

    internal static class EmoteExt {
        internal static string Name(this Emote emote) => emote switch {
            Emote.ObjectSit => "Object sit",
            Emote.Sleep => "Sleep",
            _ => throw new ArgumentOutOfRangeException(nameof(emote), emote, null),
        };
    }
}

#endif

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using Dalamud.Game;
using Dalamud.Game.Text.SeStringHandling;

namespace RoleplayersToolbox {
    internal static class Util {
        public static double DistanceBetween(Vector3 a, Vector3 b) {
            var xDiff = a.X - b.X;
            var yDiff = a.Y - b.Y;
            var zDiff = a.Z - b.Z;
            var sumOfSquares = Math.Pow(xDiff, 2) + Math.Pow(yDiff, 2) + Math.Pow(zDiff, 2);
            return Math.Sqrt(sumOfSquares);
        }

        public static bool TryScanText(this SigScanner scanner, string sig, out IntPtr result) {
            result = IntPtr.Zero;
            try {
                result = scanner.ScanText(sig);
                return true;
            } catch (KeyNotFoundException) {
                return false;
            }
        }

        public static bool TryGetStaticAddressFromSig(this SigScanner scanner, string sig, out IntPtr result) {
            result = IntPtr.Zero;
            try {
                result = scanner.GetStaticAddressFromSig(sig);
                return true;
            } catch (KeyNotFoundException) {
                return false;
            }
        }

        public static SeString ReadSeString(IntPtr ptr, SeStringManager manager) {
            var bytes = ReadTerminatedBytes(ptr);
            return manager.Parse(bytes);
        }

        public static string ReadString(IntPtr ptr) {
            var bytes = ReadTerminatedBytes(ptr);
            return Encoding.UTF8.GetString(bytes);
        }

        private static unsafe byte[] ReadTerminatedBytes(IntPtr ptr) {
            if (ptr == IntPtr.Zero) {
                return new byte[0];
            }

            var bytes = new List<byte>();

            var bytePtr = (byte*) ptr;
            while (*bytePtr != 0) {
                bytes.Add(*bytePtr);
                bytePtr += 1;
            }

            return bytes.ToArray();
        }

        internal static IntPtr FollowPointerChain(IntPtr start, IEnumerable<int> offsets) {
            if (start == IntPtr.Zero) {
                return IntPtr.Zero;
            }

            foreach (var offset in offsets) {
                start = Marshal.ReadIntPtr(start + offset);
                if (start == IntPtr.Zero) {
                    return IntPtr.Zero;
                }
            }

            return start;
        }
    }
}

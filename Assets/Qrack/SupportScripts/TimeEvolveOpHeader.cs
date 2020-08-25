using System.Runtime.InteropServices;

namespace Qrack
{
    [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 136)]
    public struct TimeEvolveOpHeader
    {
        public uint target;
        public uint controlLen;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public uint[] controls;
    }
}
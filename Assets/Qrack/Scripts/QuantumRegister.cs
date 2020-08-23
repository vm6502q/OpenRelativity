using UnityEngine;

namespace Qrack
{
    public class QuantumRegister : QuantumSystem
    {
        public QuantumSystem QuantumSystem;

        public uint[] QuantumSystemMappings;

        override protected uint GetSystemIndex(uint registerIndex)
        {
            return QuantumSystemMappings[registerIndex];
        }

    }
}

using System.Collections.Generic;
using UnityEngine;

namespace Qrack
{
    public class QuantumRegister : QuantumSystem
    {
        public QuantumSystem QuantumSystem;

        public ulong[] QuantumSystemMappings;

        override protected ulong GetSystemIndex(ulong registerIndex)
        {
            return QuantumSystemMappings[registerIndex];
        }

        public override void CheckAlloc(List<ulong> bits)
        {
            QuantumSystem.CheckAlloc(bits);
        }

        protected void Start()
        {
            lastQubitCount = QubitCount;
        }

        override protected void Update()
        {
            base.Update();
            SystemId = QuantumSystem.SystemId;
        }
    }
}

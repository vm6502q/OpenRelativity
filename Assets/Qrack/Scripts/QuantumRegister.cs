using UnityEngine;

namespace Qrack
{
    public class QuantumRegister : QuantumSystem
    {
        public uint RegisterStartIndex = 0;
        public QuantumSystem QuantumSystem;

        private uint RegisterEnd
        {
            get
            {
                return RegisterStartIndex + QubitCount;
            }
        }

        override protected uint GetSystemIndex(uint registerIndex)
        {
            return registerIndex + RegisterStartIndex;
        }

        // Start is called before the first frame update
        void Start()
        {
            if (RegisterEnd > 64)
            {
                // Invalid bounds
                RegisterStartIndex = 0;
                QubitCount = 0;
            }

            if (QuantumSystem.QubitCount < RegisterEnd)
            {
                QuantumSystem.QubitCount = RegisterEnd;
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (RegisterEnd > 64)
            {
                // Invalid bounds
                RegisterStartIndex = 0;
                QubitCount = 0;
            }

            if (QuantumSystem.QubitCount < RegisterEnd)
            {
                QuantumSystem.QubitCount = RegisterEnd;
            }

        }
    }
}

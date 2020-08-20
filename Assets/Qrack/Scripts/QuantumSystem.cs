using UnityEngine;

namespace Qrack
{
    public class QuantumSystem : MonoBehaviour
    {
        public RealTimeQasmProgram QuantumProgram;
        public uint QubitCount = 1;
        public bool[] ClassicalBitRegisters;
        public float[] ClassicalFloatRegisters;

        private uint lastQubitCount;

        private QuantumManager _qMan = null;

        private QuantumManager qMan
        {
            get
            {
                if (_qMan == null)
                {
                    _qMan = FindObjectOfType<QuantumManager>();
                }

                return _qMan;
            }
        }

        private uint registerId;

        // Start is called before the first frame update
        void Start()
        {
            registerId = qMan.AllocateSimulator(QubitCount);
            lastQubitCount = QubitCount;
        }

        private void Update()
        {
            if (QubitCount > 64)
            {
                QubitCount = 64;
            }

            if (QubitCount < 1)
            {
                QubitCount = 1;
            }

            if (lastQubitCount < QubitCount)
            {
                for (uint i = lastQubitCount; i < QubitCount; i++)
                {
                    qMan.AllocateQubit(registerId, i);
                }
            }

            if (lastQubitCount > QubitCount)
            {
                for (uint i = (lastQubitCount - 1); i >= QubitCount; i--)
                {
                    qMan.ReleaseQubit(registerId, i);
                }
            }
        }

        private void OnDestroy()
        {
            if (qMan != null)
            {
                qMan.DeallocateSimulator(registerId);
            }
        }
    }
}
using Boo.Lang;
using System;
using System.Runtime.InteropServices;

using UnityEngine;

namespace Tachyoid
{
    public class QuantumManager : MonoBehaviour
    {
        public const string QRACKSIM_DLL_NAME = @"qrack_pinvoke";

        [DllImport(QRACKSIM_DLL_NAME, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl, EntryPoint = "init_count")]
        private static extern uint Init(uint numQubits);

        [DllImport(QRACKSIM_DLL_NAME, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl, EntryPoint = "destroy")]
        private static extern void Destroy(uint simId);

        [DllImport(QRACKSIM_DLL_NAME, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl, EntryPoint = "allocateQubit")]
        private static extern void AllocQubit(uint simId, uint qubitId);

        [DllImport(QRACKSIM_DLL_NAME, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl, EntryPoint = "release")]
        private static extern void Release(uint simId, uint qubitId);

        private List<uint> SimulatorIds = new List<uint>();

        private void OnDestroy()
        {
            for (int i = 0; i < SimulatorIds.Count; i++)
            {
                Destroy(SimulatorIds[i]);
            }
        }

        public uint AllocateSimulator(uint numQubits)
        {
            uint simId = Init(numQubits);
            SimulatorIds.Add(simId);
            return simId;
        }

        public void DeallocateSimulator(uint simId)
        {
            if (SimulatorIds.Contains(simId))
            {
                Destroy(simId);
                SimulatorIds.Remove(simId);
            }
        }

        public void AllocateQubit(uint simId, uint qubitId)
        {
            AllocQubit(simId, qubitId);
        }

        public void ReleaseQubit(uint simId, uint qubitId)
        {
            Release(simId, qubitId);
        }

    }
}

using Boo.Lang;
using System.Runtime.InteropServices;

using UnityEngine;

namespace Qrack
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

        [DllImport(QRACKSIM_DLL_NAME, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl, EntryPoint = "X")]
        private static extern void X(uint simId, uint qubitId);

        [DllImport(QRACKSIM_DLL_NAME, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Y")]
        private static extern void Y(uint simId, uint qubitId);

        [DllImport(QRACKSIM_DLL_NAME, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Z")]
        private static extern void Z(uint simId, uint qubitId);

        [DllImport(QRACKSIM_DLL_NAME, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl, EntryPoint = "H")]
        private static extern void H(uint simId, uint qubitId);

        [DllImport(QRACKSIM_DLL_NAME, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl, EntryPoint = "S")]
        private static extern void S(uint simId, uint qubitId);

        [DllImport(QRACKSIM_DLL_NAME, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl, EntryPoint = "T")]
        private static extern void T(uint simId, uint qubitId);

        [DllImport(QRACKSIM_DLL_NAME, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl, EntryPoint = "AdjS")]
        private static extern void AdjS(uint simId, uint qubitId);

        [DllImport(QRACKSIM_DLL_NAME, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl, EntryPoint = "AdjT")]
        private static extern void AdjT(uint simId, uint qubitId);

        [DllImport(QRACKSIM_DLL_NAME, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl, EntryPoint = "MCX")]
        private static extern void MCX(uint simId, uint controlLen, uint[] controls, uint targetId);

        [DllImport(QRACKSIM_DLL_NAME, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl, EntryPoint = "MCY")]
        private static extern void MCY(uint simId, uint controlLen, uint[] controls, uint targetId);

        [DllImport(QRACKSIM_DLL_NAME, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl, EntryPoint = "MCZ")]
        private static extern void MCZ(uint simId, uint controlLen, uint[] controls, uint targetId);

        [DllImport(QRACKSIM_DLL_NAME, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl, EntryPoint = "MCH")]
        private static extern void MCH(uint simId, uint controlLen, uint[] controls, uint targetId);

        [DllImport(QRACKSIM_DLL_NAME, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl, EntryPoint = "MCS")]
        private static extern void MCS(uint simId, uint controlLen, uint[] controls, uint targetId);

        [DllImport(QRACKSIM_DLL_NAME, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl, EntryPoint = "MCT")]
        private static extern void MCT(uint simId, uint controlLen, uint[] controls, uint targetId);

        [DllImport(QRACKSIM_DLL_NAME, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl, EntryPoint = "MCADJS")]
        private static extern void MCADJS(uint simId, uint controlLen, uint[] controls, uint targetId);

        [DllImport(QRACKSIM_DLL_NAME, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl, EntryPoint = "MCADJT")]
        private static extern void MCADJ(uint simId, uint controlLen, uint[] controls, uint targetId);

        [DllImport(QRACKSIM_DLL_NAME, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl, EntryPoint = "MCU")]
        private static extern void MCU(uint simId, uint controlLen, uint[] controls, uint targetId, double theta, double phi, double lambda);

        [DllImport(QRACKSIM_DLL_NAME, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl, EntryPoint = "M")]
        private static extern uint M(uint simId, uint qubitId);

        /*
        X, Y, Z, H, S, T, U,
        MCX, MCY, MCZ, MCH, MCS, MCT, MCU,
        M, QSET, QRESET
        */

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

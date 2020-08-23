#if OPEN_RELATIVITY_INCLUDED
using OpenRelativity.Objects;
#endif

using System.Collections.Generic;
using UnityEngine;

namespace Qrack
{
    public class QuantumSystem : MonoBehaviour
    {

        public uint QubitCount = 1;
        public float ClockOffset;

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

        private uint systemId;

        virtual protected uint GetSystemIndex(uint registerIndex)
        {
            return registerIndex;
        }

#if OPEN_RELATIVITY_INCLUDED
        private RelativisticObject _myRO;

        private RelativisticObject myRO
        {
            get
            {
                return _myRO != null ? _myRO : _myRO = GetComponent<RelativisticObject>();
            }
        }
#endif

        public float LocalTime
        {
            get
            {
#if OPEN_RELATIVITY_INCLUDED
                return ClockOffset + myRO.GetLocalTime();
#else
                return clockOFfset + Time.time;
#endif
            }
        }

        public float LocalDeltaTime
        {
            get
            {
#if OPEN_RELATIVITY_INCLUDED
                return (float)myRO.localDeltaTime;
#else
                return Time.deltaTime;
#endif
            }
        }

        public float LocalFixedDeltaTime
        {
            get
            {
#if OPEN_RELATIVITY_INCLUDED
                return (float)myRO.localFixedDeltaTime;
#else
                return Time.fixedDeltaTime;
#endif
            }
        }

        // Start is called before the first frame update
        void Start()
        {
            systemId = qMan.AllocateSimulator(QubitCount);
            lastQubitCount = QubitCount;
        }

        

        void Update()
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
                    QuantumManager.AllocateQubit(systemId, i);
                }
            }

            if (lastQubitCount > QubitCount)
            {
                for (uint i = (lastQubitCount - 1); i >= QubitCount; i--)
                {
                    QuantumManager.ReleaseQubit(systemId, i);
                }
            }

        }

        void OnDestroy()
        {
            if (qMan != null)
            {
                qMan.DeallocateSimulator(systemId);
            }
        }

        private uint[] MapControls(uint[] controls)
        {
            uint[] mappedControls = new uint[controls.Length];
            for (int i = 0; i < controls.Length; i++)
            {
                mappedControls[i] = GetSystemIndex(controls[i]);
            }

            return mappedControls;
        }

        public void Rand(uint targetId)
        {
            QuantumManager.Rand(systemId, GetSystemIndex(targetId));
        }

        public void X(uint targetId)
        {
            QuantumManager.X(systemId, GetSystemIndex(targetId));
        }

        public void Y(uint targetId)
        {
            QuantumManager.Y(systemId, GetSystemIndex(targetId));
        }

        public void Z(uint targetId)
        {
            QuantumManager.Z(systemId, GetSystemIndex(targetId));
        }

        public void H(uint targetId)
        {
            QuantumManager.H(systemId, GetSystemIndex(targetId));
        }
        public void S(uint targetId)
        {
            QuantumManager.S(systemId, GetSystemIndex(targetId));
        }

        public void T(uint targetId)
        {
            QuantumManager.T(systemId, GetSystemIndex(targetId));
        }

        public void AdjS(uint targetId)
        {
            QuantumManager.AdjS(systemId, GetSystemIndex(targetId));
        }

        public void AdjT(uint targetId)
        {
            QuantumManager.AdjT(systemId, GetSystemIndex(targetId));
        }

        public void U(uint targetId, double theta, double phi, double lambda)
        {
            QuantumManager.U(systemId, GetSystemIndex(targetId), theta, phi, lambda);
        }

        public void R(Pauli basis, double phi, uint targetId)
        {
            QuantumManager.R(systemId, (uint)basis, phi, GetSystemIndex(targetId));
        }

        public void Exp(uint targetId, double phi)
        {
            QuantumManager.Exp(systemId, GetSystemIndex(targetId), phi);
        }

        public void RX(uint targetId, double phi)
        {
            QuantumManager.RX(systemId, GetSystemIndex(targetId), phi);
        }

        public void RY(uint targetId, double phi)
        {
            QuantumManager.RY(systemId, GetSystemIndex(targetId), phi);
        }

        public void RZ(uint targetId, double phi)
        {
            QuantumManager.RZ(systemId, GetSystemIndex(targetId), phi);
        }

        public void MCX(uint[] controls, uint targetId)
        {
            QuantumManager.MCX(systemId, (uint)controls.Length, MapControls(controls), targetId);
        }

        public void MCY(uint[] controls, uint targetId)
        {
            QuantumManager.MCY(systemId, (uint)controls.Length, MapControls(controls), targetId);
        }

        public void MCZ(uint[] controls, uint targetId)
        {
            QuantumManager.MCZ(systemId, (uint)controls.Length, MapControls(controls), targetId);
        }

        public void MCH(uint[] controls, uint targetId)
        {
            QuantumManager.MCH(systemId, (uint)controls.Length, MapControls(controls), targetId);
        }

        public void MCS(uint[] controls, uint targetId)
        {
            QuantumManager.MCS(systemId, (uint)controls.Length, MapControls(controls), targetId);
        }

        public void MCT(uint[] controls, uint targetId)
        {
            QuantumManager.MCT(systemId, (uint)controls.Length, MapControls(controls), targetId);
        }

        public void MCADJS(uint[] controls, uint targetId)
        {
            QuantumManager.MCADJS(systemId, (uint)controls.Length, MapControls(controls), targetId);
        }

        public void MCADJT(uint[] controls, uint targetId)
        {
            QuantumManager.MCADJT(systemId, (uint)controls.Length, MapControls(controls), targetId);
        }

        public void MCU(uint[] controls, uint targetId, double theta, double phi, double lambda)
        {
            QuantumManager.MCU(systemId, (uint)controls.Length, MapControls(controls), GetSystemIndex(targetId), theta, phi, lambda);
        }

        public void MCR(Pauli basis, double phi, uint[] controls, uint targetId)
        {
            QuantumManager.MCR(systemId, (uint)basis, phi, (uint)controls.Length, MapControls(controls), GetSystemIndex(targetId));
        }

        public void MCExp(uint[] controls, uint targetId, double phi)
        {
            QuantumManager.MCExp(systemId, (uint)controls.Length, MapControls(controls), GetSystemIndex(targetId), phi);
        }

        public void MCRX(uint[] controls, uint targetId, double phi)
        {
            QuantumManager.MCRX(systemId, (uint)controls.Length, MapControls(controls), GetSystemIndex(targetId), phi);
        }

        public void MCRY(uint[] controls, uint targetId, double phi)
        {
            QuantumManager.MCRY(systemId, (uint)controls.Length, MapControls(controls), GetSystemIndex(targetId), phi);
        }

        public void MCRZ(uint[] controls, uint targetId, double phi)
        {
            QuantumManager.MCRZ(systemId, (uint)controls.Length, MapControls(controls), GetSystemIndex(targetId), phi);
        }

        public bool M(uint targetId)
        {
            return QuantumManager.M(systemId, GetSystemIndex(targetId)) > 0;
        }

        public void QSET(uint targetId)
        {
            if (!M(targetId))
            {
                X(targetId);
            }
        }

        public void QRESET(uint targetId)
        {
            if (M(targetId))
            {
                X(targetId);
            }
        }

        public void QAND(uint qInput1, uint qInput2, uint qOutput)
        {
            QuantumManager.AND(systemId, GetSystemIndex(qInput1), GetSystemIndex(qInput2), GetSystemIndex(qOutput));
        }

        public void QOR(uint qInput1, uint qInput2, uint qOutput)
        {
            QuantumManager.OR(systemId, GetSystemIndex(qInput1), GetSystemIndex(qInput2), GetSystemIndex(qOutput));
        }

        public void QXOR(uint qInput1, uint qInput2, uint qOutput)
        {
            QuantumManager.XOR(systemId, GetSystemIndex(qInput1), GetSystemIndex(qInput2), GetSystemIndex(qOutput));
        }

        public void QNAND(uint qInput1, uint qInput2, uint qOutput)
        {
            QuantumManager.NAND(systemId, GetSystemIndex(qInput1), GetSystemIndex(qInput2), GetSystemIndex(qOutput));
        }

        public void QNOR(uint qInput1, uint qInput2, uint qOutput)
        {
            QuantumManager.NOR(systemId, GetSystemIndex(qInput1), GetSystemIndex(qInput2), GetSystemIndex(qOutput));
        }

        public void QXNOR(uint qInput1, uint qInput2, uint qOutput)
        {
            QuantumManager.XNOR(systemId, GetSystemIndex(qInput1), GetSystemIndex(qInput2), GetSystemIndex(qOutput));
        }

        public void CQAND(bool cInput, uint qInput, uint cOutput)
        {
            QuantumManager.CLAND(systemId, cInput, GetSystemIndex(qInput), GetSystemIndex(cOutput));
        }

        public void CQOR(bool cInput, uint qInput, uint cOutput)
        {
            QuantumManager.CLOR(systemId, cInput, GetSystemIndex(qInput), GetSystemIndex(cOutput));
        }

        public void CQXOR(bool cInput, uint qInput, uint cOutput)
        {
            QuantumManager.CLXOR(systemId, cInput, GetSystemIndex(qInput), GetSystemIndex(cOutput));
        }

        public void CQNAND(bool cInput, uint qInput, uint cOutput)
        {
            QuantumManager.CLNAND(systemId, cInput, GetSystemIndex(qInput), GetSystemIndex(cOutput));
        }

        public void CQNOR(bool cInput, uint qInput, uint cOutput)
        {
            QuantumManager.CLNOR(systemId, cInput, GetSystemIndex(qInput), GetSystemIndex(cOutput));
        }

        public void CQXNOR(bool cInput, uint qInput, uint cOutput)
        {
            QuantumManager.CLXNOR(systemId, cInput, GetSystemIndex(qInput), GetSystemIndex(cOutput));
        }
    }
}
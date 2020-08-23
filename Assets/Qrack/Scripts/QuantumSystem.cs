#if OPEN_RELATIVITY_INCLUDED
using OpenRelativity.Objects;
#endif

using System.Collections.Generic;
using UnityEngine;

namespace Qrack
{
    public class QuantumSystem : MonoBehaviour
    {
        public RealTimeQasmProgram QuantumProgram;
        public uint QubitCount = 1;
        public bool[] ClassicalBits;
        public float[] ClassicalFloats;
        public int[] ClassicalAccumulators;

        private uint lastQubitCount;

        private float clockOffset;
        private List<bool> isIfTrue;
        private int nextClockBlockIndex;
        private int[] instructionIndices;
        private float[] countDowns;
        private bool[] isBlockFinished;

        private List<List<RealTimeQasmInstruction>> ActiveClockBlock { get; set; }

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

        private float LocalTime
        {
            get
            {
#if OPEN_RELATIVITY_INCLUDED
                return clockOffset + myRO.GetLocalTime();
#else
                return clockOFfset + Time.time;
#endif
            }
        }

        private float LocalDeltaTime
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

        private float LocalFixedDeltaTime
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
            clockOffset = 0;
            nextClockBlockIndex = 0;
            isIfTrue = new List<bool>();
            IterateClockBlock();
        }

        private void IterateClockBlock()
        {
            if (QuantumProgram.ClockBlocks.Count <= nextClockBlockIndex)
            {
                if (QuantumProgram.doRepeat)
                {
                    nextClockBlockIndex = 0;
                } else
                {
                    ActiveClockBlock = null;
                    return;
                }
            }

            ActiveClockBlock = QuantumProgram.ClockBlocks[nextClockBlockIndex];
            instructionIndices = new int[ActiveClockBlock.Count];
            countDowns = new float[ActiveClockBlock.Count];
            isBlockFinished = new bool[ActiveClockBlock.Count];
            nextClockBlockIndex++;
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

            RunInstructions();
        }

        void OnDestroy()
        {
            if (qMan != null)
            {
                qMan.DeallocateSimulator(systemId);
            }
        }

        private void RunInstructions()
        {
            if (ActiveClockBlock == null)
            {
                return;
            }

            bool isAllFinished = true;

            for (int i = 0; i < ActiveClockBlock.Count; i++)
            {
                if (isBlockFinished[i])
                {
                    continue;
                }

                if (instructionIndices[i] == 0)
                {
                    if (LocalTime >= ActiveClockBlock[i][0].Time)
                    {
                        RunBlock(i);
                    }
                } else
                {
                    countDowns[i] -= LocalDeltaTime;

                    if (countDowns[i] <= 0)
                    {
                        RunBlock(i);
                    }
                }

                if (!isBlockFinished[i])
                {
                    isAllFinished = false;
                }
            }

            if (isAllFinished)
            {
                int lastBlock = ActiveClockBlock.Count - 1;
                int lastIndex = ActiveClockBlock[lastBlock].Count - 1;
                RealTimeQasmInstruction lastInstruction = ActiveClockBlock[lastBlock][lastIndex];
                if (lastInstruction.Gate == QasmInstruction.SETCLOCK)
                {
                    clockOffset = lastInstruction.FloatValue - (LocalTime - clockOffset);
                }
                IterateClockBlock();
            }
        }

        private void RunBlock(int i)
        {
            RunInstruction(i);
            instructionIndices[i]++;

            bool isChained = true;

            while ((ActiveClockBlock[i].Count > instructionIndices[i]) && isChained)
            {
                RealTimeQasmInstruction nextInstruction;
                if (ActiveClockBlock[i].Count > instructionIndices[i])
                {
                    nextInstruction = ActiveClockBlock[i][instructionIndices[i]];
                }
                else
                {
                    isChained = false;
                    continue;
                }

                if (nextInstruction.IsForcedSerial ||
                    (nextInstruction.IsRelativeTime && (nextInstruction.Time <= (-countDowns[i]))))
                {
                    RunInstruction(i);
                    instructionIndices[i]++;
                    countDowns[i] += nextInstruction.Time;
                }
            }

            if (ActiveClockBlock[i].Count > instructionIndices[i])
            {
                countDowns[i] += ActiveClockBlock[i][1].Time;
            }
            else
            {
                isBlockFinished[i] = true;
            }
        }

        private void SkipBranch(int blockIndex)
        {
            QasmInstruction nextGate;
            do
            {
                instructionIndices[blockIndex]++;
                nextGate = ActiveClockBlock[blockIndex][instructionIndices[blockIndex]].Gate;
            } while ((nextGate != QasmInstruction.ELSE) && (nextGate != QasmInstruction.ENDIF));

            if (nextGate == QasmInstruction.ENDIF)
            {
                isIfTrue.RemoveAt(isIfTrue.Count - 1);
            }
        }

        private void RunInstruction(int blockIndex)
        {
            RealTimeQasmInstruction rtqi = ActiveClockBlock[blockIndex][instructionIndices[blockIndex]];

            bool isTrue = false;

            if (rtqi.Gate == QasmInstruction.IF)
            {
                isTrue = true;
                for (int i = 0; i < rtqi.Controls.Length; i++)
                {
                    if (rtqi.IsIndirectControls[i])
                    {
                        isTrue &= ClassicalBits[ClassicalAccumulators[rtqi.Controls[i]]];
                    }
                    else
                    {
                        isTrue &= ClassicalBits[rtqi.Controls[i]];
                    }

                    if (!isTrue)
                    {
                        break;
                    }
                }

                isIfTrue.Add(isTrue);

                if (!isTrue)
                {
                    SkipBranch(blockIndex);
                }
            }

            if (rtqi.Gate == QasmInstruction.ELSE && isIfTrue[isIfTrue.Count - 1])
            {
                SkipBranch(blockIndex);
            }

            if (rtqi.Gate == QasmInstruction.ENDIF)
            {
                isIfTrue.RemoveAt(isIfTrue.Count - 1);
            }

            instructionIndices[blockIndex]++;
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

        public void MCX(uint controlLen, uint[] controls, uint targetId)
        {
            QuantumManager.MCX(systemId, controlLen, MapControls(controls), targetId);
        }

        public void MCY(uint controlLen, uint[] controls, uint targetId)
        {
            QuantumManager.MCY(systemId, controlLen, MapControls(controls), targetId);
        }

        public void MCZ(uint controlLen, uint[] controls, uint targetId)
        {
            QuantumManager.MCZ(systemId, controlLen, MapControls(controls), targetId);
        }

        public void MCH(uint controlLen, uint[] controls, uint targetId)
        {
            QuantumManager.MCH(systemId, controlLen, MapControls(controls), targetId);
        }

        public void MCS(uint controlLen, uint[] controls, uint targetId)
        {
            QuantumManager.MCS(systemId, controlLen, MapControls(controls), targetId);
        }

        public void MCT(uint controlLen, uint[] controls, uint targetId)
        {
            QuantumManager.MCT(systemId, controlLen, MapControls(controls), targetId);
        }

        public void MCADJS(uint controlLen, uint[] controls, uint targetId)
        {
            QuantumManager.MCADJS(systemId, controlLen, MapControls(controls), targetId);
        }

        public void MCADJT(uint controlLen, uint[] controls, uint targetId)
        {
            QuantumManager.MCADJT(systemId, controlLen, MapControls(controls), targetId);
        }

        public void MCU(uint controlLen, uint[] controls, uint targetId, double theta, double phi, double lambda)
        {
            QuantumManager.MCU(systemId, controlLen, MapControls(controls), GetSystemIndex(targetId), theta, phi, lambda);
        }

        public void MCR(Pauli basis, double phi, uint controlLen, uint[] controls, uint targetId)
        {
            QuantumManager.MCR(systemId, (uint)basis, phi, controlLen, MapControls(controls), GetSystemIndex(targetId));
        }

        public void MCExp(uint controlLen, uint[] controls, uint targetId, double phi)
        {
            QuantumManager.MCExp(systemId, controlLen, MapControls(controls), GetSystemIndex(targetId), phi);
        }

        public void MCRX(uint controlLen, uint[] controls, uint targetId, double phi)
        {
            QuantumManager.MCRX(systemId, controlLen, MapControls(controls), GetSystemIndex(targetId), phi);
        }

        public void MCRY(uint controlLen, uint[] controls, uint targetId, double phi)
        {
            QuantumManager.MCRY(systemId, controlLen, MapControls(controls), GetSystemIndex(targetId), phi);
        }

        public void MCRZ(uint controlLen, uint[] controls, uint targetId, double phi)
        {
            QuantumManager.MCRZ(systemId, controlLen, MapControls(controls), GetSystemIndex(targetId), phi);
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

        private void AdjustClassicalRegisterLength(uint cTargetId)
        {
            bool[] nRegisters = new bool[cTargetId + 1];

            for (int i = 0; i < ClassicalBits.Length; i++)
            {
                nRegisters[i] = ClassicalBits[i];
            }

            ClassicalBits = nRegisters;
        }

        private void SetOrReset(uint cTargetId, bool set)
        {
            if (cTargetId < ClassicalBits.Length)
            {
                ClassicalBits[cTargetId] = set;

                return;
            }

            AdjustClassicalRegisterLength(cTargetId);

            ClassicalBits[cTargetId] = set;
        }

        public void SET(uint cTargetId)
        {
            SetOrReset(cTargetId, true);
        }

        public void RESET(uint cTargetId)
        {
            SetOrReset(cTargetId, false);
        }

        public void FLOAD(uint fTargetId, float value)
        {
            ClassicalFloats[fTargetId] = value;
        }

        public void NOT(uint cTargetId)
        {
            if (cTargetId < ClassicalFloats.Length)
            {
                AdjustClassicalRegisterLength(cTargetId);
            }

            ClassicalBits[cTargetId] = !ClassicalBits[cTargetId];
        }

        private void AdjustBoolMaxIndex(uint cInput1, uint cInput2, uint cOutput)
        {
            // Store maximum value in cInput1

            if (cInput2 > cInput1)
            {
                cInput1 = cInput2;
            }

            if (cOutput > cInput1)
            {
                cInput1 = cOutput;
            }

            if (cInput1 < ClassicalFloats.Length)
            {
                AdjustClassicalRegisterLength(cInput1);
            }
        }

        public void AND(uint cInput1, uint cInput2, uint cOutput)
        {
            AdjustBoolMaxIndex(cInput1, cInput2, cOutput);
            ClassicalBits[cOutput] = ClassicalBits[cInput1] && ClassicalBits[cInput2];
        }

        public void OR(uint cInput1, uint cInput2, uint cOutput)
        {
            AdjustBoolMaxIndex(cInput1, cInput2, cOutput);
            ClassicalBits[cOutput] = ClassicalBits[cInput1] || ClassicalBits[cInput2];
        }

        public void XOR(uint cInput1, uint cInput2, uint cOutput)
        {
            AdjustBoolMaxIndex(cInput1, cInput2, cOutput);
            ClassicalBits[cOutput] = ClassicalBits[cInput1] ^ ClassicalBits[cInput2];
        }

        public void NAND(uint cInput1, uint cInput2, uint cOutput)
        {
            AdjustBoolMaxIndex(cInput1, cInput2, cOutput);
            ClassicalBits[cOutput] = !(ClassicalBits[cInput1] && ClassicalBits[cInput2]);
        }

        public void NOR(uint cInput1, uint cInput2, uint cOutput)
        {
            AdjustBoolMaxIndex(cInput1, cInput2, cOutput);
            ClassicalBits[cOutput] = !(ClassicalBits[cInput1] || ClassicalBits[cInput2]);
        }

        public void XNOR(uint cInput1, uint cInput2, uint cOutput)
        {
            AdjustBoolMaxIndex(cInput1, cInput2, cOutput);
            ClassicalBits[cOutput] = !(ClassicalBits[cInput1] ^ ClassicalBits[cInput2]);
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

        public void CQAND(uint cInput, uint qInput, uint cOutput)
        {
            if (cInput < ClassicalFloats.Length)
            {
                AdjustClassicalRegisterLength(cInput);
            }
            QuantumManager.CLAND(systemId, ClassicalBits[cInput], GetSystemIndex(qInput), GetSystemIndex(cOutput));
        }

        public void CQOR(uint cInput, uint qInput, uint cOutput)
        {
            if (cInput < ClassicalFloats.Length)
            {
                AdjustClassicalRegisterLength(cInput);
            }
            QuantumManager.CLOR(systemId, ClassicalBits[cInput], GetSystemIndex(qInput), GetSystemIndex(cOutput));
        }

        public void CQXOR(uint cInput, uint qInput, uint cOutput)
        {
            if (cInput < ClassicalFloats.Length)
            {
                AdjustClassicalRegisterLength(cInput);
            }
            QuantumManager.CLXOR(systemId, ClassicalBits[cInput], GetSystemIndex(qInput), GetSystemIndex(cOutput));
        }

        public void CQNAND(uint cInput, uint qInput, uint cOutput)
        {
            if (cInput < ClassicalFloats.Length)
            {
                AdjustClassicalRegisterLength(cInput);
            }
            QuantumManager.CLNAND(systemId, ClassicalBits[cInput], GetSystemIndex(qInput), GetSystemIndex(cOutput));
        }

        public void CQNOR(uint cInput, uint qInput, uint cOutput)
        {
            if (cInput < ClassicalFloats.Length)
            {
                AdjustClassicalRegisterLength(cInput);
            }
            QuantumManager.CLNOR(systemId, ClassicalBits[cInput], GetSystemIndex(qInput), GetSystemIndex(cOutput));
        }

        public void CQXNOR(uint cInput, uint qInput, uint cOutput)
        {
            if (cInput < ClassicalFloats.Length)
            {
                AdjustClassicalRegisterLength(cInput);
            }
            QuantumManager.CLXNOR(systemId, ClassicalBits[cInput], GetSystemIndex(qInput), GetSystemIndex(cOutput));
        }
    }
}
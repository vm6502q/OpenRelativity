using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Qrack
{
    public abstract class RealTimeQasmProgram : MonoBehaviour
    {
        public QuantumSystem QuantumSystem;
        public bool DoRepeat = false;
        public int InstructionIndex = 0;

        private float nextInstructionTime;

        protected List<RealTimeQasmInstruction> ProgramInstructions { get; set; }
        protected abstract void StartProgram();

        private void Start()
        {
            nextInstructionTime = 0;

            ProgramInstructions = new List<RealTimeQasmInstruction>();

            StartProgram();

            StartCoroutine(RunProgram());
        }

        IEnumerator RunProgram()
        {
            RealTimeQasmInstruction rtqi = ProgramInstructions[InstructionIndex];

            if (nextInstructionTime <= QuantumSystem.LocalTime)
            {
                rtqi.quantumProgramUpdate(this);

                InstructionIndex++;

                if (InstructionIndex >= ProgramInstructions.Count)
                {
                    InstructionIndex = 0;
                    if (!DoRepeat)
                    {
                        gameObject.SetActive(false);
                    }
                }

                if (rtqi.IsRelativeTime)
                {
                    nextInstructionTime += rtqi.Time;
                }
                else
                {
                    nextInstructionTime = rtqi.Time;
                }
            }

            yield return null;
        }

    }

}